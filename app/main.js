const { app, BrowserWindow, ipcMain, dialog, screen } = require('electron');
const path = require('path');
const url = require('url')
const util = require('util');
const fs = require('fs');

const spawn = require('child_process').spawn;
const exec = require('child_process').exec;

const settingsPath = path.join(app.getPath('userData'),'app_db.json');
const { Low } = require('lowdb');
const { JSONFile } = require('lowdb/node')

let fixpath = require('fix-path').default;
fixpath()

let which = require('which');

let ffbinaries = require('ffbinaries');
let ffmpeg_ps
let db

function createWindow(needs_ffmpeg) {
    let externalDisplay = screen.getPrimaryDisplay()
    let width = 800;
    let height = 600;
    if (externalDisplay) {
        width = externalDisplay.bounds.width*0.8;
        height = externalDisplay.bounds.height*0.8;
    }
    const win = new BrowserWindow({
        width: width,
        height: height,
        webPreferences: {
            nodeIntegration: true,
            contextIsolation: false
        }
    });
    win.loadURL(url.format({
        pathname: path.join(__dirname, needs_ffmpeg ? 'ffmpeg.html':'index.html'),
        protocol: 'file:',
        slashes: true
    }));
    win.on('closed', function () {
        // Dereference the window object, usually you would store windows
        // in an array if your app supports multi windows, this is the time
        // when you should delete the corresponding element.
        mainWindow = null
    })
}

app.whenReady().then(() => {
    db = new Low(new JSONFile(settingsPath), { ffmpeg: {ffmpeg:false, ffprobe:false}, window:{x:0, y:0, width:800, height:600}});
    db.write();

    const { ffmpeg } = db.data
    let ffmpeg_store = ffmpeg
    console.log(ffmpeg_store);

    let ffmpegOpts = null;
    if(ffmpeg_store.ffmpeg){ 
        ffmpegOpts = { path: path.dirname(ffmpeg_store.ffmpeg), nothrow: true } 
    }

    console.log("run ffmpeg")
    const hasFfmpeg = which.sync('ffmpeg', ffmpegOpts ? ffmpegOpts : { nothrow: true })

    ffmpeg_path = (hasFfmpeg) ? hasFfmpeg : false;
    let ffprobeOpts = {};
    if (ffmpeg_store.ffprobe) { 
        ffprobeOpts = { path: path.dirname(ffmpeg_store.ffprobe) } 
    }

    const hasffprobe = which.sync('ffprobe', ffprobeOpts ? ffprobeOpts : { nothrow: true })
    ffprobe_path = (hasffprobe) ? hasffprobe : false;

    console.log(ffmpeg_path, ffprobe_path);
    createWindow(!ffmpeg_path || !ffprobe_path);
});
app.on('window-all-closed', () => {
    app.quit();
});
app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
});

ipcMain.on('install_ffmpeg',function(event,input){
    console.log("INSTALL");
    let platform = ffbinaries.detectPlatform();
    let dest = path.join(app.getPath("userData"), "ffmpeg");

    ffbinaries.downloadFiles(['ffmpeg','ffprobe'], {quiet: true, destination: dest}, function () {
        console.log('Downloaded binaries for ' + platform + '.');
        ffmpeg_path = dest+"/ffmpeg";
        ffprobe_path = dest+"/ffprobe";

        db.update(({ ffmpeg }) => ffmpeg = Object.assign({}, ffmpeg, { ffmpeg: { ffmpeg: ffmpeg_path, ffprobe: ffprobe_path } }))

        mainWindow.loadURL(url.format({
            pathname: path.join(__dirname, 'index.html'),
            protocol: 'file:',
            slashes: true
        }))
    // event.sender.send('ffmpeg_installed', platform);
    });

});
// In this file you can include the rest of your app's specific main process
// code. You can also put them in separate files and require them here.
ipcMain.on('probeInput', (event, input) => {
    console.log("PROBE", ffprobe_path);
    console.log("INPUT", input)
    var cmd = "\""+ffprobe_path+"\" -v quiet -print_format json -show_format -show_streams \""+input+"\"";

    exec(cmd, function(so,se,e) {
        console.log(so,se,e);
        var metadata = JSON.parse(se);
        event.sender.send('probeResult', metadata);
    });
});
ipcMain.on('getPalette', (event, input, prefs) => {
    // console.log("prefs",prefs);
    // var input = prefs.file.input;
    var w = prefs.dimensions.width || 320;
    var h = prefs.dimensions.height || 240;
    var fps = prefs.fps || 24;
    var stats = prefs.color.stats_mode || 'full';
    // var colors = prefs.color.colors || 256;
    var transparency = (prefs.color.alpha?1:0) || 0;
    var colors = prefs.color.count || 256;

    var scaleCmd = "scale="+w+":"+h;

    var vf = util.format("fps=%s,%s:flags=lanczos,palettegen=stats_mode=%s:max_colors=%s:reserve_transparent=%s", fps, scaleCmd, stats, colors, transparency);

    if(ffmpeg_ps) ffmpeg_ps.kill();

    const fileInput = input
    ffmpeg_ps = spawn(ffmpeg_path, ["-i", fileInput, "-vf", vf, "-f", "image2", "-vcodec", "png", "pipe:1"])

    var data = [];
    ffmpeg_ps.stdout.on('data',function(d){
        data.push(d);
    });
    ffmpeg_ps.stderr.on('data',function(d){
        // console.log("stderr",d.toString());
    });
    ffmpeg_ps.on('close',function(){
        event.sender.send('paletteResult', Buffer.concat(data).toString('base64'));
    });
})
ipcMain.on('cancelProcess', (event,opts) => {
    if(ffmpeg_ps){ ffmpeg_ps.kill(); }
});

var canceled = false;
ipcMain.on('cancel_export', (event, opts) => {
    canceled=true;
    if(ffmpeg_ps){ ffmpeg_ps.kill(); }
    if(opts.remove){
        //remove the partially exported file
        console.log("REMOVE", opts.remove);
        fs.unlinkSync(opts.remove);
    }
});
ipcMain.on('getThumbnail', (event, input, palette, time, settings) => {

    // .frames(1)
// .duration(1)
// .complexFilter("fps="+fps+","+scaleCmd+":flags=lanczos[x];[x][1:v]paletteuse=dither="+dither)
    // .outputFormat("gif")
    // var settings = {fps:30,dither:'true'};
    // var gif = "fps="+settings.fps+",flags=lanczos[x];[x][1:v]paletteuse=dither="+settings.dither;
    var w = settings.dimensions.width || 320;
    var h = settings.dimensions.height || 240;
    var fps = settings.fps || 24;
    var scaleCmd = "scale="+w+":"+h;
    var dither = settings.color.dither ? ('bayer:bayer_scale='+settings.color.dither_scale) : 'none';
    var diff_mode = settings.color.diff_mode ? settings.color.diff_mode : "rectangle";
    var diff_mode = settings.loop ? settings.loop : 0;

    var vf = util.format("fps=%s,%s:flags=lanczos [x];[x][1:v]paletteuse=dither=%s:diff_mode=%s", fps, scaleCmd, dither, diff_mode);
    console.log(vf);

    if(ffmpeg_ps) ffmpeg_ps.kill();
    ffmpeg_ps = spawn(ffmpeg_path, [
        "-ss", time,
        "-i", input,
        "-i", "pipe:0",
        "-t", 1,
        "-lavfi", vf,
        "-frames:v", 1,
        "-an", "-f", "image2",
        "-vcodec", "gif", "pipe:1"
    ])

    var data = [];
    ffmpeg_ps.stdout.on('data',function(d){
        // console.log("stdout",d.toString());
        data.push(d);
    });
    ffmpeg_ps.stderr.on('data',function(d){
    // console.log("stderr",d.toString());
    });
    ffmpeg_ps.on('close', function(){
        event.sender.send('thumbnailResult', Buffer.concat(data).toString('base64'));
        console.log('done');
    });

    ffmpeg_ps.stdin.write(Buffer.from(palette, 'base64'));
    ffmpeg_ps.stdin.end();

    // exec("ffmpeg -ss "+time+" -i "+input+" -t 1 -frames:v 1 -f image2 -vcodec png -", function(so,se,e) {
    //   // console.log(so,se,e);
    //   console.log(so);
    //   event.sender.send('thumbnailResult', new Buffer(se, 'binary').toString('base64'));
    // });
});
ipcMain.on('exportGif', (event, input, output, palette, settings) => {
    var stats = settings.color.stats_mode || 'full';
    var dither = settings.color.dither ? ('bayer:bayer_scale='+settings.color.dither_scale) : 'none';
    var colors = settings.color.colors || 256;
    var fps = settings.fps || 10;
    var w = settings.dimensions.width || 320;
    var h = settings.dimensions.height || 240;
    var scaleCmd = "scale="+w+":"+h;
    var vf = util.format("fps=%s,%s:flags=lanczos [x];[x][1:v]paletteuse=dither=%s", fps, scaleCmd, dither);

    // var filters = util.format("fps=%s,%s:flags=lanczos[x];[x][1:v]paletteuse=dither=%s", fps, scaleCmd, dither);
    if(ffmpeg_ps) ffmpeg_ps.kill();

    ffmpeg_ps = spawn(ffmpeg_path, [
        "-i", input,
        "-i", "pipe:0",
        "-lavfi", vf,
        "-an",
        "-f", "gif",
        output
    ])
    // var data = [];
    ffmpeg_ps.stdout.on('data',function(d){
        console.log("stdout",d.toString());
        // data.push(d);
    });
    ffmpeg_ps.stderr.on('data',function(d){
        // var time = /time=(\d+)\:(\d+)\:(\d+)\.(\d+)\s+/.exec(d.toString());
        var progress = parseProgressLine(d.toString());
        console.log(progress);
        // if(time && time.length >= 5){
        if(progress && progress.time){
            // var time = (parseInt(time[1])*3600) + (parseInt(time[2])*60) + parseFloat(time[3] + "." + time[4]);
            // var sec = timemarkToSeconds(progress.time);
            // var size = parseInt(progress.size.replace('kB',''))*1000;

            var progress_obj = {
                sec: timemarkToSeconds(progress.time),
                size: progress.size ? parseInt(progress.size.replace('kB',''))*1000 : 0
            }
            event.sender.send('export_progress', progress_obj);
        }
    });
    ffmpeg_ps.on('close',function(){
    // event.sender.send('thumbnailResult', Buffer.concat(data).toString('base64'));
    console.log('done');
    if(canceled){
        canceled=false;
        event.sender.send('export_canceled', {});
    }else{
        event.sender.send('export_finished', getFilesizeInBytes(output));
    }
    });

    ffmpeg_ps.stdin.write(Buffer.from(palette, 'base64'));
    ffmpeg_ps.stdin.end();
});

ipcMain.handle('save-dialog', async function(event, output){
    const { canceled, filePath } = await dialog.showSaveDialog({title:"Export GIF", defaultPath: output, buttonLabel:"Export"})

    if (canceled){
        return null
    }

    return filePath
})

function timemarkToSeconds(timemark) {
    if (typeof timemark === 'number') {
        return timemark;
    }

    if (timemark.indexOf(':') === -1 && timemark.indexOf('.') >= 0) {
        return Number(timemark);
    }

    var parts = timemark.split(':');

    // add seconds
    var secs = Number(parts.pop());

    if (parts.length) {
        // add minutes
        secs += Number(parts.pop()) * 60;
    }

    if (parts.length) {
        // add hours
        secs += Number(parts.pop()) * 3600;
    }

    return secs;
}

function parseProgressLine(line) {
    var progress = {};

    // Remove all spaces after = and trim
    line  = line.replace(/=\s+/g, '=').trim();
    var progressParts = line.split(' ');

    // Split every progress part by "=" to get key and value
    for(var i = 0; i < progressParts.length; i++) {
        var progressSplit = progressParts[i].split('=', 2);
        var key = progressSplit[0];
        var value = progressSplit[1];

        // This is not a progress line
        if(typeof value === 'undefined')
            return null;

        progress[key] = value;
    }

    return progress;
}
function getFilesizeInBytes(filename) {
    const stats = fs.statSync(filename)
    const fileSizeInBytes = stats.size
    return fileSizeInBytes
}