"""Pack source stills into per-frame action folders under Animations/."""
from pathlib import Path

import numpy as np
from PIL import Image

from fix_idle_head_bob import IDLE_DIR, apply_head_bob
from install_anim_frames import write_single_sprite_meta

IMG = Path(r"C:\Users\21372\.grok\sessions\D%3A%5Cgame%5CMetroidvania\01a07c34-acd8-72a0-a88c-6970db459d77\images")
IDLE_SRC = Path(r"D:\game\Metroidvania\Assets\Art\Characters\protagonist_cursed_pilgrim_chibi.png")
OUT_DIR = Path(r"D:\game\Metroidvania\Assets\Art\Characters\Animations")
OUT_DIR.mkdir(parents=True, exist_ok=True)

RUN_ORDER = ["3.jpg", "7.jpg", "8.jpg", "1.jpg", "6.jpg", "5.jpg", "9.jpg", "2.jpg"]
IDLE_ORDER = [IDLE_SRC, IMG / "11.jpg", IDLE_SRC, IMG / "10.jpg"]


def key_black(im: Image.Image, thresh: int = 16) -> Image.Image:
    im = im.convert("RGBA")
    arr = np.array(im)
    mask = (arr[:, :, 0] < thresh) & (arr[:, :, 1] < thresh) & (arr[:, :, 2] < thresh)
    arr[:, :, 3] = np.where(mask, 0, arr[:, :, 3])
    arr[mask, 0:3] = 0
    return Image.fromarray(arr)


def opaque_bbox(im: Image.Image):
    a = np.array(im.split()[-1])
    ys, xs = np.where(a > 12)
    if len(xs) == 0:
        return 0, 0, im.width, im.height
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def pack_frames(paths, action: str, prefix: str):
    keyed = []
    boxes = []
    for p in paths:
        im = key_black(Image.open(p))
        box = opaque_bbox(im)
        keyed.append(im)
        boxes.append(box)

    widths = [b[2] - b[0] for b in boxes]
    heights = [b[3] - b[1] for b in boxes]
    cell_w = max(widths) + 24
    cell_h = max(heights) + 24
    cell_w += cell_w % 2
    cell_h += cell_h % 2

    dest = OUT_DIR / action
    dest.mkdir(parents=True, exist_ok=True)
    for i, (im, box) in enumerate(zip(keyed, boxes)):
        crop = im.crop(box)
        cell = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
        x = (cell_w - crop.width) // 2
        y = cell_h - 8 - crop.height
        cell.paste(crop, (x, y), crop)
        out = dest / f"{prefix}_{i}.png"
        cell.save(out, "PNG")
        write_single_sprite_meta(out)

    print(f"{action}/: {len(keyed)} frames {cell_w}x{cell_h}")
    return dest, cell_w, cell_h, len(keyed)


def main():
    run_paths = [IMG / n for n in RUN_ORDER]
    if all(p.exists() for p in run_paths):
        pack_frames(run_paths, "run", "run")
    else:
        print("skip run pack: source jpgs missing")

    if all(Path(p).exists() for p in IDLE_ORDER):
        pack_frames(IDLE_ORDER, "idle", "idle")
    else:
        print("skip idle repack: source frames missing; bobbing existing idle frames")

    if IDLE_DIR.exists() and (IDLE_DIR / "idle_0.png").exists():
        apply_head_bob(IDLE_DIR)
    else:
        raise SystemExit(f"missing idle frames: {IDLE_DIR}")


if __name__ == "__main__":
    main()
