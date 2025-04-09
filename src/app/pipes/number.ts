import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'number',
})
export class NumberPipe implements PipeTransform {
  transform(str: string | number): number {
    if (str === 0){
        return 1
    }
    return Number(str)
  }
}