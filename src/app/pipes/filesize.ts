import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'fileSize',
})
export class FileSizePipe implements PipeTransform {
  transform(size: number): string {
    let i: number = Math.floor( Math.log(size) / Math.log(1000) );

    return (size > 0) ? `${Number(Number(size/Math.pow(1000, i)).toFixed(2))*1} ${['B', 'kB', 'MB', 'GB', 'TB'][i]}` :
                        '-'
  }
}