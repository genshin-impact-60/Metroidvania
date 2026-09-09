"""Build open door using closed frame (complete left pillar) + open leaf/interior."""
from __future__ import annotations

from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage
from skimage.morphology import convex_hull_image

DOORS = Path(r"D:\game\Metroidvania\Assets\Art\Environment\Doors")
WORK = Path(r"D:\game\Metroidvania\Tools\_door_cutout_work")
SHEET = DOORS / "map_doors_common.png"


def hull_rgba_from_rgb(im: Image.Image) -> Image.Image:
    arr = np.array(im.convert("RGBA"))
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
    pad = 8
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


def inner_door_mask_closed(a: np.ndarray) -> np.ndarray:
    """Mask of wooden door panel inside closed arch (not stone frame)."""
    opaque = a[:, :, 3] > 128
    rgb = a[:, :, :3].astype(np.float32)
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    lum = r + g + b
    # wood: brownish, darker than stone, not gold
    wood = opaque & (lum < 420) & (lum > 60) & (r > g - 5) & (r > b + 10) & (g > b)
    # also very dark iron on door
    iron = opaque & (lum < 90) & (np.abs(r - g) < 25) & (np.abs(g - b) < 25)
    door = wood | iron
    # keep only largest component roughly in center
    door = ndimage.binary_opening(door, structure=np.ones((3, 3)))
    door = ndimage.binary_closing(door, structure=np.ones((7, 7)), iterations=2)
    labeled, n = ndimage.label(door)
    if n == 0:
        return door
    # pick component closest to image center
    cy, cx = a.shape[0] // 2, a.shape[1] // 2
    best, bestd = 0, 1e18
    for i in range(1, n + 1):
        ys, xs = np.where(labeled == i)
        if len(xs) < 500:
            continue
        d = (ys.mean() - cy) ** 2 + (xs.mean() - cx) ** 2
        if d < bestd:
            bestd = d
            best = i
    mask = labeled == best
    # fill holes in door panel
    mask = ndimage.binary_fill_holes(mask)
    # slightly erode so stone lip remains from closed frame
    mask = ndimage.binary_erosion(mask, structure=np.ones((3, 3)), iterations=2)
    return mask


def main() -> None:
    # Full open crop from sheet (correct column bounds)
    sheet = Image.open(SHEET).convert("RGB")
    # content run for door 2: (346, 673), y from earlier ~222-809
    open_crop = sheet.crop((338, 222, 681, 809))
    open_src = hull_rgba_from_rgb(open_crop)
    open_src.save(WORK / "open_src_hull.png")

    closed = Image.open(DOORS / "map_door_common_closed.png").convert("RGBA")
    ca = np.array(closed)
    cx0, cy0, cx1, cy1 = content_bbox(ca)
    target_h = cy1 - cy0 + 1
    target_w = cx1 - cx0 + 1

    # Scale open source content to match closed content box
    oa = np.array(open_src)
    ox0, oy0, ox1, oy1 = content_bbox(oa)
    open_content = open_src.crop((ox0, oy0, ox1 + 1, oy1 + 1))
    open_resized = open_content.resize((target_w, target_h), Image.Resampling.LANCZOS)
    ora = np.array(open_resized)

    # Door interior mask on closed
    door_mask = inner_door_mask_closed(ca)
    # Expand mask slightly upward into arch void area (pointed top)
    door_mask = ndimage.binary_dilation(door_mask, structure=np.ones((5, 5)), iterations=3)
    # Restrict to inside stone: don't eat outer frame — keep left/right margins of content
    margin = int(0.16 * target_w)
    restrict = np.zeros_like(door_mask)
    restrict[cy0:cy1 + 1, cx0 + margin : cx1 - margin + 1] = True
    # also allow a bit more on right where open door leaf swings out
    restrict[cy0:cy1 + 1, cx1 - margin : min(ca.shape[1], cx1 + 40)] = True
    door_mask &= restrict
    # Dilate more in center for open interior
    center = np.zeros_like(door_mask)
    center[cy0 + int(0.15 * target_h) : cy1 - int(0.05 * target_h), cx0 + margin : cx1 - int(0.12 * target_w)] = True
    door_mask |= ndimage.binary_dilation(door_mask & center, iterations=8) & restrict & (ca[:, :, 3] > 128)

    Image.fromarray((door_mask.astype(np.uint8) * 255)).save(WORK / "door_replace_mask.png")

    # Place open_resized aligned to closed content bbox
    placed = np.zeros_like(ca)
    placed[cy0 : cy0 + target_h, cx0 : cx0 + target_w] = ora

    # For replace region: use open pixels; where open is transparent/dark void keep dark
    result = ca.copy()
    # soft blend at mask edge
    dist_in = ndimage.distance_transform_edt(door_mask)
    dist_out = ndimage.distance_transform_edt(~door_mask)
    band = 6.0
    # weight 1 inside, 0 outside, ramp on edge
    weight = np.clip(dist_in / band, 0, 1)
    weight[~door_mask] = 0
    # near outer edge of mask ramp
    edge = door_mask & (dist_in <= band)
    weight[edge] = dist_in[edge] / band

    src_a = placed[:, :, 3].astype(np.float32) / 255.0
    # Prefer open RGB where mask; if open alpha low but mask (interior void), use black
    for c in range(3):
        open_c = placed[:, :, c].astype(np.float32)
        void = door_mask & (placed[:, :, 3] < 40)
        open_c[void] = 8  # near-black interior
        src_a_eff = src_a.copy()
        src_a_eff[void] = 1.0
        w = weight * np.clip(src_a_eff, 0, 1)
        # stronger replace in solid mask core
        w = np.maximum(w, (dist_in > band).astype(np.float32) * door_mask)
        result[:, :, c] = (
            w * open_c + (1 - w) * ca[:, :, c].astype(np.float32)
        ).astype(np.uint8)
    # alpha stays from closed (complete silhouette including left pillar)
    result[:, :, 3] = ca[:, :, 3]

    # Right side: open door leaf may extend past closed door wood — allow paste where open has opaque
    # beyond closed wood but still near frame
    swing = (placed[:, :, 3] > 128) & (ca[:, :, 3] > 128)
    # only right half additions already in blend; also extend canvas if open leaf sticks out
    # Check if open content is wider on right relative to door
    # Expand result canvas if needed for swinging door
    # Find open opaque relative to placed that falls near right of closed content
    extra_right = 0
    right_cols = np.where((placed[:, :, 3] > 128).any(axis=0))[0]
    if len(right_cols) and right_cols[-1] > cx1:
        extra_right = int(right_cols[-1] - cx1 + 12)

    if extra_right > 0:
        wide = np.zeros((result.shape[0], result.shape[1] + extra_right, 4), dtype=np.uint8)
        wide[:, : result.shape[1]] = result
        # paste protruding open pixels
        for x in range(cx1, min(placed.shape[1], result.shape[1] + extra_right)):
            col = placed[:, x]
            m = col[:, 3] > 128
            if not m.any():
                continue
            dx = x
            if dx < wide.shape[1]:
                wide[m, dx] = col[m]
        result = wide

    out = Image.fromarray(result)
    # crop tight
    a = result[:, :, 3]
    ys, xs = np.where(a > 10)
    pad = 8
    out = out.crop(
        (
            max(0, int(xs.min()) - pad),
            max(0, int(ys.min()) - pad),
            min(out.size[0], int(xs.max()) + 1 + pad),
            min(out.size[1], int(ys.max()) + 1 + pad),
        )
    )
    dest = DOORS / "map_door_common_open.png"
    out.save(dest)
    out.save(WORK / "map_door_common_open.png")
    print("saved", dest, out.size)

    # QA left vs closed left
    for label, path in [("open", dest), ("closed", DOORS / "map_door_common_closed.png")]:
        arr = np.array(Image.open(path).convert("RGBA"))
        m = arr[:, :, 3] > 128
        ys, xs = np.where(m)
        x0 = int(xs.min())
        firsts = [
            int(np.where(m[y])[0][0])
            for y in range(int(ys.min() + 0.4 * (ys.max() - ys.min())), int(ys.min() + 0.85 * (ys.max() - ys.min())))
            if m[y].any()
        ]
        print(label, "size", arr.shape[1], arr.shape[0], "left_margin", x0, "first-x range", max(firsts) - min(firsts))

    for bg, name in [((255, 0, 255, 255), "magenta"), ((245, 240, 230, 255), "cream")]:
        c = Image.new("RGBA", out.size, bg)
        c.alpha_composite(out)
        c.convert("RGB").save(WORK / f"preview_open_composite_{name}.png")


if __name__ == "__main__":
    main()
