#!/usr/bin/env python3
"""
Generate UI sprites (buttons and frames) with REAL transparency.
Uses PIL to draw shapes on transparent RGBA canvas — no checkerboard, no baked background.
Output: Assets/ArtUnbound/UI/Sprites/
"""
import random
import sys
from pathlib import Path

try:
    from PIL import Image, ImageDraw
except ImportError:
    print("Installing Pillow...")
    import subprocess
    subprocess.check_call([sys.executable, "-m", "pip", "install", "Pillow", "-q"])
    from PIL import Image, ImageDraw


def draw_rounded_rect(draw, xy, radius, fill, outline=None, width=1):
    """Draw rounded rectangle. PIL's rounded_rectangle (Pillow 9+)."""
    x1, y1, x2, y2 = xy
    draw.rounded_rectangle(xy, radius=radius, fill=fill, outline=outline, width=width)


def create_button_pill(size=(256, 96), radius=48):
    """Pill-shaped button: gray semi-transparent, subtle white glow. Fully transparent background."""
    w, h = size
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Glow layers (white, decreasing alpha)
    for i in range(4, 0, -1):
        pad = i * 4
        alpha = 40 - i * 8
        draw.rounded_rectangle(
            (pad, pad, w - pad, h - pad),
            radius=radius - pad,
            fill=(255, 255, 255, alpha),
        )
    # Main pill: dark gray semi-transparent
    pad = 8
    draw.rounded_rectangle(
        (pad, pad, w - pad, h - pad),
        radius=radius - pad,
        fill=(80, 80, 90, 200),
        outline=(255, 255, 255, 100),
        width=2,
    )
    return img


def create_button_circle_glossy(size=(128, 128)):
    """Circular glossy/bubble button. Transparent background."""
    w, h = size
    cx, cy = w // 2, h // 2
    r = min(w, h) // 2 - 8
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Outer glow
    for i in range(3, 0, -1):
        rr = r + i * 6
        alpha = 50 - i * 12
        draw.ellipse((cx - rr, cy - rr, cx + rr, cy + rr), fill=(255, 255, 255, alpha))
    # Main circle: dark gray
    draw.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(70, 75, 85, 230))
    # Glossy highlight (top-left quadrant)
    hl_r = r // 2
    hl_cx, hl_cy = cx - r // 4, cy - r // 4
    draw.ellipse(
        (hl_cx - hl_r, hl_cy - hl_r, hl_cx + hl_r, hl_cy + hl_r),
        fill=(255, 255, 255, 60),
    )
    # Subtle border
    draw.ellipse((cx - r, cy - r, cx + r, cy + r), outline=(255, 255, 255, 120), width=2)
    return img


def create_button_primary(size=(256, 64), radius=24):
    """Primary button: elongated rectangle, rounded corners, glow on edges. Transparent background."""
    w, h = size
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Glow
    for i in range(3, 0, -1):
        pad = i * 3
        alpha = 35 - i * 8
        draw.rounded_rectangle(
            (pad, pad, w - pad, h - pad),
            radius=radius - pad,
            fill=(255, 255, 255, alpha),
        )
    # Main rect
    pad = 6
    draw.rounded_rectangle(
        (pad, pad, w - pad, h - pad),
        radius=radius - pad,
        fill=(60, 65, 80, 220),
        outline=(255, 255, 255, 130),
        width=2,
    )
    return img


def create_panel_rounded(size=(256, 256), radius=28, color_rgba=(71, 65, 51, 200), border_rgba=(220, 210, 170, 180)):
    """
    Rounded rectangle for panel background. Semi-transparent dark bronze/grey
    with a subtle light border. 9-slice friendly (spriteBorder 28).
    """
    w, h = size
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    draw.rounded_rectangle((0, 0, w - 1, h - 1), radius=radius, fill=color_rgba)
    draw.rounded_rectangle((0, 0, w - 1, h - 1), radius=radius, fill=None, outline=border_rgba, width=3)
    return img


def create_panel_rounded_border(size=(256, 256), radius=28, border_width=3, color_rgba=(220, 210, 170, 180)):
    """
    Rounded rectangle border only — transparent center, subtle outline.
    Matches PanelRounded dimensions for overlay. 9-slice friendly (spriteBorder ~28).
    """
    w, h = size
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    draw.rounded_rectangle(
        (0, 0, w - 1, h - 1),
        radius=radius,
        fill=None,
        outline=color_rgba,
        width=border_width,
    )
    return img


def create_frame(size=(128, 128), border=32, color_rgb=(180, 150, 80), inner_transparent=True):
    """
    Frame with transparent center. Border is solid color.
    For 9-slice: corners and edges are drawn; center stays transparent.
    """
    w, h = size
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    b = border
    # Outer rect (full frame outline)
    draw.rectangle((0, 0, w - 1, h - 1), outline=color_rgb + (255,), width=b)
    # Fill the border area (frame) — we need to fill 4 L-shaped regions
    # Top bar
    draw.rectangle((0, 0, w, b), fill=color_rgb + (255,))
    # Bottom bar
    draw.rectangle((0, h - b, w, h), fill=color_rgb + (255,))
    # Left bar
    draw.rectangle((0, 0, b, h), fill=color_rgb + (255,))
    # Right bar
    draw.rectangle((w - b, 0, w, h), fill=color_rgb + (255,))
    # Inner area stays transparent (we didn't draw there)
    return img


def create_frame_wood(size=(695, 695), border=80, seed=42):
    """
    Natural wood frame — light oak, visible grain, subtle depth.
    Horizontal grain on top/bottom, vertical on sides. Not flat or artificial.
    """
    random.seed(seed)
    w, h = size
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    pixels = img.load()
    b = border
    # Light oak base (R, G, B)
    base = (218, 195, 155)
    grain_dark = (185, 165, 125)
    inner_shadow = (195, 175, 135)
    outer_highlight = (235, 218, 185)

    def clamp(c):
        return max(0, min(255, int(c)))

    def draw_bar(x1, y1, x2, y2, horizontal_grain):
        """Draw a bar with wood grain. horizontal_grain=True = lines run left-right."""
        for py in range(y1, y2):
            for px in range(x1, x2):
                dist_inner = min(px - x1, py - y1, x2 - 1 - px, y2 - 1 - py)
                dist_outer = min(px, py, w - 1 - px, h - 1 - py) if (x1 == 0 or y1 == 0) else dist_inner
                # Subtle gradient: darker toward inner edge
                t = dist_inner / max(1, b * 0.3)
                r, g, b_ = base
                r = clamp(r - t * 15)
                g = clamp(g - t * 12)
                b_ = clamp(b_ - t * 8)
                # Wood grain: alternate slightly darker lines
                if horizontal_grain:
                    grain_pos = py
                else:
                    grain_pos = px
                if grain_pos % 8 < 2 or (grain_pos + random.randint(0, 2)) % 12 < 2:
                    r = clamp(r - 18)
                    g = clamp(g - 15)
                    b_ = clamp(b_ - 10)
                pixels[px, py] = (r, g, b_, 255)

    # Top bar (horizontal grain)
    draw_bar(0, 0, w, b, True)
    # Bottom bar
    draw_bar(0, h - b, w, h, True)
    # Left bar (vertical grain)
    draw_bar(0, b, b, h - b, False)
    # Right bar
    draw_bar(w - b, b, w, h - b, False)
    return img


def create_frame_material(size=(695, 695), border=40, base_rgb=(180, 150, 80), highlight_rgb=None, shadow_rgb=None):
    """
    Frame with same shape as FrameDetail (wood): border bars, transparent center.
    Uses material-specific colors for metallic look. Same layout as create_frame_wood.
    """
    w, h = size
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    pixels = img.load()
    b = border

    def clamp(c):
        return max(0, min(255, int(c)))

    # Default highlight/shadow from base
    if highlight_rgb is None:
        highlight_rgb = tuple(clamp(c * 1.15) for c in base_rgb)
    if shadow_rgb is None:
        shadow_rgb = tuple(clamp(c * 0.75) for c in base_rgb)

    def draw_bar(x1, y1, x2, y2, horizontal):
        """Draw bar with gradient: darker inner, lighter outer. Same shape as wood frame."""
        for py in range(y1, y2):
            for px in range(x1, x2):
                dist_inner = min(px - x1, py - y1, x2 - 1 - px, y2 - 1 - py)
                t = min(1.0, dist_inner / max(1, b * 0.35))
                # Blend: shadow (inner) -> base -> highlight (outer)
                r = clamp(base_rgb[0] * (1 - t) + shadow_rgb[0] * t)
                g = clamp(base_rgb[1] * (1 - t) + shadow_rgb[1] * t)
                b_ = clamp(base_rgb[2] * (1 - t) + shadow_rgb[2] * t)
                # Subtle outer highlight
                dist_outer = min(px - x1, py - y1, x2 - 1 - px, y2 - 1 - py) if horizontal else min(px - x1, py - y1, x2 - 1 - px, y2 - 1 - py)
                if dist_outer < b // 4:
                    blend = 1 - dist_outer / (b / 4)
                    r = clamp(r + (highlight_rgb[0] - r) * blend * 0.5)
                    g = clamp(g + (highlight_rgb[1] - g) * blend * 0.5)
                    b_ = clamp(b_ + (highlight_rgb[2] - b_) * blend * 0.5)
                pixels[px, py] = (r, g, b_, 255)

    draw_bar(0, 0, w, b, True)
    draw_bar(0, h - b, w, h, True)
    draw_bar(0, b, b, h - b, False)
    draw_bar(w - b, b, w, h - b, False)
    return img


def create_frame_layered(size=(695, 695), border=174, inner_transparent=True):
    """
    Frame with depth and layering — warm golden-brown, polished wood look.
    Outer band, recessed middle, darker inner bevel. Scales with size.
    """
    w, h = size
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    b = border
    o = max(4, int(border * 10 / 32))
    m = max(4, int(border * 10 / 32))
    i = max(2, int(border * 8 / 32))
    outer_rgba = (180, 155, 110, 255)
    mid_rgba = (130, 108, 75, 255)
    inner_rgba = (95, 80, 55, 255)

    def fill_top(y1, y2, color):
        draw.rectangle((0, y1, w, y2), fill=color)
    def fill_bottom(y1, y2, color):
        draw.rectangle((0, h - y2, w, h - y1), fill=color)
    def fill_left(x1, x2, color):
        draw.rectangle((x1, 0, x2, h), fill=color)
    def fill_right(x1, x2, color):
        draw.rectangle((w - x2, 0, w - x1, h), fill=color)

    fill_top(0, o, outer_rgba)
    fill_bottom(0, o, outer_rgba)
    fill_left(0, o, outer_rgba)
    fill_right(0, o, outer_rgba)
    fill_top(o, o + m, mid_rgba)
    fill_bottom(o, o + m, mid_rgba)
    fill_left(o, o + m, mid_rgba)
    fill_right(o, o + m, mid_rgba)
    fill_top(o + m, b, inner_rgba)
    fill_bottom(o + m, b, inner_rgba)
    fill_left(o + m, b, inner_rgba)
    fill_right(o + m, b, inner_rgba)
    return img


def main():
    script_dir = Path(__file__).parent
    out_dir = script_dir.parent / "Assets" / "ArtUnbound" / "UI" / "Sprites"
    out_dir.mkdir(parents=True, exist_ok=True)

    # --- Buttons ---
    create_button_pill().save(out_dir / "ButtonPill.png")
    print("Generated: ButtonPill.png")

    create_button_circle_glossy().save(out_dir / "ButtonCircleGlossy.png")
    print("Generated: ButtonCircleGlossy.png")

    create_button_primary().save(out_dir / "ButtonPrimary.png")
    print("Generated: ButtonPrimary.png")

    create_panel_rounded().save(out_dir / "PanelRounded.png")
    print("Generated: PanelRounded.png (with built-in border)")

    # --- Frames: FrameDetail shape (wood) as base. Madera = copy of FrameDetail. ---
    frame_detail = create_frame_wood(size=(695, 695), border=40)
    frame_detail.save(out_dir / "FrameThumbnail.png")
    print("Generated: FrameThumbnail.png (wood, 695x695)")
    frame_detail.save(out_dir / "FrameDetail.png")
    print("Generated: FrameDetail.png (wood, 695x695)")
    frame_detail.save(out_dir / "FrameMadera.png")
    print("Generated: FrameMadera.png (same shape as FrameDetail, wood)")

    # --- Tier frames: same shape as FrameDetail, different materials ---
    create_frame_material(
        size=(695, 695), border=40,
        base_rgb=(205, 127, 50),
        highlight_rgb=(230, 160, 90),
        shadow_rgb=(140, 85, 35),
    ).save(out_dir / "FrameBronce.png")
    print("Generated: FrameBronce.png (same shape, bronze)")

    create_frame_material(
        size=(695, 695), border=40,
        base_rgb=(192, 192, 192),
        highlight_rgb=(220, 220, 220),
        shadow_rgb=(140, 140, 140),
    ).save(out_dir / "FramePlata.png")
    print("Generated: FramePlata.png (same shape, silver)")

    create_frame_material(
        size=(695, 695), border=40,
        base_rgb=(218, 165, 32),
        highlight_rgb=(255, 215, 100),
        shadow_rgb=(160, 120, 20),
    ).save(out_dir / "FrameOro.png")
    print("Generated: FrameOro.png (same shape, gold)")

    create_frame_material(
        size=(695, 695), border=40,
        base_rgb=(55, 50, 45),
        highlight_rgb=(85, 78, 70),
        shadow_rgb=(35, 32, 28),
    ).save(out_dir / "FrameEbano.png")
    print("Generated: FrameEbano.png (same shape, ebony/platinum)")

    print("\nDone. Buttons and frames have transparent backgrounds.")


if __name__ == "__main__":
    main()
