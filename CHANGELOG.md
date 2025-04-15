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