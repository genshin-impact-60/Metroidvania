"""Rewrite Unity .meta for cathedral single-tile PNGs: full rect, bottom pivot, height-matched PPU."""
from __future__ import annotations

import re
from pathlib import Path

from PIL import Image

TILE_DIR = Path(r"D:\game\Metroidvania\Assets\Art\Environment\Tilesets\cathedral")

# Original sheet slice heights @ 256 PPU (world units before TileScale).
ORIG_H_WORLD = {
    0: 197 / 256,
    1: 162 / 256,
    2: 167 / 256,
    3: 265 / 256,
    4: 262 / 256,
    5: 262 / 256,
    6: 266 / 256,
    7: 163 / 256,
    8: 163 / 256,
    9: 162 / 256,
    10: 215 / 256,
    11: 146 / 256,
    12: 167 / 256,
    13: 142 / 256,
    14: 135 / 256,
}


def write_meta(png: Path, index: int) -> None:
    im = Image.open(png)
    w, h = im.size
    target_h = ORIG_H_WORLD.get(index, h / 256)
    ppu = max(1, int(round(h / target_h)))
    name = png.stem  # map_tileset_cathedral_N
    meta = png.with_suffix(".png.meta")
    guid = None
    if meta.exists():
        m = re.search(r"^guid: ([a-f0-9]+)", meta.read_text(encoding="utf-8"), re.M)
        if m:
            guid = m.group(1)
    if not guid:
        import hashlib

        guid = hashlib.md5(str(png).encode()).hexdigest()

    # Stable internalID from name hash (positive 64-bit-ish).
    internal = abs(hash(name)) % (10**16)
    if internal > 2**62:
        internal = internal % (2**62)

    text = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable:
  - first:
      213: {internal}
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
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    meta.write_text(text, encoding="utf-8")
    print(f"{png.name}: {w}x{h} ppu={ppu} pivot=bottom")


def main() -> None:
    for png in sorted(TILE_DIR.glob("map_tileset_cathedral_*.png")):
        idx = int(png.stem.rsplit("_", 1)[1])
        write_meta(png, idx)


if __name__ == "__main__":
    main()
