"""Copy unpacked sheet frames into Assets and write Single-sprite Unity .meta files."""
from __future__ import annotations

import hashlib
import shutil
import uuid
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Tools" / "_unpacked_frames"
ANIM = ROOT / "Assets" / "Art" / "Characters" / "Animations"

# unpacked folder stem → action folder name under Animations/
SHEET_TO_ACTION = {
    "protagonist_cursed_pilgrim_chibi_idle_sheet": "idle",
    "protagonist_cursed_pilgrim_chibi_idle_oathblade_sheet": "idle_oathblade",
    "protagonist_cursed_pilgrim_chibi_run_sheet": "run",
    "protagonist_cursed_pilgrim_chibi_jump_sheet": "jump",
    "protagonist_cursed_pilgrim_chibi_getup_sheet": "getup",
}

PPU = 256


def make_guid(s: str) -> str:
    return hashlib.md5(s.encode()).hexdigest()


def write_single_sprite_meta(png_path: Path) -> None:
    im = Image.open(png_path)
    w, h = im.size
    name = png_path.stem
    iid = -abs(int(hashlib.md5(name.encode()).hexdigest()[:15], 16) % (10**18))
    sid = uuid.uuid4().hex
    guid = make_guid(str(png_path.resolve()))
    meta = f"""fileFormatVersion: 2
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
  spritePixelsToUnits: {PPU}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
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
      alignment: 7
      pivot: {{x: 0.5, y: 0}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      customData: 
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: {sid}
      internalID: {iid}
      vertices: []
      indices: 
      edges: []
      weights: []
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
    nameFileIdTable:
      {name}: {iid}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    meta_path = Path(str(png_path) + ".meta")
    meta_path.write_text(meta, encoding="utf-8")


def main() -> None:
    if not SRC.exists():
        raise SystemExit(f"missing unpacked frames: {SRC} (run unpack_anim_sheets.py first)")

    total = 0
    for sheet_stem, action in SHEET_TO_ACTION.items():
        src_dir = SRC / sheet_stem
        if not src_dir.is_dir():
            raise SystemExit(f"missing: {src_dir}")
        dest = ANIM / action
        if dest.exists():
            shutil.rmtree(dest)
        dest.mkdir(parents=True, exist_ok=True)
        for png in sorted(src_dir.glob("*.png")):
            out = dest / png.name
            shutil.copy2(png, out)
            write_single_sprite_meta(out)
            total += 1
            print(f"  {action}/{out.name}")
        print(f"{action}: {total} frames so far → {dest.relative_to(ROOT)}")

    # Remove sprite sheets (and optional backups) from Animations.
    removed = 0
    for pattern in ("*_sheet.png", "*_sheet.png.meta", "*_sheet_pre_align.png", "*_sheet_pre_align.png.meta",
                    "*_sheet_green.png", "*_sheet_green.png.meta"):
        for path in ANIM.glob(pattern):
            path.unlink()
            removed += 1
            print(f"deleted {path.name}")

    print(f"done: installed {total} frames, removed {removed} sheet files")


if __name__ == "__main__":
    main()
