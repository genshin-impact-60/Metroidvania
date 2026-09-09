"""Chroma-green interact gens -> rembg RGBA singles + Unity .meta."""
from __future__ import annotations

import secrets
import shutil
import uuid
from pathlib import Path

import numpy as np
from PIL import Image
from rembg import new_session, remove
from scipy import ndimage

SRC = Path(r"C:\Users\21372\.cursor\projects\d-game-Metroidvania\assets")
OUT = Path(r"D:\game\Metroidvania\Assets\Art\Environment\Interact")
WORK = Path(r"D:\game\Metroidvania\Tools\_interact_cutout_work")
RAW_ROOT = WORK

ITEMS = [
    ("map_interact_chest", "chest_closed"),
    ("map_interact_chest", "chest_open"),
    ("map_interact_chest", "chest_ornate"),
    ("map_interact_switches", "switch_pressure_up"),
    ("map_interact_switches", "switch_pressure_down"),
    ("map_interact_switches", "switch_lever_up"),
    ("map_interact_switches", "switch_lever_pulled"),
    ("map_interact_switches", "switch_seal_off"),
    ("map_interact_switches", "switch_seal_on"),
    ("map_interact_switches", "switch_valve_idle"),
    ("map_interact_switches", "switch_valve_turned"),
    ("map_interact_save_cathedral", "save_cathedral_off"),
    ("map_interact_save_cathedral", "save_cathedral_on"),
    ("map_interact_save_cavern", "save_cavern_off"),
    ("map_interact_save_cavern", "save_cavern_on"),
    ("map_interact_save_clocktower", "save_clocktower_off"),
    ("map_interact_save_clocktower", "save_clocktower_on"),
    ("map_interact_save_crypt", "save_crypt_off"),
    ("map_interact_save_crypt", "save_crypt_on"),
    ("map_interact_save_forest", "save_forest_off"),
    ("map_interact_save_forest", "save_forest_on"),
]

META_TMPL = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable:
  - first:
      213: {iid}
    second: {name}
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 9
  spritePivot: {{x: 0.5, y: 0.0}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites:
    - serializedVersion: 2
      name: {name}
      rect:
        serializedVersion: 2
        x: 0
        y: 0
        width: {w}
        height: {h}
      alignment: 9
      pivot: {{x: 0.5, y: 0.0}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: {sprite_id}
      internalID: {iid}
      vertices: []
      indices: 
      edges: []
      weights: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable:
      {name}: {iid}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def black_to_green(im: Image.Image) -> Image.Image:
    arr = np.array(im.convert("RGB"), dtype=np.int16)
    r, g, b = arr[:, :, 0], arr[:, :, 1], arr[:, :, 2]
    dark = (r < 28) & (g < 28) & (b < 28)
    mag = (r > 60) & (b > 40) & (g < r * 0.75) & ((r - g) > 25) & (r + b > g * 2)
    mask = dark | mag
    labeled, _n = ndimage.label(mask)
    border = set()
    border.update(labeled[0, :].tolist())
    border.update(labeled[-1, :].tolist())
    border.update(labeled[:, 0].tolist())
    border.update(labeled[:, -1].tolist())
    border.discard(0)
    keep = np.isin(labeled, list(border))
    out = arr.copy()
    out[keep] = (0, 255, 0)
    return Image.fromarray(out.astype(np.uint8), "RGB")


def green_frac(im: Image.Image) -> float:
    a = np.array(im.convert("RGB"))
    r, g, b = a[:, :, 0].astype(int), a[:, :, 1].astype(int), a[:, :, 2].astype(int)
    return float(((g > 180) & (g > r + 40) & (g > b + 40)).mean())


def despill(im: Image.Image) -> Image.Image:
    arr = np.array(im.convert("RGBA"), dtype=np.float32)
    rgb = arr[:, :, :3]
    a = arr[:, :, 3]
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    pure = (g > 200) & (r < 100) & (b < 100)
    a[pure] = 0
    edge = (a > 5) & (a < 230) & (g > r + 18) & (g > b + 18) & (g > 90)
    fix = (r + b) * 0.5
    rgb[edge, 1] = np.minimum(g[edge], fix[edge] + 10)
    a[(a < 200) & (g > r + 35) & (g > b + 35) & (r < 90) & (b < 90)] = 0
    mag = (r > 55) & (b > 40) & (g < r * 0.8) & ((r - g) > 22) & (a < 220)
    a[mag] = 0
    a[a < 10] = 0
    opaque = a > 10
    dil = ndimage.binary_dilation(~opaque, iterations=2)
    edge_green = (g > r + 25) & (g > b + 25) & (r < 110) & (b < 110) & dil & opaque
    a[edge_green] = 0
    arr[:, :, :3] = rgb
    arr[:, :, 3] = a
    return Image.fromarray(arr.astype(np.uint8), "RGBA")


def crop_alpha(im: Image.Image, pad: int = 4) -> Image.Image:
    a = np.array(im)[:, :, 3]
    ys, xs = np.where(a > 10)
    if len(xs) == 0:
        return im
    x0 = max(0, int(xs.min()) - pad)
    x1 = min(im.width, int(xs.max()) + 1 + pad)
    y0 = max(0, int(ys.min()) - pad)
    y1 = min(im.height, int(ys.max()) + 1 + pad)
    return im.crop((x0, y0, x1, y1))


def write_meta(name: str, w: int, h: int) -> None:
    meta_path = OUT / f"{name}.png.meta"
    if meta_path.exists():
        return
    guid = uuid.uuid4().hex
    iid = -secrets.randbelow(2**62) if secrets.randbits(1) else secrets.randbelow(2**62)
    sprite_id = uuid.uuid4().hex
    meta_path.write_text(
        META_TMPL.format(guid=guid, iid=iid, name=name, sprite_id=sprite_id, w=w, h=h),
        encoding="utf-8",
    )


def process_one(sheet: str, name: str, session) -> None:
    green_path = SRC / f"{name}_green.png"
    raw_path = RAW_ROOT / sheet / f"{name}_raw.png"
    archive = WORK / "green" / f"{name}_green.png"
    archive.parent.mkdir(parents=True, exist_ok=True)

    use_fallback = False
    if green_path.exists():
        shutil.copy2(green_path, archive)
        im = Image.open(green_path).convert("RGB")
        if green_frac(im) < 0.22:
            use_fallback = True
            print(f"  {name}: green weak ({green_frac(im):.2f}) -> raw fallback")
    else:
        use_fallback = True
        print(f"  {name}: missing green -> raw fallback")

    if use_fallback:
        im = black_to_green(Image.open(raw_path))
    else:
        im = black_to_green(im)

    pad = 40
    canvas = Image.new("RGB", (im.width + pad * 2, im.height + pad * 2), (0, 255, 0))
    canvas.paste(im, (pad, pad))
    cut = remove(canvas, session=session)
    cut = despill(cut)
    cut = crop_alpha(cut)
    dest = OUT / f"{name}.png"
    cut.save(dest)
    write_meta(name, cut.width, cut.height)
    a = np.array(cut)
    r, g, b, al = a[:, :, 0].astype(int), a[:, :, 1].astype(int), a[:, :, 2].astype(int), a[:, :, 3]
    green = int(((g > r + 30) & (g > b + 30) & (al > 10) & (r < 100)).sum())
    print(f"{name}: {cut.size} green_left={green}")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    session = new_session("birefnet-general-lite")
    for sheet, name in ITEMS:
        process_one(sheet, name, session)


if __name__ == "__main__":
    main()
