## 2.0.0-alpha.2 (2025-12-06)

I will only list the notable ones here

**_Major changes_**
* Updated Electrion (v35.1.5 -> 39.2.6)
* Updated Angular (v19.2.6 -> 21.0.3)
* Updated primeng (v19.1.0 -> 21.0.1)
* Updated jquery to latest version (3.7.1 -> 4.0.0-rc.1)
* Updated jasmine/karma/jest testing to vitest
* Added "got" package override due to conflicts with the electron packager
* Migrated [ffbinaries](https://www.npmjs.com/package/ffbinaries) to [ffbinaries-plus](https://www.npmjs.com/package/ffbinaries-plus) to fix security vulnerability
* [Migrated @primeng/themes to @primeuix/themes](https://primeng.org/migration/v21) due to v12 changes
* [Deprecated zone.js in favor of zoneless and change detection](https://angular.dev/guide/zoneless)
* New feature: WebP support + additional compression options [c13808b](https://github.com/Cookzz/GifSalmon/commit/c13808bd0489f88a9eccecb96ef5c7d1bb01adee)

**_Fixes_**
* fix: forgot to re-add ffmpeg check [07fb0bb](https://github.com/Cookzz/GifSalmon/commit/07fb0bb3b1b4e6caa19455159ffccc82124bcdc4)
* fix: inconsistent styling [1bb1043](https://github.com/Cookzz/GifSalmon/commit/1bb1043f01a8d03c9eb3d3dc4f957bbe46404368)
* fix: file upload doesn't accept video-only [a47c5e7](https://github.com/Cookzz/GifSalmon/commit/a47c5e76b1f3484cabe06d2a89d543f60b29c16b)
* fix: checkbox not properly aligned [540b1a8](https://github.com/Cookzz/GifSalmon/commit/540b1a8893c9d264a25973714a76794d249b6402)
* fix: href breaking pages [1800a39](https://github.com/Cookzz/GifSalmon/commit/1800a3967dde5d8b063ad77b7a145ec05a5f484e)
* fix: (security CVE-2023-28155) change to ffbinaries-plus [bbe3328](https://github.com/Cookzz/GifSalmon/commit/bbe33285f97684fd80534b37d3cb6f946d4f4bb2)
* fix: scroll listener not killed properly [bf5621f](https://github.com/Cookzz/GifSalmon/commit/bf5621f95be083bf3a835945ca6f17d5f2a46528)


## 2.0.0-alpha (2025-04-16)
Subject to change. Some are also missing commit ids because some changes are too scattered or are combined together.

**_Major changes_**
* Migrated to [Angular](https://angular.dev/) from [AngularJS](https://angularjs.org/)
    - Deprecated renderer.js to Angular Components
    - Moved to TypeScript with a mix of `import` and `require(..)`
    - `$scope.<variable>` -> `this.<variable>` (this is the result of moving to components)
    - `ng-click` -> `(click)`
    - `ng-mouseup` -> `(mouseup)`
    - `ng-blur` -> `(blur)`
    - `ng-model` -> `[(ngModel)]` (this also replaces `$scope.watch(..)`)
    - `ng-if` -> `@if (value) else { <element> }`
    - `app.directive(..)` & `app.controller(..)` -> [components](https://github.com/Cookzz/GifSalmon/tree/v2-alpha/src/app/components)
    - `app.filter(..)` -> [pipes](https://github.com/Cookzz/GifSalmon/tree/v2-alpha/src/app/pipes)
    - All electron functions as `ipcRenderer.on(..)` are moved to [preload.ts](https://github.com/Cookzz/GifSalmon/blob/v2-alpha/preload.ts), received in [electron.service.ts](https://github.com/Cookzz/GifSalmon/blob/v2-alpha/src/service/electron.service.ts) to be used

* Deprecated [jquery-minicolors](https://github.com/claviska/jquery-miniColors/) for [primeng](https://github.com/primefaces/primeng) ([b742e52](https://github.com/Cookzz/GifSalmon/commit/b742e5218711639d1ea9f7e775e5e5cc68f184de))
* Changed to page routing to redirect users if they have ffmpeg installed or not ([f2d66d3](https://github.com/Cookzz/GifSalmon/commit/f2d66d3458b2811a981709af0cea208d032927fc))

**_Misc changes_**
* feature: have palette color picker move with scroll event ([2a04eac](https://github.com/Cookzz/GifSalmon/commit/2a04eacb2bfe5e8df2bfa688ba26ced0eebf5e53))
* fix: palette color picker not on top ([8e26290](https://github.com/Cookzz/GifSalmon/commit/8e262909f059ec05da4d94a64e8689d37ea8d14e))

There are a lot of commits and changes but these are the most notable ones to highlight.

## 1.0.9 (2025-03-29)

* upgrade electron to 35.1.2 & fix breaking changes ([ebf1082](https://github.com/Cookzz/GifSalmon/commit/ebf1082619eeee7527f61802cc4cbc50cd5b7533))
* fix: color palette not working ([623282b](https://github.com/Cookzz/GifSalmon/commit/623282b3bd1dd92ecbc767222d2566542c6968b5))
* chore: code cleanup ([10ec31d](https://github.com/Cookzz/GifSalmon/commit/10ec31d0d7f0ef05666444ac746e42954c770e48))


## 1.0.8 (2025-03-24)

The versioning is continuation of [GifTuna's v1.0.7](https://github.com/dudewheresmycode/GifTuna/releases/tag/1.0.7)

**Dependencies**

- Updated electron version (1.6.2 -> 35.0.3)
- Updated AngularJS (1.6.4 -> v1.8.3)
- Updated ffbinaries (1.0.1) to ffbinaries-plus (1.2.4, request deprecated for axios)
- Updated fix-path (2.1.0 -> 4.0.0)
- Updated jQuery (3.2.1 -> 3.7.1)
- Updated lowdb (0.16.2 -> 7.0.1)
- Updated which (1.2.14 -> 5.0.0)
- Deprecated electron-packager to @electron/packager

**Changes**

- Fixed breaking changes from dependency upgrade in main.js
- Minor changes to imports
- openDialog is moved to main.js due to electron breaking change [solution from stack overflow](https://stackoverflow.com/questions/36637201/requiring-electron-dialog-from-render-process-is-undefined)