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

## What problems does it solve?
You might wonder why I'd spend the time forking, developing and migrating this application to 2025 standards. Well, there's a few reasons.

1. Web-based solutions exists but I don't know what's going on behind the scenes
2. No **watermarks**, **file size limits** or **upload limits** (x amount of converts per day). Most solutions online have one of the 3, or all. I don't like that. Let me convert whatever, whenever and however I please.
3. Open source solutions mostly requires the use of CLI. I am a dev but sometimes I just want UI/UX for things instead of, you know, running commands
4. No internet required to use (except downloading ffmpeg)

## Tested Platforms

- [x] Windows 11 24H2
- [ ] Linux
- [ ] MacOS