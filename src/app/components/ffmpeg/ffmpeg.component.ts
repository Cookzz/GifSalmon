import { Component, EventEmitter, Input, Output } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { LoaderComponent } from "../loader/loader.component";

/* A very simple slider without dependency on third party components */
@Component({
    selector: "ffmpeg",
    templateUrl: './ffmpeg.component.html',
    styleUrl: './ffmpeg.component.css',
    imports: [LoaderComponent]
})

export class FfmpegComponent {
    installing: boolean = false

    ffmpeg_click(){

    }

    quit(){

    }

    install(){
        
    }
}