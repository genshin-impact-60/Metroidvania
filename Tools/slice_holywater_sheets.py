"""Convert holy-water AI sheets: black→alpha, auto-slice, write Unity .meta."""
from __future__ import annotations

import hashlib
import uuid
from pathlib import Path

import numpy as np
from PIL import Image


def black_to_alpha(im: Image.Image, thresh: int = 18) -> Image.Image:
    im = im.convert("RGBA")
    arr = np.array(im)
    rgb = arr[:, :, :3].astype(np.int16)
    mask = (rgb[:, :, 0] < thresh) & (rgb[:, :, 1] < thresh) & (rgb[:, :, 2] < thresh)
    dark = rgb.sum(axis=2) < thresh * 3
    mask = mask | dark
    arr[mask, 3] = 0
    rem = ~mask
    lum = rgb.sum(axis=2)
    soft = rem & (lum < 55)
    arr[soft, 3] = np.clip((lum[soft] - 20) * 6, 0, 255).astype(np.uint8)
    return Image.fromarray(arr)


def find_boxes(arr: np.ndarray, min_area: int = 400, gap: int = 12):
    a = arr[:, :, 3] > 12
    h, w = a.shape
    visited = np.zeros_like(a, dtype=bool)
    boxes = []
    for y in range(h):
        for x in range(w):
            if not a[y, x] or visited[y, x]:
                continue
            stack = [(x, y)]
            visited[y, x] = True
            minx = maxx = x
            miny = maxy = y
            count = 0
            while stack:
                cx, cy = stack.pop()
                count += 1
                minx = min(minx, cx)
                maxx = max(maxx, cx)
                miny = min(miny, cy)
                maxy = max(maxy, cy)
                for nx, ny in ((cx - 1, cy), (cx + 1, cy), (cx, cy - 1), (cx, cy + 1)):
                    if 0 <= nx < w and 0 <= ny < h and a[ny, nx] and not visited[ny, nx]:
                        visited[ny, nx] = True
                        stack.append((nx, ny))
            if count >= min_area:
                minx = max(0, minx - 2)
                miny = max(0, miny - 2)
                maxx = min(w - 1, maxx + 2)
                maxy = min(h - 1, maxy + 2)
                boxes.append([minx, miny, maxx, maxy, count])
    boxes.sort(key=lambda b: b[0])
    merged = []
    for b in boxes:
        if not merged:
            merged.append(b)
            continue
        prev = merged[-1]
        if b[0] <= prev[2] + gap and not (b[3] < prev[1] or b[1] > prev[3]):
            prev[0] = min(prev[0], b[0])
            prev[1] = min(prev[1], b[1])
            prev[2] = max(prev[2], b[2])
            prev[3] = max(prev[3], b[3])
            prev[4] += b[4]
        else:
            merged.append(b)
    return [(x0, y0, x1 - x0 + 1, y1 - y0 + 1) for x0, y0, x1, y1, _ in merged]


def make_guid(s: str) -> str:
    return hashlib.md5(s.encode()).hexdigest()


def write_meta(path: Path, sprites, ppu: int = 100) -> None:
    im = Image.open(path)
    _, h = im.size
    internal_table = []
    sprite_blocks = []
    name_table = []
    for name, x, y, w, box_h, pivot in sprites:
        uy = h - (y + box_h)
        iid = -abs(int(hashlib.md5(name.encode()).hexdigest()[:15], 16) % (10**18))
        sid = uuid.uuid4().hex
        internal_table.append(f"  - first:\n      213: {iid}\n    second: {name}")
        name_table.append(f"      {name}: {iid}")
        px, py = pivot
        sprite_blocks.append(
            f"""    - serializedVersion: 2
      name: {name}
      rect:
        serializedVersion: 2
        x: {x}
        y: {uy}
        width: {w}
        height: {box_h}
      alignment: 9
      pivot: {{x: {px}, y: {py}}}
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
      weights: []"""
        )

    guid = make_guid(str(path.resolve()))
    meta = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable:
{chr(10).join(internal_table)}
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
  spriteMode: 2
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
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
{chr(10).join(sprite_blocks)}
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
{chr(10).join(name_table)}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    path.with_suffix(".png.meta").write_text(meta, encoding="utf-8")
    print("wrote meta", path.name, "sprites", len(sprites))


def process(path: Path, names, pivots, min_area: int = 500) -> None:
    # Prefer original RGB from Cursor assets if Art copy already alpha'd oddly
    im = black_to_alpha(Image.open(path))
    arr = np.array(im)
    boxes = find_boxes(arr, min_area=min_area, gap=12)
    print(path.name, "boxes", len(boxes), boxes)
    im.save(path)
    sprites = []
    for i, box in enumerate(boxes):
        name = names[i] if i < len(names) else f"{path.stem}_{i}"
        pivot = pivots[i] if i < len(pivots) else (0.5, 0.0)
        sprites.append((name, box[0], box[1], box[2], box[3], pivot))
    write_meta(path, sprites, ppu=100)


def main() -> None:
    base = Path(r"D:/game/Metroidvania/Assets/Art/Environment/Hazards")
    cursor = Path(r"C:/Users/21372/.cursor/projects/d-game-Metroidvania/assets")

    # Re-copy fresh RGB sources before alpha keying
    for name in (
        "map_hazard_holywater_floor_decal.png",
        "map_hazard_holywater_cathedral_flat.png",
        "map_hazard_holywater_cathedral.png",
    ):
        src = cursor / name
        if src.exists():
            Image.open(src).save(base / name)

    process(
        base / "map_hazard_holywater_floor_decal.png",
        names=[
            "holywater_burn",
            "holywater_burn_small",
            "holywater_drop_0",
            "holywater_drop_1",
            "holywater_drop_2",
            "holywater_impact",
        ],
        pivots=[
            (0.5, 0.05),
            (0.5, 0.05),
            (0.5, 1.0),
            (0.5, 1.0),
            (0.5, 1.0),
            (0.5, 0.1),
        ],
        min_area=350,
    )

    process(
        base / "map_hazard_holywater_cathedral_flat.png",
        names=[
            "holywater_flat_pool",
            "holywater_flat_basin",
            "holywater_flat_drop_pedestal",
            "holywater_ceiling_drip_a",
            "holywater_ceiling_drip_b",
            "holywater_flat_ripple",
        ],
        pivots=[
            (0.5, 0.05),
            (0.5, 0.05),
            (0.5, 0.0),
            (0.5, 1.0),
            (0.5, 1.0),
            (0.5, 0.1),
        ],
        min_area=350,
    )

    process(
        base / "map_hazard_holywater_cathedral.png",
        names=[
            "holywater_basin_pool",
            "holywater_ceiling_block",
            "holywater_frag_a",
            "holywater_frag_b",
            "holywater_splash",
        ],
        pivots=[
            (0.5, 0.05),
            (0.5, 1.0),
            (0.5, 1.0),
            (0.5, 1.0),
            (0.5, 0.1),
        ],
        min_area=400,
    )


if __name__ == "__main__":
    main()
