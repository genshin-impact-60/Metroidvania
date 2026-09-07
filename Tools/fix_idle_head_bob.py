"""Rebuild idle sheet so the head breathes with the body.

Source sheets often only animate cloak/torso. This composites a soft vertical
head bob onto rest + sway poses so the neck no longer looks welded in place.

Safe to re-run on an already-fixed sheet: rest is cell 0, sway body is recovered
from the peak cell (or the last cell on a raw sheet).
"""

from pathlib import Path

import numpy as np
from PIL import Image

SHEET = Path(r"D:\game\Metroidvania\Assets\Art\Characters\Animations\protagonist_cursed_pilgrim_chibi_idle_sheet.png")
OUT = SHEET

# Head / shoulder blend band (source sheet coordinates).
SPLIT_Y = 355
FEATHER = 30


def load_cells(path: Path, n: int = 4):
    sheet = Image.open(path).convert("RGBA")
    w, h = sheet.size
    cw = w // n
    cells = [np.array(sheet.crop((i * cw, 0, (i + 1) * cw, h))) for i in range(n)]
    return cells, cw, h


def head_weight(h: int, split_y: int = SPLIT_Y, feather: int = FEATHER) -> np.ndarray:
    ys = np.arange(h, dtype=np.float32)[:, None]
    return np.clip((split_y - ys) / float(feather), 0.0, 1.0)


def shift_rgba(src: np.ndarray, dy: int) -> np.ndarray:
    if dy == 0:
        return src.copy()
    out = np.zeros_like(src)
    if dy > 0:
        out[dy:] = src[:-dy]
    else:
        out[:dy] = src[-dy:]
    return out


def with_head_bob(body: np.ndarray, head_src: np.ndarray, dy: int) -> np.ndarray:
    """Keep body pixels, overlay head_src shifted down by dy with a soft neck blend."""
    h = body.shape[0]
    w_head = head_weight(h)
    shifted = shift_rgba(head_src, dy)

    body_f = body.astype(np.float32)
    head_f = shifted.astype(np.float32)
    wh = w_head[..., None]

    head_a = (head_f[:, :, 3:4] / 255.0) * wh
    out = body_f.copy()
    out[:, :, :3] = head_f[:, :, :3] * head_a + body_f[:, :, :3] * (1.0 - head_a)
    out[:, :, 3:4] = np.maximum(body_f[:, :, 3:4], head_f[:, :, 3:4] * wh)

    if dy > 0:
        top = head_weight(h, split_y=SPLIT_Y - dy - 4, feather=10)
        empty = head_f[:, :, 3:4] < 8
        clear = empty * (1.0 - top[..., None]) * wh
        out[:, :, 3:4] *= 1.0 - clear

    return np.clip(out, 0, 255).astype(np.uint8)


def body_diff_score(a: np.ndarray, b: np.ndarray) -> int:
    diff = np.abs(a.astype(np.int16) - b.astype(np.int16)).sum(axis=2)
    mask = (a[:, :, 3] > 12) | (b[:, :, 3] > 12)
    below = np.arange(a.shape[0])[:, None] > SPLIT_Y
    return int(((diff > 30) & mask & below).sum())


def resolve_sway(cells, rest: np.ndarray) -> np.ndarray:
    """Pick the best body-sway donor, then restore a neutral head from rest."""
    best = None
    best_score = -1
    for cell in cells[1:]:
        score = body_diff_score(rest, cell)
        if score > best_score:
            best_score = score
            best = cell
    if best is None or best_score < 1000:
        return rest
    # Neutralize head so repeated runs don't stack bob offsets.
    return with_head_bob(best, rest, 0)


def pack_cells(cells, cw, h) -> Image.Image:
    sheet = Image.new("RGBA", (cw * len(cells), h), (0, 0, 0, 0))
    for i, cell in enumerate(cells):
        sheet.paste(Image.fromarray(cell, "RGBA"), (i * cw, 0))
    return sheet


def apply_head_bob(sheet_path: Path = SHEET, out_path: Path = OUT) -> Path:
    cells, cw, h = load_cells(sheet_path)
    rest = cells[0]
    sway = resolve_sway(cells, rest)

    frames = [
        with_head_bob(rest, rest, 0),
        with_head_bob(rest, rest, 3),
        with_head_bob(sway, rest, 5),
        with_head_bob(rest, rest, 2),
    ]

    out = pack_cells(frames, cw, h)
    out.save(out_path, "PNG")
    print(f"wrote {out_path.name}: {out.size[0]}x{out.size[1]} cells={len(frames)} {cw}x{h}")
    print("poses: rest, inhale(+3), peak sway(+5), exhale(+2)")
    return out_path


def main():
    apply_head_bob()


if __name__ == "__main__":
    main()
