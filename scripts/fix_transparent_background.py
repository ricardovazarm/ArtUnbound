#!/usr/bin/env python3
"""
Fix PNG images that have checkerboard pattern baked in instead of true transparency.
Replaces checkerboard (alternating light/dark gray squares) with actual alpha=0.
"""
import sys
from pathlib import Path

try:
    from PIL import Image
    import numpy as np
except ImportError:
    print("Installing Pillow and numpy...")
    import subprocess
    subprocess.check_call([sys.executable, "-m", "pip", "install", "Pillow", "numpy", "-q"])
    from PIL import Image
    import numpy as np


def get_checkerboard_colors(img_array, margin=20):
    """Sample corners/edges to find the two checkerboard colors."""
    h, w = img_array.shape[:2]
    samples = []
    # Sample corners and edges
    for y in [margin, h - margin - 1]:
        for x in range(margin, w - margin, max(1, (w - 2 * margin) // 10)):
            samples.append(tuple(img_array[y, x, :3]))
    for x in [margin, w - margin - 1]:
        for y in range(margin, h - margin, max(1, (h - 2 * margin) // 10)):
            samples.append(tuple(img_array[y, x, :3]))
    # Get two most common (checkerboard)
    from collections import Counter
    counts = Counter(samples)
    common = counts.most_common(2)
    return [c[0] for c in common]


def color_distance(c1, c2):
    return sum((a - b) ** 2 for a, b in zip(c1, c2)) ** 0.5


def fix_transparency(input_path, output_path=None, tolerance=25):
    """Replace checkerboard background with true transparency."""
    img = Image.open(input_path).convert("RGBA")
    arr = np.array(img)
    
    if output_path is None:
        output_path = input_path
    
    # Get checkerboard colors from edges
    try:
        c1, c2 = get_checkerboard_colors(arr)
    except Exception:
        # Fallback: common checkerboard grays
        c1 = (192, 192, 192)
        c2 = (128, 128, 128)
    
    # Create alpha: 0 where pixel matches checkerboard, else keep original alpha
    r, g, b, a = arr[:, :, 0], arr[:, :, 1], arr[:, :, 2], arr[:, :, 3]
    
    dist1 = np.sqrt((r.astype(float) - c1[0])**2 + (g.astype(float) - c1[1])**2 + (b.astype(float) - c1[2])**2)
    dist2 = np.sqrt((r.astype(float) - c2[0])**2 + (g.astype(float) - c2[1])**2 + (b.astype(float) - c2[2])**2)
    
    is_checkerboard = (dist1 < tolerance) | (dist2 < tolerance)
    new_alpha = np.where(is_checkerboard, 0, a)
    arr[:, :, 3] = new_alpha
    
    Image.fromarray(arr).save(output_path, "PNG")
    print(f"Fixed: {input_path}")


def main():
    script_dir = Path(__file__).parent
    sprites_dir = script_dir.parent / "Assets" / "ArtUnbound" / "UI" / "Sprites"
    
    # All generated button and frame PNGs
    files = [
        "ButtonPill.png", "ButtonCircleGlossy.png", "ButtonPrimary.png",
        "FrameThumbnail.png", "FrameDetail.png", "FrameMadera.png",
        "FrameOro.png", "FramePlata.png", "FrameBronce.png"
    ]
    
    for f in files:
        path = sprites_dir / f
        if path.exists():
            fix_transparency(path)
        else:
            print(f"Skip (not found): {path}")


if __name__ == "__main__":
    main()
