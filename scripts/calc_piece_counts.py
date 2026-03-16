#!/usr/bin/env python3
"""
Calculate piece counts for Art Unbound artwork puzzles.

Given image dimensions (width x height), returns the 4 piece counts for
Easy, Normal, Hard, and Expert difficulties. Uses the same algorithm as
PuzzleBoard.CalculateGridDimensions in Unity.

Usage:
    python scripts/calc_piece_counts.py 3000 2009
    python scripts/calc_piece_counts.py 3000x2009

Output:
    Easy:   77 pieces
    Normal: 126 pieces
    Hard:   196 pieces
    Expert: 289 pieces

Copy these values into the ArtworkDefinition asset (pieceCountEasy, etc.).
"""
import math
import sys


# Target counts from PuzzleConfig (8², 11², 14², 17²)
TARGETS = [64, 121, 196, 289]


def calc_piece_count(target: int, width: int, height: int) -> int:
    """
    Replicates Unity PuzzleBoard.CalculateGridDimensions logic.
    aspectRatio = width / height
    rows = round(sqrt(target / aspectRatio))
    cols = round(rows * aspectRatio)
    return rows * cols
    """
    if width <= 0 or height <= 0:
        return 0
    ratio = width / height
    rows = round(math.sqrt(target / ratio))
    rows = max(2, rows)
    cols = round(rows * ratio)
    cols = max(2, cols)
    return rows * cols


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        print("\nExample: python scripts/calc_piece_counts.py 3000 2009")
        sys.exit(1)

    # Parse dimensions: "3000 2009" or "3000x2009"
    dim_str = " ".join(sys.argv[1:]).replace("x", " ").replace("×", " ")
    parts = dim_str.split()
    if len(parts) != 2:
        print("Error: Provide width and height (e.g. 3000 2009 or 3000x2009)")
        sys.exit(1)

    try:
        width = int(parts[0])
        height = int(parts[1])
    except ValueError:
        print("Error: Dimensions must be integers")
        sys.exit(1)

    print(f"Dimensions: {width} × {height}")
    print(f"Aspect ratio: {width/height:.3f}")
    print()
    print("Piece counts for ArtworkDefinition:")
    print("-" * 40)

    labels = ["Easy", "Normal", "Hard", "Expert"]
    for i, target in enumerate(TARGETS):
        count = calc_piece_count(target, width, height)
        print(f"  pieceCount{labels[i]:8} = {count}")

    print()
    print("Copy these values into your ArtworkDefinition asset.")


if __name__ == "__main__":
    main()
