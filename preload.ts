import { contextBridge, ipcRenderer, webUtils } from 'electron';
import { parse, join } from 'path'

/* Additional functions */
const formatPath = (path: any) => {
    const base = parse(path);
    return join(base.dir, base.name+'.gif');
}

/* This acts as a bridge between main and renderer on electron, basically "exposing" the electron library to the renderer */
contextBridge.exposeInMainWorld('electron', {
    send: (channel: string, ...data: any) => ipcRenderer.send(channel, ...data),
    invoke: (channel: string, data?: any) => ipcRenderer.invoke(channel, data),
    on: (eventName: string, callback: any) => ipcRenderer.on(eventName, callback),
    getPathForFile: (file: any) => webUtils.getPathForFile(file),
    formatPath: formatPath
});