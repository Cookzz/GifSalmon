import { Routes } from '@angular/router';
import { FfmpegComponent } from './components/ffmpeg/ffmpeg.component';
import { HomeComponent } from './components/home/home.component';

export const routes: Routes = [
    { path: '', component: HomeComponent },
    { path: 'download', component: FfmpegComponent }
];