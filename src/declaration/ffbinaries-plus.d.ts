declare module 'ffbinaries-plus' {
    export type PlatformCode =
        | 'osx-64'
        | 'linux-32'
        | 'linux-64'
        | 'linux-armel'
        | 'linux-armhf'
        | 'linux-arm64'
        | 'windows-32'
        | 'windows-64';

    export type Component = 'ffmpeg' | 'ffprobe' | 'ffplay' | 'ffserver';

    export interface OsInfo {
        type: string;
        arch: string;
    }

    export interface DownloadOptions {
        destination?: string;
        platform?: string;
        version?: string;
        force?: boolean;
        quiet?: boolean;
        tickerFn?: (tickData: { filename: string; progress: number }) => void;
        tickerInterval?: number;
    }

    export interface DownloadResult {
        filename: string;
        path: string;
        size?: string;
        status: string;
        code: 'FILE_EXISTS' | 'DONE_FROM_CACHE' | 'DONE_CLEAN';
    }

    export type DownloadCallback = (err: string | null, results: DownloadResult[]) => void;

    export interface VersionData {
        version: string;
        bin: Partial<Record<PlatformCode, Partial<Record<Component, string>>>>;
        [key: string]: unknown;
    }

    export interface LocatedBinary {
        found: boolean;
        isExecutable: boolean;
        isSystem: boolean;
        path: string | null;
        version: string | null;
    }

    export interface LocateOptions {
        paths?: string | string[];
        ensureExecutable?: boolean;
    }

    export function downloadBinaries(callback: DownloadCallback): void;
    export function downloadBinaries(opts: DownloadOptions, callback: DownloadCallback): void;
    export function downloadBinaries(components: Component | Component[] | null, callback: DownloadCallback): void;
    export function downloadBinaries(
        components: Component | Component[] | null,
        opts: DownloadOptions,
        callback: DownloadCallback
    ): void;

    export const downloadFiles: typeof downloadBinaries;

    export function locateBinariesSync(
        components: Component | Component[],
        opts?: LocateOptions
    ): Partial<Record<Component, LocatedBinary>>;

    export function getVersionData(
        version: string | undefined,
        callback: (err: string | null, data?: VersionData) => void
    ): void;

    export function listVersions(callback: (err: string | null, versions: string[]) => void): void;

    export function listPlatforms(): PlatformCode[];

    export function detectPlatform(osinfo?: OsInfo): PlatformCode | null;

    export function resolvePlatform(input: string): PlatformCode | null;

    export function getBinaryFilename(component: Component, platform: string): string;

    export function clearCache(): void;
}
