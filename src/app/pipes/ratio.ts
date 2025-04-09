import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'ratio',
})
export class RatioPipe implements PipeTransform {
  transform(x: number): string {
    const tolerance = 1.0E-6;
    let h1=1;
    let h2=0;
    let k1=0; 
    let k2=1;

    let b = x;
    do {
        let a = Math.floor(b);
        let aux = h1; h1 = a*h1+h2; h2 = aux;
        aux = k1; k1 = a*k1+k2; k2 = aux;
        b = 1/(b-a);
    } while (Math.abs(x-h1/k1) > x*tolerance);

    return h1+":"+k1;
  }
}