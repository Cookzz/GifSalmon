import { Component, EventEmitter, Input, Output } from "@angular/core";
import { FormsModule } from "@angular/forms";

/* A very simple slider without dependency on third party components */
@Component({
    selector: "frame-scrubber",
    templateUrl: './frame-scrubber.component.html',
    styleUrl: './frame-scrubber.component.css',
    imports: [FormsModule]
})

export class FrameScrubber {
    @Input()
    maxFrames: any;

    @Input()
    currentFrames: any;

    @Input()
    disabled: boolean = false;

    @Output()
    maxFramesChange: EventEmitter<any> = new EventEmitter<any>()

    @Output()
    currentFramesChange: EventEmitter<any> = new EventEmitter<any>()

    //if i want to check for changed slider value separately
    @Output()
    framesChanged: EventEmitter<any> = new EventEmitter<any>()

    params = { value: 0, max: 0 }

    ngOnChanges(){
        if (this.maxFrames){
            this.params.max = this.maxFrames
        }
        if (this.currentFrames){
            this.params.value = this.currentFrames
        }
    }

    sliderChange(){
        this.framesChanged.emit(this.params)
        this.currentFramesChange.emit(this.currentFrames)
        this.maxFramesChange.emit(this.maxFrames)
    }
}