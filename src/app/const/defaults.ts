import { Defaults, ExportTypes, PresetTypes } from "../types/default.interface";

export const defaultValue: Defaults = {
  file: { input: null },
  probe: { duration: 0, rate: 0 },
  dimensions: {
    original_width: 320,
    original_height: 240,
    width: 320,
    height: 240,
    lock: true,
    ratio: 240/320
  },
  fps: 24,
  color: {
    stats_mode: "diff",
    diff_mode: "rectangle",
    dither: true,
    count: 256,
    dither_scale: 3,
    alpha: false
  },
  exportType: ExportTypes.webp,
  export: {
    webp: {
      lossless: false,
      compression_level: 4,
      quality: 70,
      loop: true,
      preset: PresetTypes.default,
    }
  }
}