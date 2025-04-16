## 2.0.0-alpha (2025-04-16)
Subject to change. Some are also missing commit ids because some changes are too scattered or are combined together.

**_Major changes_**
* Migrated to [Angular](https://angular.dev/) from [AngularJS](https://angularjs.org/)
    - Deprecated renderer.js to Angular Components
    - Moved to TypeScript with a mix of `import` and `require(..)`
    - `ng-click` -> `(click)`
    - `ng-mouseup` -> `(mouseup)`
    - `ng-blur` -> `(blur)`
    - `ng-model` -> `[(ngModel)]` (this also replaces `$scope.watch(..)`)
    - `ng-if` -> `@if (value) else { <element> }`
    - `app.directive(..)` -> [components](https://github.com/Cookzz/GifSalmon/tree/v2-alpha/src/app/components)
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
- Updated ffbinaries (1.0.1 -> 1.1.6)
- Updated fix-path (2.1.0 -> 4.0.0)
- Updated jQuery (3.2.1 -> 3.7.1)
- Updated lowdb (0.16.2 -> 7.0.1)
- Updated which (1.2.14 -> 5.0.0)
- Deprecated electron-packager to @electron/packager

**Changes**

- Fixed breaking changes from dependency upgrade in main.js
- Minor changes to imports
- openDialog is moved to main.js due to electron breaking change [solution from stack overflow](https://stackoverflow.com/questions/36637201/requiring-electron-dialog-from-render-process-is-undefined)