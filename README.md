# GifSalmon
A gif tuning application for Mac, Windows and Linux.
![Imgur](https://i.imgur.com/1dZGjtM.png)
An updated version of [GifTuna](https://github.com/dudewheresmycode/GifTuna) for 2025.

## What has changed

* Updated to the latest electron version & packager
* Updated to the latest dependencies
* Fixed breaking changes from dependency upgrades
* Ensure it can run, compile and function, at least in Windows 11 environment

## What has not changed

* 99% of the codebase. I have only updated all of the core and necessary lines that will make this function the same as before
* AngularJS (have not upgraded to Angular.io yet)
* No bug fixes were done (yet)

## Plans

* Update to Angular.io and deprecate AngularJS
* Re-writing of the entire application but will keep the core functionality, appearance, etc. the same the best I can.
* Bug fixes
* Auto-download of ffmpeg instead of prompting user to do it instead

## What is in progress

* Slowly migrating AngularJS to Angular v19 on a separate folder for testing, may take up to a month for anything to be ready
* Maintaining and cleaning up current codebase but will not do any drastic changes in preparation for version 2.0.0
* Fix bugs if there is any found

## Compression

Done my fair share of research but ffmpeg alone will not be able to properly reduce the size of the gif if you're using its default settings and would require even more external dependencies like [gifsicle](https://github.com/imagemin/gifsicle-bin) or [gifski](https://github.com/ImageOptim/gifski) but because of how it will require multiple pipes/stream from ffmpeg -> export as .gif first -> pipe it into gifsicle or ffmpeg -> generate frames -> stream it into gifski, adding compression to reduce the gif's file size is a lot more harder than I expected without compromising the current UI/UX.

Currently, with the current ffmpeg's limitations and the [inefficiency of .gif](https://askubuntu.com/questions/1297372/shorten-a-gif-file-with-ffmpeg) & [that gifs will always be bigger than its mp4 version](https://www.reddit.com/r/explainlikeimfive/comments/ttawly/eli5_why_are_mp4_files_so_much_smaller_than_gif/), there is a few ways you can reduce the exported gif size:

1. Enable & reduce dither grid
2. Use "Frame Difference" palette mode
3. Reduce color palette (128 if you want to strike a balance)
4. Reduce fps (the recommended one i see only is between 12-14fps)
5. Make the gif smaller in dimensions (lower the scale)

Meanwhile, I will try my best to have a built-in compression ready for version 2.0.0.

## Why

Because I'm actively using it daily with a few friends and there are some issues that I am very annoyed by and want to fix it but the main repo is already 8 years old and doesn't work/compile with my current dev env & tools when trying to set it up.
