import { 
    Component, 
    EventEmitter, 
    Input, 
    OnChanges, 
    Output, 
    SimpleChanges, 
    ViewChild,
    ElementRef,
    Renderer2
} from "@angular/core";
import { FormsModule } from "@angular/forms";
import { CommonModule } from '@angular/common';
import { ColorPickerModule } from 'primeng/colorpicker';

@Component({
    selector: "pixel-palette",
    templateUrl: './palette.component.html',
    styleUrl: './palette.component.css',
    imports: [FormsModule, CommonModule, ColorPickerModule]
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
    stored: any = null
    startedIn: boolean = false
    rgb: any = {}

    scrollListener: any = null;

    @ViewChild("pixelpicker", { read: ElementRef }) picker?: ElementRef;

    constructor(private renderer: Renderer2){}

    ngOnChanges(changes: SimpleChanges): void {
        if (changes?.['palette'] && (changes?.['palette']?.currentValue !== changes?.['palette']?.previousValue)){
            this.read_palette(changes['palette'].currentValue)
        }
    }

    pickMe(i: number, e: MouseEvent){
        e.preventDefault();
        e.stopPropagation();

        const color = this.pixels[i];
        this.rgb = this.rgb_to_obj(color[0], color[1], color[2])
        this.stored = color;
        this.pixelIndex = i;
        this.hexValue = this.rgb_to_hex(color[0], color[1], color[2]);

        if (e.target){
            const p = $(e.target).offset()
            const top = p?.top ?? 0
            const left = p?.left ?? 0

            this.renderer.appendChild(document.body, this.picker?.nativeElement)

            //this helps the palette color picker scroll with the selected palette
            this.setOrRefreshScrollListener(top)

            this.renderer.setStyle(this.picker?.nativeElement, 'top', `${top}px`)
            this.renderer.setStyle(this.picker?.nativeElement, 'left', `${left}px`)
            this.renderer.addClass(this.picker?.nativeElement, 'open')
        }
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

    /* picker-actions functions */
    set(){
        this.renderer.removeClass(this.picker?.nativeElement, 'open')
        this.killScrollListener()
        this.pixels[this.pixelIndex] = [this.rgb.r, this.rgb.g, this.rgb.b];
        this.pixelIndex = null;
        this.put_palette();
        this.rgb = {};
    }

    close(){
        this.renderer.removeClass(this.picker?.nativeElement, 'open')
        this.killScrollListener()
        this.pixelIndex = null
        this.rgb = {}
    }

    /* pixel-picker function */
    stopPropogation(e: MouseEvent){
        e.stopPropagation()
    }

    start(){
        this.startedIn = true
    }

    onColorPickerChanged(){
        this.hexValue = this.rgb_to_hex(this.rgb.r, this.rgb.g, this.rgb.b)
    }

    /* RGB conversion */
    rgb_to_hex(r: number, g: number, b: number){
        let bin = r << 16 | g << 8 | b;
        return ((h) => {
          return new Array(7-h.length).join("0")+h
        })(bin.toString(16).toUpperCase())
    }

    rgb_to_obj(r: number, g: number, b: number){
        return { r, g, b }
    }

    /* Scroll listener for palette color picker */
    setOrRefreshScrollListener(top: number){
        //scroll listener will duplicate itself without doing this, you HAVE to turn it off first if it was ran once before
        this.killScrollListener()

        const currentScrollTop = $(document.body).find('div.settings-form').scrollTop() ?? 0
        this.scrollListener = $(document.body).find('div.settings-form').on('scroll', ()=>{
            const scrollTop = ($(document.body).find('div.settings-form').scrollTop() ?? 0) - currentScrollTop
            this.renderer.setStyle(this.picker?.nativeElement, 'top', `${top - scrollTop}px`)
        })
    }

    /* Destroy the listener when it has changed target or closed */
    killScrollListener(){
        if (this.scrollListener){
            this.scrollListener = null
            $(document.body).find('div.settings-form').off('scroll')
        }
    }
}