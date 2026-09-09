"""Split biome tileset sheets into one RGBA PNG per item + Unity .meta.

Sheets already have transparent backgrounds — crop by existing alpha only.
Do NOT key near-black RGB (interior stone cracks are intentionally dark).
"""
from __future__ import annotations

import hashlib
import re
import uuid
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = Path(r"D:\game\Metroidvania\Assets\Art\Environment\Tilesets")

# closing iterations to bridge thin danglers within one item
SHEETS = {
    "cavern": 0,
    "forest": 2,
    "clocktower": 0,
    "crypt": 4,
}

ALPHA_FG = 12
MIN_AREA = 800
PAD = 2
PPU = 256

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
  alignment: 7
  spritePivot: {{x: 0.5, y: 0}}
  spritePixelsToUnits: {ppu}
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
    textureCompression: 0
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
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: {sprite_id}
    internalID: {iid}
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable:
      {name}: {iid}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def extract_items(im: Image.Image, close_iter: int) -> list[Image.Image]:
    arr = np.array(im.convert("RGBA"))
    fg = arr[:, :, 3] > ALPHA_FG
    if close_iter > 0:
        struct = ndimage.generate_binary_structure(2, 2)
        closed = ndimage.binary_closing(fg, structure=struct, iterations=close_iter)
    else:
        closed = fg
    labeled, n = ndimage.label(closed)
    items: list[tuple[int, float, Image.Image]] = []
    h, w = fg.shape
    for i in range(1, n + 1):
        comp = labeled == i
        area = int((comp & fg).sum())
        if area < MIN_AREA:
            continue
        ys, xs = np.where(comp)
        x0 = max(0, int(xs.min()) - PAD)
        x1 = min(w, int(xs.max()) + 1 + PAD)
        y0 = max(0, int(ys.min()) - PAD)
        y1 = min(h, int(ys.max()) + 1 + PAD)
        crop = arr[y0:y1, x0:x1].copy()
        # drop neighboring sheet items; keep original alpha inside this component
        local = labeled[y0:y1, x0:x1] == i
        crop[~local, 3] = 0
        cx = (x0 + x1) * 0.5
        items.append((y0, cx, Image.fromarray(crop, "RGBA")))
    items.sort(key=lambda t: (round(t[0] / 50), t[1]))
    return [im for _, _, im in items]


def write_meta(png: Path, name: str, ppu: int = PPU) -> None:
    meta = png.with_suffix(".png.meta")
    guid = None
    sprite_id = None
    iid = None
    if meta.exists():
        text = meta.read_text(encoding="utf-8")
        m = re.search(r"^guid: ([a-f0-9]+)", text, re.M)
        if m:
            guid = m.group(1)
        m = re.search(r"spriteID: ([a-f0-9]+)", text)
        if m:
            sprite_id = m.group(1)
        m = re.search(r"internalID: (-?\d+)", text)
        if m and m.group(1) != "0":
            iid = int(m.group(1))
    if not guid:
        guid = hashlib.md5(str(png).replace("\\", "/").encode()).hexdigest()
    if iid is None:
        iid = abs(int(hashlib.md5(name.encode()).hexdigest()[:15], 16)) % (10**16)
        if iid > 2**62:
            iid %= 2**62
    if not sprite_id:
        sprite_id = uuid.uuid4().hex
    meta.write_text(
        META_TMPL.format(guid=guid, iid=iid, name=name, ppu=ppu, sprite_id=sprite_id),
        encoding="utf-8",
    )


def process_biome(biome: str, close_iter: int) -> None:
    src = ROOT / f"map_tileset_{biome}.png"
    out_dir = ROOT / biome
    out_dir.mkdir(parents=True, exist_ok=True)
    folder_meta = ROOT / f"{biome}.meta"
    if not folder_meta.exists():
        folder_meta.write_text(
            f"""fileFormatVersion: 2
guid: {hashlib.md5(f'tileset_folder_{biome}'.encode()).hexdigest()}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""",
            encoding="utf-8",
        )

    items = extract_items(Image.open(src), close_iter)
    print(f"{biome}: {len(items)} items from {src.name}")
    for i, tile in enumerate(items):
        name = f"map_tileset_{biome}_{i}"
        dest = out_dir / f"{name}.png"
        tile.save(dest)
        write_meta(dest, name)
        a = np.array(tile)
        holes = (a[:, :, 3] == 0).mean()
        # interior near-black that stayed opaque (should be >0 for rocky tiles)
        rgb = a[:, :, :3].astype(np.int16)
        dark_opaque = (
            (rgb[:, :, 0] < 18)
            & (rgb[:, :, 1] < 18)
            & (rgb[:, :, 2] < 18)
            & (a[:, :, 3] > 200)
        ).sum()
        print(
            f"  {name}.png  {tile.size[0]}x{tile.size[1]}  "
            f"trans%={holes:.3f} dark_opaque={dark_opaque}"
        )


def main() -> None:
    for biome, close_iter in SHEETS.items():
        process_biome(biome, close_iter)


if __name__ == "__main__":
    main()
