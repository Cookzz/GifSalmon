import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { CommonModule } from '@angular/common';

/* A very simple slider without dependency on third party components */
@Component({
    selector: "pixel-palette",
    templateUrl: './palette.component.html',
    styleUrl: '../../css/style.css',
    imports: [FormsModule, CommonModule]
})

export class PixelPalette implements OnChanges {
    @Input()
    maxColors: number = 256

    @Input()
    palette: any;

    @Output()
    paletteChanged: EventEmitter<any> = new EventEmitter<any>()

    pixels: any[] = []
    pixelIndex: any = null;
    hexValue: any = null

    picker: any = $().find('.pixel-picker')
    mini: any = $().find('input.pixel-mini-colors')

    ngOnChanges(changes: SimpleChanges): void {
        if (changes?.['palette'] && (changes?.['palette']?.currentValue !== changes?.['palette']?.previousValue)){
            this.read_palette(changes['palette'].currentValue)
        }
    }

    pickMe(i: number, e: MouseEvent){
        e.preventDefault();
        e.stopPropagation();
        
        const color = this.pixels[i];
        this.mini.minicolors('value', `rgb(${color.join(',')})`);
        // scope.stored = color;
        this.pixelIndex = i;
        this.hexValue = this.rgb_to_hex(color[0], color[1], color[2]);

        if (e.target){
            const p = $(e.target).offset()
        }
        // $picker.css('top',p.top+'px').css('left',p.left+'px').addClass('open');
    }

    put_palette(){
        if (this.pixels.length > 0){
          const canvas_copy = document.createElement('canvas');
          const ctx_copy = canvas_copy.getContext('2d');

            if (ctx_copy){
                const idata = ctx_copy.getImageData(0,0,16,16);

                for (let i = 0;i < idata.data.length; i+=4) {
                    const pixel = Math.floor(i/4);
                    idata.data[i] = this.pixels[pixel] ? this.pixels[pixel][0] : 0;
                    idata.data[i+1] = this.pixels[pixel] ? this.pixels[pixel][1] : 0;
                    idata.data[i+2] = this.pixels[pixel] ? this.pixels[pixel][2] : 0;
                    idata.data[i+3] = 255;
                }
                // for(var i=0;i<scope.pixels.length;i++){
                //   data[i] = scope.pixels[i][0] || 0;
                //   data[i+1] = scope.pixels[i][1] || 0;
                //   data[i+2] = scope.pixels[i][2] || 0;
                //   data[i+3] = 255;
                // }
                // console.log(data);
                ctx_copy.putImageData(idata,0,0);

                const uri = canvas_copy.toDataURL("image/png").split('base64,')[1];
                this.palette = uri;
            }
        }
    }

    rgb_to_hex(r: number, g: number, b: number){
        let bin = r << 16 | g << 8 | b;
        return ((h) => {
          return new Array(7-h.length).join("0")+h
        })(bin.toString(16).toUpperCase())
    }

    read_palette(img_src: any){
        let img = new Image();
        img.src = 'data:image/png;base64,'+img_src;

        img.onload = () => {
          const canvas = document.createElement('canvas');
          canvas.width = img.width;
          canvas.height = img.height;
          const ctx = canvas.getContext('2d');
          
          if (ctx){
            ctx.drawImage(img, 0, 0);
          
            const pixel_data = ctx.getImageData(0, 0, canvas.width, canvas.height);
            const pixel_array = [];

            for (let i=0; i < pixel_data.data.length; i+=4){
                let p = [];
                p[0] = pixel_data.data[i];
                p[1] = pixel_data.data[i+1];
                p[2] = pixel_data.data[i+2];
                p[3] = 255;
                if(pixel_array.length < this.maxColors)
                pixel_array.push(p);
            }
            this.pixels = pixel_array
          }
        }
    }

    color(c: any){
        return {
            'background-color': `rgb(${c[0]},${c[1]},${c[2]})`
        }
    }
}