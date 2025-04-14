# GifSalmon
A gif tuning application for Mac, Windows and Linux.
![Imgur](https://i.imgur.com/1dZGjtM.png)
An updated version of [GifTuna](https://github.com/dudewheresmycode/GifTuna) for 2025.

# Alpha Development
No alpha releases/binaries will be provided until migration is completed.

Everything here including README is subject to change

## Progress

* Latest version of electron & dependencies as of April 2025
* Migrating to [Angular](https://angular.dev/) from [AngularJS](https://angularjs.org/)
* Fixing breaking changes & keeping as much of the original functionality during migration

## Plans

* Support for additional compression with either [gifsicle](https://github.com/imagemin/gifsicle-bin) or [gifski](https://github.com/ImageOptim/gifski)
* Support for multiple animated image formats: [.apng](https://en.wikipedia.org/wiki/APNG), [.webp](https://en.wikipedia.org/wiki/WebP) and [.avif](https://en.wikipedia.org/wiki/AVIF) aside from .gif
* Gif trimming - export gif from X to Y seconds
* Reduce or if possible, remove dependency from JQuery and use angular's provided modules for DOM manipulation
* Switch to Tauri

## Tested Platforms

- [x] Windows 11 24H2
- [ ] Linux
- [ ] MacOS