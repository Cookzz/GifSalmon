import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'framerate',
})
export class FrameratePipe implements PipeTransform {
  transform(input: any, suffix: boolean): any {
    if(!input){
        return '-';
    }
    
    const fr = input.split('/');
    const r = (parseInt(fr[0])/parseInt(fr[1]));
    if (suffix) {
        return `${r} fps`;
    }
    return r;
  }
}