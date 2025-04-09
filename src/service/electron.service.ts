import { Injectable } from '@angular/core';

/* Here, we "inject" pre-loaded electron renderer functions from preload.ts */
@Injectable({
  providedIn: 'root'
})
export class ElectronService {

  constructor() { }

  getElectron(){
    return (<any>window).electron;
  }
}