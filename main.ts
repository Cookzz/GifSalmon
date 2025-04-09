import { 
  app, 
  BrowserWindow, 
  ipcMain, 
  screen, 
  dialog,
  webUtils 
} from 'electron'
import fixPath from 'fix-path';
import { spawn, exec, ChildProcessWithoutNullStreams } from 'node:child_process'
import { join, dirname } from 'node:path'
import { unlinkSync, statSync } from 'node:fs'
import * as url from 'node:url'
import * as util from 'node:util'

/* Typescript doesn't work well on these */
const { Low } = require('lowdb')
const { JSONFile } = require('lowdb/node')
const { sync } = require('which')

fixPath()
const settingsPath = join(app.getPath('userData'),'app_db.json');

let win: BrowserWindow | null;
let db: any;
let ffmpeg_path: string;
let ffprobe_path: string;
let ffmpeg_ps: ChildProcessWithoutNullStreams;

const createWindow = (needs_ffmpeg?: boolean) => {
  const externalDisplay = screen.getPrimaryDisplay()
  let width = 800;
  let height = 600;
  if (externalDisplay) {
      width = externalDisplay.bounds.width*0.8;
      height = externalDisplay.bounds.height*0.8;
  }
  win = new BrowserWindow({
    width: width,
    height: height,
    webPreferences: {
        nodeIntegration: true,
        preload: join(__dirname, 'preload.js')
    }
  });

  win.webContents.openDevTools()
  // load the dist folder from Angular
  win.loadURL(
    url.format({
      pathname: join(__dirname, "/dist/browser/index.html"),
      protocol: "file:",
      slashes: true
    })
  );
  // The following is optional and will open the DevTools:
  // win.webContents.openDevTools()
  win.on("closed", () => {
    win = null;
  });
}

app.whenReady().then(() => {
  db = new Low(new JSONFile(settingsPath), { ffmpeg: {ffmpeg:false, ffprobe:false}, window:{x:0, y:0, width:800, height:600}});
  db.write();

  const { ffmpeg } = db.data
  let ffmpeg_store = ffmpeg
  console.log(ffmpeg_store);

  let ffmpegOpts = null;
  if(ffmpeg_store.ffmpeg){ 
      ffmpegOpts = { path: dirname(ffmpeg_store.ffmpeg), nothrow: true } 
  }

  const hasFfmpeg = sync('ffmpeg', ffmpegOpts ? ffmpegOpts : { nothrow: true })

  ffmpeg_path = (hasFfmpeg) ? hasFfmpeg : false;
  let ffprobeOpts = {};
  if (ffmpeg_store.ffprobe) { 
      ffprobeOpts = { path: dirname(ffmpeg_store.ffprobe) } 
  }

  const hasffprobe = sync('ffprobe', ffprobeOpts ? ffprobeOpts : { nothrow: true })
  ffprobe_path = (hasffprobe) ? hasffprobe : false;

  console.log(ffmpeg_path, ffprobe_path);
  createWindow(!ffmpeg_path || !ffprobe_path);
});

// on macOS, closing the window doesn't quit the app
app.on("window-all-closed", () => {
  if (process.platform !== "darwin") {
    app.quit();
  }
});

app.on('activate', function () {
    // On OS X it's common to re-create a window in the app when the
    // dock icon is clicked and there are no other windows open.
    if (win === null) {
        createWindow()
    }
})

/* Invoke Handler */
ipcMain.handle('save-dialog', async (event, path) => {
  const { canceled, filePath } = await dialog.showSaveDialog({title:"Export GIF", defaultPath: path, buttonLabel:"Export"})

  if (canceled){
      return null
  }

  return filePath
})

// ipcMain.on('message', (event, message) => {
//     console.log("Message from Renderer:", message);
// })

ipcMain.on('probeInput', (event, input) => {
  const cmd = `"${ffprobe_path}"  -v quiet -print_format json -show_format -show_streams "${input}"`

  exec(cmd, (so,se,e) => {
      const metadata = JSON.parse(se);
      event.sender.send('probeResult', metadata);
  });
});

ipcMain.on('getPalette', (event, input, prefs) => {
  const w = prefs.dimensions.width || 320;
  const h = prefs.dimensions.height || 240;
  const fps = prefs.fps || 24;
  const stats = prefs.color.stats_mode || 'full';
  // var colors = prefs.color.colors || 256;
  const transparency = (prefs.color.alpha?1:0) || 0;
  const colors = prefs.color.count || 256;

  const scaleCmd = `scale=${w}:${h}`;

  const vf = util.format("fps=%s,%s:flags=lanczos,palettegen=stats_mode=%s:max_colors=%s:reserve_transparent=%s", fps, scaleCmd, stats, colors, transparency);

  if (ffmpeg_ps) { ffmpeg_ps.kill(); }

  ffmpeg_ps = spawn(ffmpeg_path, ["-i", input, "-vf", vf, "-f", "image2", "-vcodec", "png", "pipe:1"])

  let data: any[] = [];
  ffmpeg_ps.stdout.on('data', (d: any) => {
      data.push(d);
  });
  ffmpeg_ps.on('close', () => {
      event.sender.send('paletteResult', Buffer.concat(data).toString('base64'));
  });
})

ipcMain.on('getThumbnail', (event, input, palette, time, settings) => {
  const w = settings.dimensions.width || 320;
  const h = settings.dimensions.height || 240;
  const fps = settings.fps || 24;
  const scaleCmd = "scale="+w+":"+h;
  const dither = settings.color.dither ? ('bayer:bayer_scale='+settings.color.dither_scale) : 'none';
  const diff_mode = settings.color.diff_mode ? settings.color.diff_mode : "rectangle";

  const vf = util.format("fps=%s,%s:flags=lanczos [x];[x][1:v]paletteuse=dither=%s:diff_mode=%s", fps, scaleCmd, dither, diff_mode);

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

  let data: any[] = [];
  ffmpeg_ps.stdout.on('data', (d) => {
      data.push(d);
  });

  ffmpeg_ps.on('close', () => {
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