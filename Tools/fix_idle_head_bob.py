"""Rebuild idle frames so the head breathes with the body.

Source frames often only animate cloak/torso. This composites a soft vertical
head bob onto rest + sway poses so the neck no longer looks welded in place.

Safe to re-run: rest is idle_0, sway body is recovered from a later frame.
"""

from pathlib import Path

import numpy as np
from PIL import Image

IDLE_DIR = Path(r"D:\game\Metroidvania\Assets\Art\Characters\Animations\idle")

# Head / shoulder blend band (frame coordinates).
SPLIT_Y = 355
FEATHER = 30


def load_cells(folder: Path, n: int = 4):
    cells = []
    cw = h = None
    for i in range(n):
        path = folder / f"idle_{i}.png"
        if not path.exists():
            raise FileNotFoundError(path)
        arr = np.array(Image.open(path).convert("RGBA"))
        if cw is None:
            h, cw = arr.shape[0], arr.shape[1]
        cells.append(arr)
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


def apply_head_bob(folder: Path = IDLE_DIR) -> Path:
    cells, cw, h = load_cells(folder)
    rest = cells[0]
    sway = resolve_sway(cells, rest)

    frames = [
        with_head_bob(rest, rest, 0),
        with_head_bob(rest, rest, 3),
        with_head_bob(sway, rest, 5),
        with_head_bob(rest, rest, 2),
    ]

    for i, frame in enumerate(frames):
        out = folder / f"idle_{i}.png"
        Image.fromarray(frame, "RGBA").save(out, "PNG")

    print(f"wrote {folder}: cells={len(frames)} {cw}x{h}")
    print("poses: rest, inhale(+3), peak sway(+5), exhale(+2)")
    return folder


def main():
    apply_head_bob()


if __name__ == "__main__":
    main()
