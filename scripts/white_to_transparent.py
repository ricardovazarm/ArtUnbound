#!/usr/bin/env python3
"""
Replace white (and near-white) pixels with transparency in PNG images.
Use when IA-generated images have white background instead of transparent.
Usage: python white_to_transparent.py [file_or_dir ...]
Default: processes Assets/ArtUnbound/UI/Sprites/*.png
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


def is_white_or_near(pixel_rgb, threshold=240):
    """Consider white if R,G,B are all >= threshold. Default 240 catches off-white."""
    return all(c >= threshold for c in pixel_rgb[:3])


def white_to_transparent(input_path, output_path=None, threshold=240):
    """
    Replace white/near-white pixels with alpha=0.
    threshold: 255 = only pure white; 240 = off-white; 220 = light gray too.
    """
    img = Image.open(input_path).convert("RGBA")
    arr = np.array(img)
    r, g, b, a = arr[:, :, 0], arr[:, :, 1], arr[:, :, 2], arr[:, :, 3]
    # Pixel is "white" if all RGB >= threshold
    is_white = (r >= threshold) & (g >= threshold) & (b >= threshold)
    # Set alpha to 0 where white
    new_alpha = np.where(is_white, 0, a)
    arr[:, :, 3] = new_alpha
    out = output_path or input_path
    Image.fromarray(arr).save(out, "PNG")
    replaced = np.sum(is_white)
    print(f"Fixed: {input_path} ({replaced} white pixels -> transparent)")
    return replaced


def main():
    script_dir = Path(__file__).parent
    sprites_dir = script_dir.parent / "Assets" / "ArtUnbound" / "UI" / "Sprites"

    if len(sys.argv) > 1:
        paths = []
        for p in sys.argv[1:]:
            path = Path(p)
            if path.is_dir():
                paths.extend(path.glob("*.png"))
            elif path.exists():
                paths.append(path)
            else:
                print(f"Skip (not found): {path}")
        files = paths
    else:
        # Default: only Frame*.png (buttons have white glow — don't replace)
        files = list(sprites_dir.glob("Frame*.png"))

    if not files:
        print("No PNG files to process.")
        return

    for f in files:
        try:
            white_to_transparent(f, threshold=245)
        except Exception as e:
            print(f"Error {f}: {e}")

    print("\nDone. White pixels replaced with transparency.")


if __name__ == "__main__":
    main()
