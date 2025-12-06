import { Component, NgZone } from '@angular/core';
import { FileSizePipe, RatioPipe, NumberPipe, FrameratePipe } from '../../pipes'
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { TooltipModule } from 'primeng/tooltip';

import { Loader } from "../loader/loader.component";
import { ElectronService } from '../../../service/electron.service';
import { Defaults } from '../../types/default.interface';
import { FrameScrubber } from "../frame-scrubber/frame-scrubber.component";
import { defaultValue } from '../../const/defaults';
import { PixelPalette } from "../pixel-palette/palette.component";

@Component({
  selector: 'home',
  imports: [FileSizePipe, RatioPipe, NumberPipe, FormsModule, CommonModule, TooltipModule, Loader, FrameScrubber, PixelPalette],
  providers: [FrameratePipe],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})

export class HomeComponent {
  title = 'GifSalmon';

  frames = {
    current: 0,
    min: 0,
    max: 100
  }

  status = {
    state: 0,
    export: {
      progress: 0,
      size: 0,
      path: null,
      canceling: false
    }
  }

  //these follow default, but not sure what "stored" value is for
  defaults: Defaults = Object.assign({}, defaultValue)
  settings: Defaults = Object.assign({}, defaultValue)
  stored: Defaults = Object.assign({}, defaultValue)

  thumbnail: string | null = null
  palette = null
  color_value = 256

  //expose preloaded function in the renderer
  electron = new ElectronService().getElectron()

  constructor(
    private frameratePipe: FrameratePipe,
    private zone: NgZone //this is required as listeners technically runs "outside" of angular globalThis
  ){
    this.initListeners()
  }

  // All ipcRenderer listeners will initialize here
  initListeners(){
    this.electron.on('probeResult', this.getProbeResult.bind(this))
    this.electron.on('paletteResult', this.getPaletteResult.bind(this))
    this.electron.on('thumbnailResult', this.getThumbnailResult.bind(this))
    this.electron.on('export_progress', this.exportProgress.bind(this))
    this.electron.on('export_finished', this.exportFinished.bind(this))
    this.electron.on('export_canceled', this.exportCancelled.bind(this))
  }

  /* Listener function here */
  getProbeResult(event: any, probe: any){
    this.zone.run(()=>{
      const videoStream = probe.streams.find((it: any) => (it.codec_type=='video'));
      if(!videoStream){
        console.error("No video stream found");
        return;
      };
      
      this.frames.max = Number((Math.floor(videoStream.duration * 10) / 10).toFixed(1)) - 0.1;
      this.settings.probe = { 
        duration: videoStream.duration, 
        rate: (videoStream?.r_frame_rate ?? videoStream?.avg_frame_rate)
      }

      this.settings.dimensions.width = videoStream.width
      this.settings.dimensions.height = videoStream.height
      this.settings.dimensions.original_width = videoStream.width
      this.settings.dimensions.original_height = videoStream.height
      this.settings.dimensions.ratio = videoStream.width/videoStream.height

      this.settings.fps = parseInt(this.frameratePipe.transform((videoStream?.r_frame_rate ?? videoStream.avg_frame_rate), true))

      //get thumbnail
      this.refreshPalette();
    })
  }

  getPaletteResult(event: any, paletteResult: any){
    if (this.status.state == 2){
      this.zone.run(()=>{
        this.palette = paletteResult
        this.status.state = 3
      })
      const time = this.frames.current
      const path = this.electron.getPathForFile(this.settings.file.input)
      this.electron.send('getThumbnail', path, this.palette, time, this.settings)
    }
  }

  getThumbnailResult(event: any, thumbnailResult: any){
    this.zone.run(()=>{
      this.thumbnail = `data:image/gif;base64,${thumbnailResult}`
      this.status.state = 5
      this.stored = this.settings
    })
  }

  exportProgress(event: any, progress: any){
    console.log("Progress", progress);
    const p = (progress.sec/this.settings.probe.duration)*100;

    this.zone.run(()=>{
      this.status.export.progress = p;
      this.status.export.size = progress.size;
    })
  }

  exportFinished(event: any, size: any){
    console.log("DONE!");

    this.zone.run(()=>{
      this.status.export.progress = 100;
      this.status.export.size = size;
    })
  }

  exportCancelled(){
    this.zone.run(()=>{
      this.status.export.canceling = false;
      this.exportDone(false);
    })
  }
  /* Listener function here */

  /* Main functionality (these are generally imported from renderer.js) */
  onFileSelected(event: any){
    const file = event.target?.files?.[0]

    if (file){
      this.settings.file.input = file

      const path = this.electron.getPathForFile(file)
      console.log("get file path", path)
      
      this.frames.current = 0
      this.status.state = 1
      this.palette = null
      this.thumbnail = null
      this.settings.color = this.defaults.color

      this.electron.send('probeInput', path)
    }
  }

  refreshPalette(){
    this.thumbnail = null;
    this.palette = null;
    // this.cancel();

    this.status.state = 2;

    const path = this.electron.getPathForFile(this.settings.file.input)

    this.electron.send('getPalette', path, this.settings)
  }

  refreshFps(){
    if (this.stored.fps !== this.settings.fps){
      this.refreshThumbnail()
    }
  }

  cancel(reset?: boolean){
    this.electron.send('cancelProcess', {});
    if(reset){
      this.status.state = 0;
      this.settings.file = { input: null };
      this.palette = null;
      this.thumbnail = null;
    }
  }

  refreshThumbnail(){
    this.thumbnail = null;
    this.status.state = 3;
    const time = this.frames.current;
    const path = this.electron.getPathForFile(this.settings.file.input)
    this.electron.send('getThumbnail', path, this.palette, time, this.settings)
  }

  /* ngModel changes with no interactions with ipcRenderer or main.ts */
  widthChange(){
    console.log("check width change", this.settings.dimensions.width)
    if (this.settings.dimensions.width > 0 && this.settings.dimensions.lock){
      const ratio = this.settings.dimensions.original_height/this.settings.dimensions.original_width;
      this.settings.dimensions.height = Math.round(this.settings.dimensions.width*ratio);
    }
  }

  heightChange(){
    console.log("check height change", this.settings.dimensions.height)
    if (this.settings.dimensions.height > 0 && this.settings.dimensions.lock){
      const ratio = this.settings.dimensions.original_width/this.settings.dimensions.original_height;
      this.settings.dimensions.width = Math.round(this.settings.dimensions.height*ratio);
    }
  }

  refreshDimension(key: string) {
    if (this.stored.dimensions[key] != this.settings.dimensions[key]){
      console.log('diff');

      this.refreshThumbnail();
    }
  }

  frameChanged(){
    console.log("frame changed", this.frames.current)

    if (this.status.state === 5){
      this.refreshThumbnail()
    }
  }

  //export functions
  export(){
    const filePath = this.electron.getPathForFile(this.settings.file.input)
    const output = this.electron.formatPath(filePath, this.settings.exportType)

    //save dialog doesnt work in renderer anymore, we invoke it to the main instead
    this.electron.invoke("save-dialog", output).then((outputFile: any)=>{
      if(outputFile){
        this.status.export = Object.assign({}, this.status.export, { progress: 0, size: 0, path: outputFile });
        this.status.state = 4;

        this.electron.send('exportGif', filePath, outputFile, this.palette, this.settings);
      }
    })
  }

  cancelExport(){
    this.status.export.canceling = true
    this.electron.send('cancel_export', { remove: this.status.export.path })
  }

  exportDone(reset?: boolean){
    if (reset){
      this.status = {
        state: 0,
        export: {
          progress: 0,
          size: 0,
          path: null,
          canceling: false
        }
      }
      this.settings = Object.assign({}, defaultValue)
      this.stored = Object.assign({}, defaultValue)
      this.frames = {
        current: 0,
        min: 0,
        max: 100
      }
      this.palette = null
    } else {
      this.status.state = 5
      this.status.export.progress = 0;
      this.status.export.size = 0;
    }
  }

  reveal(){
    this.electron.showItemInFolder(this.status.export.path)
  }

  preview(){
    const path = this.electron.formatUrl(this.status.export.path)
    window.open(path)
  }

  /* Independently update scope */
}
