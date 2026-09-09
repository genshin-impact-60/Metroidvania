"""Convert outer black of door crop to green, then rembg."""
from __future__ import annotations

from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image
from rembg import new_session, remove

from cutout_doors_green import crop_alpha, despill

WORK = Path(r"D:\game\Metroidvania\Tools\_door_cutout_work")
OUT = Path(r"D:\game\Metroidvania\Assets\Art\Environment\Doors")


def flood_black_to_green(im: Image.Image, thresh: int = 28) -> Image.Image:
    """Flood-fill near-black connected to image edges -> chroma green."""
    arr = np.array(im.convert("RGB"))
    h, w = arr.shape[:2]
    lum = arr.astype(np.int16).sum(axis=2)
    is_black = lum <= thresh * 3
    visited = np.zeros((h, w), dtype=bool)
    q: deque[tuple[int, int]] = deque()
    for x in range(w):
        for y in (0, h - 1):
            if is_black[y, x] and not visited[y, x]:
                visited[y, x] = True
                q.append((x, y))
    for y in range(h):
        for x in (0, w - 1):
            if is_black[y, x] and not visited[y, x]:
                visited[y, x] = True
                q.append((x, y))
    while q:
        x, y = q.popleft()
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= nx < w and 0 <= ny < h and is_black[ny, nx] and not visited[ny, nx]:
                visited[ny, nx] = True
                q.append((nx, ny))
    arr[visited] = (0, 255, 0)
    return Image.fromarray(arr, "RGB")


def main() -> None:
    session = new_session("birefnet-general-lite")
    # Prefer AI green gens when they exist and look single-door; always rebuild open from ref.
    src = WORK / "ref_open.png"
    green = flood_black_to_green(Image.open(src))
    green_path = WORK / "open_flood_green.png"
    green.save(green_path)
    pad = 40
    canvas = Image.new("RGB", (green.width + pad * 2, green.height + pad * 2), (0, 255, 0))
    canvas.paste(green, (pad, pad))
    cut = remove(canvas, session=session)
    cut = despill(cut)
    cut = crop_alpha(cut)
    dest = OUT / "map_door_common_open.png"
    cut.save(dest)
    cut.save(WORK / dest.name)
    print(f"{dest.name}: {cut.size} {cut.mode}")


if __name__ == "__main__":
    main()
