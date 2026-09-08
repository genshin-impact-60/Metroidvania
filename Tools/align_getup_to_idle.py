"""
Align getup sheet to idle_0 (protagonist_sprite_spec).

Root cause: getup_6 matches idle content height but has a smaller face/halo
(~18%), so the silhouette reads skinny/stretched; switching to idle pops size.
getup_5 has idle-like face/height ratio but is overall smaller.

Fix:
- Scale getup_5 (and nearby rise frames) to idle content height.
- Replace getup_6 with idle_0 (spec: 末帧尽量贴近 idle_0).
- Repack all cells with idle feet_pad=8; widen cells if needed.
"""
from __future__ import annotations

import re
import shutil
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(r"D:\game\Metroidvania")
ANIM = ROOT / "Assets" / "Art" / "Characters" / "Animations"
GETUP = ANIM / "protagonist_cursed_pilgrim_chibi_getup_sheet.png"
IDLE = ANIM / "protagonist_cursed_pilgrim_chibi_idle_sheet.png"
BACKUP = ANIM / "protagonist_cursed_pilgrim_chibi_getup_sheet_pre_align.png"
COMPARE = ROOT / "Tools" / "_sprite_compare"

FEET_PAD = 8
HEAD_MARGIN = 10
ALPHA_THRESH = 8
# After LANCZOS, fringe alphas drop; use a low thresh so feet don't "float".
PLACE_THRESH = 1


def parse_rects(meta_path: Path):
    text = meta_path.read_text(encoding="utf-8")
    sprites = []
    for m in re.finditer(
        r"name:\s*(\S+)\s*\n\s*rect:\s*\n\s*serializedVersion:\s*\d+\s*\n"
        r"\s*x:\s*(\d+)\s*\n\s*y:\s*(\d+)\s*\n\s*width:\s*(\d+)\s*\n\s*height:\s*(\d+)",
        text,
    ):
        sprites.append(
            {
                "name": m.group(1).strip(),
                "x": int(m.group(2)),
                "y": int(m.group(3)),
                "w": int(m.group(4)),
                "h": int(m.group(5)),
            }
        )
    return sprites


def load_cells(sheet_path: Path, meta_path: Path):
    im = Image.open(sheet_path).convert("RGBA")
    W, H = im.size
    cells = []
    for s in parse_rects(meta_path):
        x, y, w, h = s["x"], s["y"], s["w"], s["h"]
        top = H - (y + h)
        cells.append((s["name"], im.crop((x, top, x + w, top + h))))
    return cells


def content_bbox(im: Image.Image, thresh: int = ALPHA_THRESH):
    a = np.array(im.split()[-1])
    ys, xs = np.where(a >= thresh)
    if len(xs) == 0:
        return None
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def face_width(im: Image.Image) -> int:
    bbox = content_bbox(im)
    if not bbox:
        return 0
    x0, y0, x1, y1 = bbox
    ch = y1 - y0
    f0 = y0 + int(ch * 0.18)
    f1 = y0 + int(ch * 0.35)
    a = np.array(im.split()[-1])
    band = a[f0:f1, x0:x1]
    ys, xs = np.where(band >= ALPHA_THRESH)
    if len(xs) == 0:
        return 0
    return int(xs.max() - xs.min() + 1)


def extract_content(im: Image.Image, thresh: int = PLACE_THRESH) -> Image.Image | None:
    bbox = content_bbox(im, thresh=thresh)
    if not bbox:
        return None
    return im.crop(bbox)


def harden_alpha(im: Image.Image, thresh: int = 12) -> Image.Image:
    """Crush soft fringe so bbox/feet stay stable after resize."""
    arr = np.array(im)
    a = arr[:, :, 3]
    arr[:, :, 3] = np.where(a < thresh, 0, np.where(a < 250, np.maximum(a, 220), a)).astype(np.uint8)
    return Image.fromarray(arr)


def scale_to_height(content: Image.Image, target_h: int) -> Image.Image:
    if content.height == target_h:
        return content
    scale = target_h / content.height
    new_w = max(1, int(round(content.width * scale)))
    scaled = content.resize((new_w, target_h), Image.Resampling.LANCZOS)
    return harden_alpha(scaled)


def place_in_cell(content: Image.Image, cell_w: int, cell_h: int, feet_pad: int = FEET_PAD) -> Image.Image:
    cell = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    max_h = cell_h - feet_pad - HEAD_MARGIN
    if content.height > max_h:
        content = scale_to_height(content, max_h)
    # Snap true opaque bottom to feet_pad (ignore sub-threshold fringe).
    bbox = content_bbox(content, thresh=PLACE_THRESH)
    if bbox:
        content = content.crop(bbox)
    x = (cell_w - content.width) // 2
    y = cell_h - feet_pad - content.height
    if y < 0:
        y = 0
    cell.paste(content, (x, y), content)
    return cell


def update_meta(meta_path: Path, cell_w: int, cell_h: int, n: int):
    text = meta_path.read_text(encoding="utf-8")

    def repl_rect(match: re.Match) -> str:
        name = match.group(1)
        # getup_i index from name
        try:
            idx = int(name.split("_")[-1])
        except ValueError:
            idx = 0
        x = idx * cell_w
        return (
            f"name: {name}\n"
            f"      rect:\n"
            f"        serializedVersion: 2\n"
            f"        x: {x}\n"
            f"        y: 0\n"
            f"        width: {cell_w}\n"
            f"        height: {cell_h}"
        )

    text2, nsub = re.subn(
        r"name:\s*(getup_\d+)\s*\n\s*rect:\s*\n\s*serializedVersion:\s*\d+\s*\n"
        r"\s*x:\s*\d+\s*\n\s*y:\s*\d+\s*\n\s*width:\s*\d+\s*\n\s*height:\s*\d+",
        repl_rect,
        text,
    )
    if nsub != n:
        raise SystemExit(f"meta rect update expected {n} sprites, got {nsub}")
    meta_path.write_text(text2, encoding="utf-8")


def load_cells_from_sheet(sheet_path: Path, meta_path: Path | None, equal_n: int | None = None):
    im = Image.open(sheet_path).convert("RGBA")
    W, H = im.size
    if equal_n:
        cell_w = W // equal_n
        return [(f"getup_{i}", im.crop((i * cell_w, 0, (i + 1) * cell_w, H))) for i in range(equal_n)]
    cells = []
    for s in parse_rects(meta_path):
        x, y, w, h = s["x"], s["y"], s["w"], s["h"]
        top = H - (y + h)
        cells.append((s["name"], im.crop((x, top, x + w, top + h))))
    return cells


def scale_uniform(content: Image.Image, scale: float) -> Image.Image:
    if abs(scale - 1.0) < 0.001:
        return content
    new_w = max(1, int(round(content.width * scale)))
    new_h = max(1, int(round(content.height * scale)))
    return harden_alpha(content.resize((new_w, new_h), Image.Resampling.LANCZOS))


def main():
    COMPARE.mkdir(parents=True, exist_ok=True)

    idle_cells = load_cells(IDLE, IDLE.with_suffix(".png.meta"))
    idle_0 = next(im for name, im in idle_cells if name == "idle_0")
    idle_content = extract_content(idle_0)
    if idle_content is None:
        raise SystemExit("idle_0 has no opaque content")
    idle_h = idle_content.height
    idle_face = face_width(idle_0)
    print(f"idle_0 content={idle_content.size} face_w={idle_face}")

    # Always rebuild from pre-align backup so reruns don't compound scale.
    src = BACKUP if BACKUP.exists() else GETUP
    if src == BACKUP:
        getup_cells = load_cells_from_sheet(src, None, equal_n=7)
        print(f"source: {src.name} (equal 7-cell)")
    else:
        getup_cells = load_cells(GETUP, GETUP.with_suffix(".png.meta"))
        print(f"source: {src.name} (meta rects)")

    if len(getup_cells) < 7:
        raise SystemExit(f"expected 7 getup frames, got {len(getup_cells)}")

    # Face target for lie→kneel: match scaled kneel (~idle face * 0.85),
    # so frames 0-3 read as one size before rising to full idle.
    # Match kneel (getup_3) face so lie→kneel doesn't pop; rise still grows to idle.
    early_face_target = int(round(idle_face * 0.925))  # ~375, matches scaled kneel
    # Height blends for rise frames (toward idle content height).
    height_blend = {
        3: 0.35,  # softer kneel size step after early frames are enlarged
        4: 0.70,
        5: 1.0,
    }

    prepared: list[Image.Image] = []
    for i, (name, cell) in enumerate(getup_cells):
        if i == 6:
            content = idle_content.copy()
            print(f"{name}: REPLACE with idle_0 content {content.size}")
            prepared.append(content)
            continue

        content = extract_content(cell)
        if content is None:
            prepared.append(cell)
            continue

        if i <= 2:
            fw = face_width(content)
            scale = (early_face_target / fw) if fw > 0 else 1.0
            scale = max(1.0, min(scale, 1.30))
            before = content.size
            content = scale_uniform(content, scale)
            print(
                f"{name}: early face {fw}->{face_width(content)} "
                f"scale={scale:.3f} {before}->{content.size}"
            )
        elif i in height_blend:
            blend = height_blend[i]
            # First bump face toward early_face_target if still small, then height blend.
            fw = face_width(content)
            if fw > 0 and fw < early_face_target:
                content = scale_uniform(content, early_face_target / fw)
            target_h = int(round(content.height + (idle_h - content.height) * blend))
            if blend >= 1.0:
                target_h = idle_h
            before = content.size
            if target_h != content.height:
                content = scale_to_height(content, target_h)
            print(
                f"{name}: rise blend={blend} {before}->{content.size} "
                f"face->{face_width(content)}"
            )
        else:
            print(f"{name}: keep {content.size}")

        prepared.append(content)

    max_w = max(c.width for c in prepared)
    max_h = max(c.height for c in prepared)
    cell_w = max(650, max_w + 24)
    cell_h = max(900, max_h + FEET_PAD + HEAD_MARGIN)
    cell_w += cell_w % 2
    cell_h += cell_h % 2
    print(f"cell size -> {cell_w}x{cell_h}")

    sheet = Image.new("RGBA", (cell_w * len(prepared), cell_h), (0, 0, 0, 0))
    final_cells = []
    for i, content in enumerate(prepared):
        cell = place_in_cell(content, cell_w, cell_h, FEET_PAD)
        sheet.paste(cell, (i * cell_w, 0), cell)
        final_cells.append(cell)

    if not BACKUP.exists():
        shutil.copy2(GETUP, BACKUP)
        print(f"backup -> {BACKUP.name}")
    else:
        print(f"backup kept: {BACKUP.name}")

    sheet.save(GETUP)
    update_meta(GETUP.with_suffix(".png.meta"), cell_w, cell_h, len(prepared))
    print(f"wrote {GETUP} ({sheet.size[0]}x{sheet.size[1]})")

    for i, cell in enumerate(final_cells):
        c = extract_content(cell)
        bb = content_bbox(cell, thresh=ALPHA_THRESH)
        feet = cell.height - bb[3] if bb else -1
        print(
            f"verify getup_{i}: content_h={c.height if c else 0} "
            f"face_w={face_width(cell)} feet_pad={feet}"
        )
    print(f"verify idle_0: content_h={idle_h} face_w={idle_face}")

    idle_cell = place_in_cell(idle_content, cell_w, cell_h)
    stand = final_cells[6]

    def tint(im, rgb):
        arr = np.array(im)
        out = arr.copy()
        mask = out[:, :, 3] > 8
        out[mask, 0] = (out[mask, 0].astype(np.int16) // 2 + rgb[0] // 2).clip(0, 255).astype(np.uint8)
        out[mask, 1] = (out[mask, 1].astype(np.int16) // 2 + rgb[1] // 2).clip(0, 255).astype(np.uint8)
        out[mask, 2] = (out[mask, 2].astype(np.int16) // 2 + rgb[2] // 2).clip(0, 255).astype(np.uint8)
        return Image.fromarray(out)

    idle_t = tint(idle_cell, (0, 220, 255))
    stand_t = tint(stand, (255, 60, 60))
    ow, oh = cell_w + 40, cell_h + 40
    overlay = Image.new("RGBA", (ow, oh), (30, 30, 36, 255))
    overlay.paste(idle_t, (20, 20), idle_t)
    tmp = Image.new("RGBA", overlay.size, (0, 0, 0, 0))
    tmp.paste(stand_t, (20, 20), stand_t)
    overlay = Image.alpha_composite(overlay, tmp)
    overlay.save(COMPARE / "overlay_after.png")
    sheet.save(COMPARE / "getup_sheet_after.png")
    print(f"preview -> {COMPARE / 'getup_sheet_after.png'}")


if __name__ == "__main__":
    main()
