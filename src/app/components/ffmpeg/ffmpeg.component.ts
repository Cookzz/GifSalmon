import { Component, NgZone } from "@angular/core";
import { Loader } from "../loader/loader.component";
import { ElectronService } from '../../../service/electron.service';

/* A very simple slider without dependency on third party components */
@Component({
    selector: "ffmpeg",
    templateUrl: './ffmpeg.component.html',
    styleUrl: './ffmpeg.component.css',
    imports: [Loader]
})

export class FfmpegComponent {
    installing: boolean = false

    electron = new ElectronService().getElectron()

    constructor(private zone: NgZone){
        this.electron.on('ffmpeg_installed', this.installedFfmpeg.bind(this))
    }

    installedFfmpeg(){
        this.zone.run(()=>{
            this.installing = false
        })
    }

    ffmpeg_click(){
        this.electron.openExternal("https://ffmpeg.org")
    }

    install(){
        this.installing = true
        this.electron.send("install_ffmpeg", {})
    }

    quit(){
        this.electron.quit()
    }
}