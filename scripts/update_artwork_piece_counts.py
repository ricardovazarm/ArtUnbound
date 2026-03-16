#!/usr/bin/env python3
"""
Update ArtworkDefinition assets with piece counts calculated from image dimensions.

For each ArtworkDefinition in Data/Artworks:
1. Resolves the fullImage GUID to the actual image file in Artworks/
2. Reads image dimensions with PIL
3. Calculates piece counts (Easy, Normal, Hard, Expert) using PuzzleBoard logic
4. Updates the .asset file with pieceCountEasy, pieceCountNormal, pieceCountHard, pieceCountExpert

Run from project root:
    python scripts/update_artwork_piece_counts.py

Requires: Pillow (pip install Pillow)
"""
import math
import re
from pathlib import Path

try:
    from PIL import Image
except ImportError:
    import subprocess
    import sys
    subprocess.check_call([sys.executable, "-m", "pip", "install", "Pillow", "-q"])
    from PIL import Image

# Paths relative to project root
PROJECT_ROOT = Path(__file__).resolve().parent.parent
ARTWORKS_DIR = PROJECT_ROOT / "Assets" / "ArtUnbound" / "Artworks"
DATA_ARTWORKS_DIR = PROJECT_ROOT / "Assets" / "ArtUnbound" / "Data" / "Artworks"

TARGETS = [64, 121, 196, 289]
LABELS = ["Easy", "Normal", "Hard", "Expert"]


def calc_piece_count(target: int, width: int, height: int) -> int:
    """Same logic as PuzzleBoard.CalculateGridDimensions."""
    if width <= 0 or height <= 0:
        return 0
    ratio = width / height
    rows = round(math.sqrt(target / ratio))
    rows = max(2, rows)
    cols = round(rows * ratio)
    cols = max(2, cols)
    return rows * cols


def build_guid_to_image_map() -> dict[str, Path]:
    """Scan Artworks .meta files: guid -> image path."""
    guid_to_path: dict[str, Path] = {}
    for meta_path in ARTWORKS_DIR.rglob("*.meta"):
        # .meta for "image.jpg" is "image.jpg.meta" -> stem = "image.jpg"
        stem = meta_path.stem
        parent = meta_path.parent
        img_path = parent / stem
        # Only process if it's an image file (skip folder .meta like "AIC.meta")
        if not img_path.suffix.lower() in (".jpg", ".jpeg", ".png", ".webp"):
            continue
        if not img_path.is_file():
            continue
        content = meta_path.read_text(encoding="utf-8")
        m = re.search(r"^guid:\s+([a-f0-9]+)\s*$", content, re.MULTILINE)
        if m:
            guid_to_path[m.group(1).lower()] = img_path
    return guid_to_path


def extract_fullimage_guid(asset_content: str) -> str | None:
    """Extract GUID from fullImage: {fileID: 21300000, guid: xxx, type: 3}."""
    m = re.search(r"fullImage:\s*\{[^}]*guid:\s*([a-f0-9]+)", asset_content)
    if m:
        return m.group(1).lower()
    return None


def get_image_dimensions(img_path: Path) -> tuple[int, int] | None:
    """Return (width, height) or None on error."""
    try:
        with Image.open(img_path) as img:
            return img.size  # (width, height)
    except Exception as e:
        print(f"  Warning: Could not read {img_path}: {e}")
        return None


def update_asset_piece_counts(asset_path: Path, counts: list[int], dry_run: bool = False) -> bool:
    """
    Add or update pieceCountEasy/Normal/Hard/Expert in the asset YAML.
    Returns True if file was modified.
    """
    content = asset_path.read_text(encoding="utf-8")

    # Check if we have ArtworkDefinition (MonoBehaviour with our script)
    if "ArtUnbound.Data.ArtworkDefinition" not in content:
        return False

    new_block = (
        f"\n  pieceCountEasy: {counts[0]}\n"
        f"  pieceCountNormal: {counts[1]}\n"
        f"  pieceCountHard: {counts[2]}\n"
        f"  pieceCountExpert: {counts[3]}"
    )

    # Pattern: existing piece count block (all 4 fields)
    existing = re.search(
        r"\n\s+pieceCountEasy:\s*\d+\s*\n"
        r"\s+pieceCountNormal:\s*\d+\s*\n"
        r"\s+pieceCountHard:\s*\d+\s*\n"
        r"\s+pieceCountExpert:\s*\d+",
        content,
    )

    if existing:
        new_content = content[: existing.start()] + new_block + content[existing.end() :]
    else:
        # Add after detailLevel
        detail_match = re.search(r"\n\s+detailLevel:\s*\d+", content)
        if detail_match:
            insert_pos = detail_match.end()
            new_content = content[:insert_pos] + new_block + content[insert_pos:]
        else:
            new_content = content.rstrip() + new_block + "\n"

    if new_content != content:
        if not dry_run:
            asset_path.write_text(new_content, encoding="utf-8")
        return True
    return False


def main():
    import argparse
    parser = argparse.ArgumentParser(description="Update ArtworkDefinition piece counts from image dimensions")
    parser.add_argument("--dry-run", action="store_true", help="Print changes without writing")
    args = parser.parse_args()

    print("Building GUID -> image path map...")
    guid_to_path = build_guid_to_image_map()
    print(f"  Found {len(guid_to_path)} image GUIDs")

    updated = 0
    skipped = 0
    errors = 0

    for asset_path in sorted(DATA_ARTWORKS_DIR.rglob("*.asset")):
        content = asset_path.read_text(encoding="utf-8")
        if "ArtUnbound.Data.ArtworkDefinition" not in content:
            continue

        guid = extract_fullimage_guid(content)
        if not guid:
            print(f"  Skip {asset_path.name}: no fullImage GUID")
            skipped += 1
            continue

        img_path = guid_to_path.get(guid)
        if not img_path:
            print(f"  Skip {asset_path.name}: image GUID {guid} not found in Artworks")
            skipped += 1
            continue

        dims = get_image_dimensions(img_path)
        if not dims:
            errors += 1
            continue

        width, height = dims
        counts = [calc_piece_count(t, width, height) for t in TARGETS]

        name = asset_path.stem
        print(f"  {name}: {width}x{height} -> {counts[0]}/{counts[1]}/{counts[2]}/{counts[3]} pieces")

        if update_asset_piece_counts(asset_path, counts, dry_run=args.dry_run):
            updated += 1

    print()
    print(f"Updated: {updated}  Skipped: {skipped}  Errors: {errors}")
    if args.dry_run and updated:
        print("(dry-run: no files written)")


if __name__ == "__main__":
    main()
