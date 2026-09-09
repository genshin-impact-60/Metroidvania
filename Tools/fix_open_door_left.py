"""Fix map_door_common_open: restore cropped left pillar using closed door."""
from __future__ import annotations

from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage
from skimage.morphology import convex_hull_image

DOORS = Path(r"D:\game\Metroidvania\Assets\Art\Environment\Doors")
WORK = Path(r"D:\game\Metroidvania\Tools\_door_cutout_work")


def hull_cutout(rgb_path: Path) -> Image.Image:
    arr = np.array(Image.open(rgb_path).convert("RGBA"))
    lum = arr[:, :, :3].astype(np.int16).sum(2)
    content = lum > 35
    hull = convex_hull_image(
        ndimage.binary_closing(content, structure=np.ones((5, 5)), iterations=3)
    )
    alpha = np.zeros(lum.shape, dtype=np.uint8)
    alpha[hull] = 255
    alpha[content] = 255
    out = arr.copy()
    out[:, :, 3] = alpha
    ys, xs = np.where(alpha > 10)
    pad = 10
    return Image.fromarray(out).crop(
        (
            max(0, int(xs.min()) - pad),
            max(0, int(ys.min()) - pad),
            min(out.shape[1], int(xs.max()) + 1 + pad),
            min(out.shape[0], int(ys.max()) + 1 + pad),
        )
    )


def content_bbox(a: np.ndarray) -> tuple[int, int, int, int]:
    m = a[:, :, 3] > 128
    ys, xs = np.where(m)
    return int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())


def left_outer_to_jamb(a: np.ndarray, frac: float = 0.72) -> tuple[int, int]:
    """Return (outer_x, jamb_x) for left pillar at given height fraction."""
    m = a[:, :, 3] > 128
    ys, xs = np.where(m)
    y0, y1 = int(ys.min()), int(ys.max())
    y = int(y0 + (y1 - y0) * frac)
    row_a = a[y]
    opaque = row_a[:, 3] > 128
    xs_row = np.where(opaque)[0]
    outer = int(xs_row[0])
    # interior: dark opaque OR transparent hole inside span
    lum = row_a[:, :3].astype(np.int16).sum(2)
    # walk inward from outer through stone until dark void / wood-ish transition
    # For closed: wood is dark brown; for open: void is near-black
    jamb = outer
    for x in range(outer, int(xs_row[-1])):
        if not opaque[x]:
            jamb = x
            break
        if lum[x] < 90 and x > outer + 40:
            # entered dark interior / door face from stone
            # confirm we're past stone: look for sustained dark
            if lum[x : x + 8].mean() < 100:
                jamb = x
                break
    else:
        jamb = outer + int((xs_row[-1] - outer) * 0.22)
    return outer, jamb


def main() -> None:
    closed = Image.open(DOORS / "map_door_common_closed.png").convert("RGBA")
    open_im = hull_cutout(WORK / "ref_open_full.png")
    th = closed.size[1]
    scale = th / open_im.size[1]
    open_im = open_im.resize(
        (int(round(open_im.size[0] * scale)), th), Image.Resampling.LANCZOS
    )

    ca = np.array(closed)
    oa = np.array(open_im)
    c_outer, c_jamb = left_outer_to_jamb(ca)
    o_outer, o_jamb = left_outer_to_jamb(oa)
    c_thick = c_jamb - c_outer
    o_thick = o_jamb - o_outer
    print(f"closed left pillar {c_outer}->{c_jamb} ({c_thick}px)")
    print(f"open   left pillar {o_outer}->{o_jamb} ({o_thick}px)")

    # Extract closed left pillar strip (outer through jamb + blend)
    blend = 18
    strip = closed.crop((max(0, c_outer - 4), 0, min(closed.size[0], c_jamb + blend), th))
    strip_a = np.array(strip)

    # Extra canvas width if open pillar thinner / clipped
    missing = max(0, c_thick - o_thick + 24)
    canvas_w = open_im.size[0] + missing
    base = Image.new("RGBA", (canvas_w, th), (0, 0, 0, 0))
    open_x = missing
    base.paste(open_im, (open_x, 0), open_im)
    ba = np.array(base)

    # Place strip so jambs align: strip jamb at open_x + o_jamb
    # strip local jamb = (c_jamb - (c_outer-4))
    strip_local_outer = 4 if c_outer >= 4 else c_outer
    strip_local_jamb = strip_local_outer + c_thick
    paste_x = (open_x + o_jamb) - strip_local_jamb
    print(f"missing={missing} paste_x={paste_x}")

    layer = Image.new("RGBA", (canvas_w, th), (0, 0, 0, 0))
    layer.paste(strip, (int(paste_x), 0), strip)
    la = np.array(layer)

    # Composite: left of open outer uses closed strip; overlap blends toward open
    result = ba.copy()
    open_abs_outer = open_x + o_outer
    for x in range(canvas_w):
        col_l = la[:, x]
        col_b = ba[:, x]
        ls = col_l[:, 3] > 0
        if not ls.any():
            continue
        if x < open_abs_outer + 2:
            # prefer strip (restores clipped edge)
            result[ls, x] = col_l[ls]
        elif x < open_abs_outer + blend:
            # alpha blend strip -> open
            t = (x - open_abs_outer) / blend
            both = ls & (col_b[:, 3] > 0)
            only_l = ls & (col_b[:, 3] == 0)
            result[only_l, x] = col_l[only_l]
            if both.any():
                for c in range(4):
                    result[both, x, c] = (
                        (1 - t) * col_l[both, c] + t * col_b[both, c]
                    ).astype(np.uint8)
        # else keep open

    im = Image.fromarray(result)
    a = result[:, :, 3]
    ys, xs = np.where(a > 10)
    pad = 8
    im = im.crop(
        (
            max(0, int(xs.min()) - pad),
            max(0, int(ys.min()) - pad),
            min(im.size[0], int(xs.max()) + 1 + pad),
            min(im.size[1], int(ys.max()) + 1 + pad),
        )
    )
    dest = DOORS / "map_door_common_open.png"
    im.save(dest)
    im.save(WORK / "map_door_common_open.png")
    print("saved", dest, im.size)

    ra = np.array(im)
    r_outer, r_jamb = left_outer_to_jamb(ra)
    print(f"result left pillar {r_outer}->{r_jamb} ({r_jamb - r_outer}px)")

    for bg, name in [((255, 0, 255, 255), "magenta"), ((245, 240, 230, 255), "cream")]:
        c = Image.new("RGBA", im.size, bg)
        c.alpha_composite(im)
        c.convert("RGB").save(WORK / f"preview_open_fixed2_{name}.png")


if __name__ == "__main__":
    main()
