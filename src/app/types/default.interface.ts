export interface Defaults {
    [index: string]: any
    file: FileInput
    probe: Probe
    dimensions: Dimensions
    fps: number
    color: Colors,
    exportType: ExportTypes,
    export: ExportSettings
}

interface FileInput {
    input: File | null
}

interface Probe {
    [index: string]: any
    duration: any
    rate: any
}

interface Dimensions {
    [index: string]: number | boolean
    original_width: number
    original_height: number
    width: number
    height: number
    lock: boolean
    ratio: number
}

interface Colors {
    [index: string]: any
    stats_mode: string
    diff_mode: string
    dither: boolean
    count: number
    dither_scale: number
    alpha: boolean
}

interface ExportSettings {
    webp: WebPSettings
}

interface WebPSettings {
    lossless: boolean //0 or 1
    compression_level: number
    quality: number
    loop: boolean //0 or 1
    preset: PresetTypes
}

export enum PresetTypes {
    custom = "custom",
    default = "default",
    balanced = "balanced",
    max = "max"
}

export enum ExportTypes {
    gif = "gif",
    apng = "apng",
    webp = "webp",
    avif = "avif"
}