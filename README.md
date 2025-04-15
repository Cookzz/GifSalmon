# GifSalmon
A gif tuning application for Mac, Windows and Linux.
![Imgur](https://i.imgur.com/1dZGjtM.png)
An updated version of [GifTuna](https://github.com/dudewheresmycode/GifTuna) for 2025.

# Alpha Development
No alpha releases/binaries will be provided until migration is completed.

Everything here including README is subject to change

## Changes

* Latest version of electron & dependencies
* Migrated to [Angular](https://angular.dev/) from [AngularJS](https://angularjs.org/)

Refer to CHANGELOG for additional details.

## Plans

* Support for additional compression with either [gifsicle](https://github.com/imagemin/gifsicle-bin) or [gifski](https://github.com/ImageOptim/gifski)
* Support for multiple animated image formats: [.apng](https://en.wikipedia.org/wiki/APNG), [.webp](https://en.wikipedia.org/wiki/WebP) and [.avif](https://en.wikipedia.org/wiki/AVIF) aside from .gif
* Gif trimming - export gif from X to Y seconds
* Reduce or if possible, remove dependency from JQuery and use angular's provided modules for DOM manipulation
* Auto-download of ffmpeg instead of prompting user to do it instead
* Switch to Tauri (Plan: approx. June - July 2025)

## Tested Platforms

- [x] Windows 11 24H2
- [ ] Linux
- [ ] MacOS