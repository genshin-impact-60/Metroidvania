from pathlib import Path

import numpy as np
from PIL import Image

from fix_idle_head_bob import apply_head_bob

IMG = Path(r"C:\Users\21372\.grok\sessions\D%3A%5Cgame%5CMetroidvania\01a07c34-acd8-72a0-a88c-6970db459d77\images")
IDLE_SRC = Path(r"D:\game\Metroidvania\Assets\Art\Characters\protagonist_cursed_pilgrim_chibi.png")
OUT_DIR = Path(r"D:\game\Metroidvania\Assets\Art\Characters\Animations")
OUT_DIR.mkdir(parents=True, exist_ok=True)

RUN_ORDER = ["3.jpg", "7.jpg", "8.jpg", "1.jpg", "6.jpg", "5.jpg", "9.jpg", "2.jpg"]
IDLE_ORDER = [IDLE_SRC, IMG / "11.jpg", IDLE_SRC, IMG / "10.jpg"]
IDLE_SHEET = OUT_DIR / "protagonist_cursed_pilgrim_chibi_idle_sheet.png"


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


def pack(paths, out_name, prefix):
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

    sheet = Image.new("RGBA", (cell_w * len(keyed), cell_h), (0, 0, 0, 0))
    for i, (im, box) in enumerate(zip(keyed, boxes)):
        crop = im.crop(box)
        x = i * cell_w + (cell_w - crop.width) // 2
        y = cell_h - 8 - crop.height
        sheet.paste(crop, (x, y), crop)

    out = OUT_DIR / out_name
    sheet.save(out, "PNG")
    print(f"{out.name}: {sheet.size[0]}x{sheet.size[1]} cells={len(keyed)} {cell_w}x{cell_h}")
    return out, cell_w, cell_h, len(keyed)


def main():
    run_paths = [IMG / n for n in RUN_ORDER]
    if all(p.exists() for p in run_paths):
        pack(run_paths, "protagonist_cursed_pilgrim_chibi_run_sheet.png", "run")
    else:
        print("skip run pack: source jpgs missing")

    if all(Path(p).exists() for p in IDLE_ORDER):
        pack(IDLE_ORDER, IDLE_SHEET.name, "idle")
    else:
        print("skip idle repack: source frames missing; bobbing existing sheet")

    if IDLE_SHEET.exists():
        apply_head_bob(IDLE_SHEET, IDLE_SHEET)
    else:
        raise SystemExit(f"missing idle sheet: {IDLE_SHEET}")


if __name__ == "__main__":
    main()
