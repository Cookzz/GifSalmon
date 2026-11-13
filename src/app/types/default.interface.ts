export interface Defaults {
    [index: string]: any
    file: FileInput
    probe: Probe
    dimensions: Dimensions
    fps: number
    color: Colors,
    exportType: ExportTypeKeys,
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
    preset: PresetTypesKeys
}

export const ExportTypes = {
  gif: "gif",
  apng: "apng",
  webp: "webp",
  avif: "avif"
} as const

export const PresetTypes = {
  custom: "custom",
  default: "default",
  balanced: "balanced",
  max: "max"
} as const

type ExportTypeKeys = typeof ExportTypes[keyof typeof ExportTypes]
type PresetTypesKeys = typeof PresetTypes[keyof typeof PresetTypes]
