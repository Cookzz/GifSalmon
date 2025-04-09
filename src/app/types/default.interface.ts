export interface Defaults {
    [index: string]: any
    file: FileInput
    probe: Probe
    dimensions: Dimensions
    fps: number
    color: Colors
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