"""Split character animation sprite sheets into per-frame PNGs.

Reads Unity .meta sprite rects (Y flipped from bottom-left) and writes
RGBA frames under Tools/_unpacked_frames/<sheet_stem>/ — outside Assets,
so Unity does not import them until you move them on purpose.
"""
from __future__ import annotations

import argparse
import re
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ANIM = ROOT / "Assets" / "Art" / "Characters" / "Animations"
OUT_ROOT = ROOT / "Tools" / "_unpacked_frames"


def parse_rects(meta_path: Path) -> list[dict]:
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


def unpack_sheet(sheet_path: Path, out_dir: Path) -> list[Path]:
    meta_path = Path(str(sheet_path) + ".meta")
    if not meta_path.exists():
        raise FileNotFoundError(f"missing meta: {meta_path}")

    rects = parse_rects(meta_path)
    if not rects:
        raise SystemExit(f"no sprite rects in {meta_path.name}")

    im = Image.open(sheet_path).convert("RGBA")
    W, H = im.size
    out_dir.mkdir(parents=True, exist_ok=True)

    written: list[Path] = []
    for s in rects:
        x, y, w, h = s["x"], s["y"], s["w"], s["h"]
        # Unity rect origin is bottom-left; PIL is top-left.
        top = H - (y + h)
        cell = im.crop((x, top, x + w, top + h))
        out = out_dir / f"{s['name']}.png"
        cell.save(out, "PNG")
        written.append(out)
        print(f"  {out.name}  {w}x{h}  @({x},{y})")
    return written


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "sheets",
        nargs="*",
        type=Path,
        help="Sheet PNG paths (default: all *_sheet.png under Animations)",
    )
    parser.add_argument(
        "-o",
        "--out",
        type=Path,
        default=OUT_ROOT,
        help=f"Output root (default: {OUT_ROOT})",
    )
    args = parser.parse_args()

    sheets = args.sheets
    if not sheets:
        sheets = sorted(ANIM.glob("*_sheet.png"))

    if not sheets:
        raise SystemExit(f"no sheets found under {ANIM}")

    total = 0
    for sheet in sheets:
        sheet = sheet.resolve()
        if not sheet.exists():
            raise SystemExit(f"missing sheet: {sheet}")
        dest = args.out / sheet.stem
        print(f"{sheet.name} → {dest.relative_to(ROOT)}")
        written = unpack_sheet(sheet, dest)
        total += len(written)

    print(f"done: {total} frames → {args.out}")


if __name__ == "__main__":
    main()
