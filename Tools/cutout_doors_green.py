"""Chroma-green door gens -> rembg RGBA sprites."""
from __future__ import annotations

from pathlib import Path

import numpy as np
from PIL import Image
from rembg import new_session, remove

SRC_DIR = Path(r"C:\Users\21372\.cursor\projects\d-game-Metroidvania\assets")
OUT_DIR = Path(r"D:\game\Metroidvania\Assets\Art\Environment\Doors")
WORK = Path(r"D:\game\Metroidvania\Tools\_door_cutout_work")


def despill(im: Image.Image) -> Image.Image:
    arr = np.array(im.convert("RGBA"), dtype=np.float32)
    rgb = arr[:, :, :3]
    a = arr[:, :, 3]
    g = rgb[:, :, 1]
    r = rgb[:, :, 0]
    b = rgb[:, :, 2]
    spill = (g > r + 12) & (g > b + 12) & (a > 8)
    fix = (r + b) * 0.5
    rgb[spill, 1] = np.minimum(g[spill], fix[spill] + 8)
    pure = (g > 200) & (r < 80) & (b < 80)
    a[pure] = 0
    arr[:, :, :3] = rgb
    arr[:, :, 3] = a
    return Image.fromarray(arr.astype(np.uint8), "RGBA")


def crop_alpha(im: Image.Image, pad: int = 8) -> Image.Image:
    arr = np.array(im)
    a = arr[:, :, 3]
    ys, xs = np.where(a > 10)
    if len(xs) == 0:
        return im
    x0 = max(0, int(xs.min()) - pad)
    x1 = min(im.width, int(xs.max()) + 1 + pad)
    y0 = max(0, int(ys.min()) - pad)
    y1 = min(im.height, int(ys.max()) + 1 + pad)
    return im.crop((x0, y0, x1, y1))


def process(src: Path, dest: Path, session) -> None:
    im = Image.open(src).convert("RGB")
    pad = 40
    canvas = Image.new("RGB", (im.width + pad * 2, im.height + pad * 2), (0, 255, 0))
    canvas.paste(im, (pad, pad))
    cut = remove(canvas, session=session)
    cut = despill(cut)
    cut = crop_alpha(cut)
    dest.parent.mkdir(parents=True, exist_ok=True)
    cut.save(dest)
    WORK.mkdir(parents=True, exist_ok=True)
    cut.save(WORK / dest.name)
    print(f"{dest.name}: {cut.size} {cut.mode}")


def main() -> None:
    session = new_session("birefnet-general-lite")
    mapping = {
        "door_closed_green.png": "map_door_common_closed.png",
        "door_open_green.png": "map_door_common_open.png",
        "door_locked_green.png": "map_door_common_locked.png",
        "door_grand_green.png": "map_door_common_grand.png",
    }
    for src_name, out_name in mapping.items():
        src = SRC_DIR / src_name
        if not src.exists():
            print(f"SKIP missing {src}")
            continue
        process(src, OUT_DIR / out_name, session)


if __name__ == "__main__":
    main()
