import { contextBridge, ipcRenderer, webUtils, shell, app } from 'electron';
import { parse, join } from 'path'
import { format } from 'url'

/* Additional functions */
const formatPath = (path: any) => {
    const base = parse(path);
    return join(base.dir, base.name+'.gif');
}

const formatUrl = (path: string) => {
    return format({
        pathname: path,
        protocol: 'file',
        slashes: true
    })
}

/* This acts as a bridge between main and renderer on electron, basically "exposing" the electron library to the renderer */
contextBridge.exposeInMainWorld('electron', {
    send: (channel: string, ...data: any) => ipcRenderer.send(channel, ...data),
    invoke: (channel: string, data?: any) => ipcRenderer.invoke(channel, data),
    on: (eventName: string, callback: any) => ipcRenderer.on(eventName, callback),
    getPathForFile: (file: any) => webUtils.getPathForFile(file),
    showItemInFolder: (path: string) => shell.showItemInFolder(path),
    openExternal: (str: string) => shell.openExternal(str),
    quit: () => app.quit(),
    formatPath: formatPath,
    formatUrl: formatUrl
});