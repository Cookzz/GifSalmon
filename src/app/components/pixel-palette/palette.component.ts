import { Component, EventEmitter, Input, Output } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { CommonModule } from '@angular/common';

/* A very simple slider without dependency on third party components */
@Component({
    selector: "pixel-palette",
    templateUrl: './palette.component.html',
    styleUrl: '../../css/style.css',
    imports: [FormsModule, CommonModule]
})

export class PixelPalette {}