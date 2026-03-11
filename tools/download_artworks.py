#!/usr/bin/env python3
"""
Download artwork images from multiple museum APIs and generate JSON metadata
for Art Unbound ArtworkDefinition assets.

Supports: Art Institute of Chicago (AIC), Met Museum.
Other museums require manual image URLs (Rijksmuseum needs API key).
"""

import json
import os
import re
import sys
import time
import urllib.request
import urllib.parse
from pathlib import Path

# Output paths (relative to project root)
PROJECT_ROOT = Path(__file__).resolve().parent.parent
ARTWORKS_DIR = PROJECT_ROOT / "Assets" / "ArtUnbound" / "Artworks"
OUTPUT_JSON = PROJECT_ROOT / "Assets" / "ArtUnbound" / "Data" / "artworks_catalog.json"

# Museum API bases
AIC_API = "https://api.artic.edu/api/v1"
AIC_IIIF = "https://www.artic.edu/iiif/2"
MET_API = "https://collectionapi.metmuseum.org/public/collection/v1"

# Art movement: keep in English (no translation)
ART_MOVEMENT_MAP = {
    "Post-Impressionism": "Post-Impressionism",
    "Impressionism": "Impressionism",
    "Realism": "Realism",
    "Baroque": "Baroque",
    "Ukiyo-e": "Ukiyo-e",
    "Spanish Renaissance": "Spanish Renaissance",
    "Rococo": "Rococo",
    "Renaissance": "Renaissance",
}

# Catalog: museum, artist, title, art_movement, source_url, api_id (extracted or override)
# Note: The Basket of Apples AIC ID is 111436 (not 111442 which is The Child's Bath)
CATALOG = [
    # Art Institute of Chicago
    {"museum": "Art Institute of Chicago", "artist": "Georges Seurat", "title": "A Sunday on La Grande Jatte — 1884",
     "art_movement": "Post-Impressionism", "source": "aic", "api_id": "27992"},
    {"museum": "Art Institute of Chicago", "artist": "Vincent van Gogh", "title": "The Bedroom",
     "art_movement": "Post-Impressionism", "source": "aic", "api_id": "28560"},
    {"museum": "Art Institute of Chicago", "artist": "Vincent van Gogh", "title": "Self-Portrait (1887)",
     "art_movement": "Post-Impressionism", "source": "aic", "api_id": "80607"},
    {"museum": "Art Institute of Chicago", "artist": "Paul Cézanne", "title": "The Basket of Apples",
     "art_movement": "Post-Impressionism", "source": "aic", "api_id": "111436"},
    {"museum": "Art Institute of Chicago", "artist": "Pierre-Auguste Renoir", "title": "Two Sisters (On the Terrace)",
     "art_movement": "Impressionism", "source": "aic", "api_id": "14655"},
    {"museum": "Art Institute of Chicago", "artist": "Mary Cassatt", "title": "The Child's Bath",
     "art_movement": "Impressionism", "source": "aic", "api_id": "111442"},
    {"museum": "Art Institute of Chicago", "artist": "Paul Gauguin", "title": "Day of the God (Mahana No Atua)",
     "art_movement": "Post-Impressionism", "source": "aic", "api_id": "27943"},
    {"museum": "Art Institute of Chicago", "artist": "Toulouse-Lautrec", "title": "At the Moulin Rouge",
     "art_movement": "Post-Impressionism", "source": "aic", "api_id": "61128"},
    # The Met
    {"museum": "The Met (NYC)", "artist": "Claude Monet", "title": "Bridge over a Pond of Water Lilies",
     "art_movement": "Impressionism", "source": "manual", "api_id": None,
     "image_url": "https://upload.wikimedia.org/wikipedia/commons/5/53/Bridge_over_a_Pond_of_Water_Lilies_MET_DT1854.jpg"},
    {"museum": "The Met (NYC)", "artist": "Rosa Bonheur", "title": "The Horse Fair",
     "art_movement": "Realism", "source": "met", "api_id": "435702"},
    {"museum": "The Met (NYC)", "artist": "El Greco", "title": "View of Toledo",
     "art_movement": "Spanish Renaissance", "source": "met", "api_id": "436575"},
    {"museum": "The Met (NYC)", "artist": "Edgar Degas", "title": "The Dance Class",
     "art_movement": "Impressionism", "source": "met", "api_id": "436139"},
    {"museum": "The Met (NYC)", "artist": "Honoré Daumier", "title": "The Third-Class Carriage",
     "art_movement": "Realism", "source": "met", "api_id": "436095"},
    {"museum": "The Met (NYC)", "artist": "Johannes Vermeer", "title": "Young Woman with a Water Pitcher",
     "art_movement": "Baroque", "source": "met", "api_id": "437881"},
    {"museum": "The Met (NYC)", "artist": "Katsushika Hokusai", "title": "Under the Wave off Kanagawa",
     "art_movement": "Ukiyo-e", "source": "met", "api_id": "45434"},
    {"museum": "The Met (NYC)", "artist": "Rembrandt", "title": "Aristotle with a Bust of Homer",
     "art_movement": "Baroque", "source": "met", "api_id": "437394"},
    # Rijksmuseum (requires API key - add RIJKS_API_KEY env var)
    {"museum": "Rijksmuseum", "artist": "Rembrandt", "title": "The Night Watch",
     "art_movement": "Baroque", "source": "rijks", "api_id": "SK-C-5"},
    {"museum": "Rijksmuseum", "artist": "Johannes Vermeer", "title": "The Milkmaid",
     "art_movement": "Baroque", "source": "rijks", "api_id": "SK-A-2344"},
    {"museum": "Rijksmuseum", "artist": "Johannes Vermeer", "title": "The Little Street",
     "art_movement": "Baroque", "source": "rijks", "api_id": "SK-A-2860"},
    # Mauritshuis, NGA, Van Gogh Museum, NG UK, Yale - no public API; use manual image_url if available
    {"museum": "Mauritshuis", "artist": "Johannes Vermeer", "title": "Girl with a Pearl Earring",
     "art_movement": "Baroque", "source": "manual", "api_id": None, "image_url": None},
    {"museum": "Mauritshuis", "artist": "Johannes Vermeer", "title": "View of Delft",
     "art_movement": "Baroque", "source": "manual", "api_id": None, "image_url": None},
    {"museum": "Mauritshuis", "artist": "Rembrandt", "title": "The Anatomy Lesson of Dr. Nicolaes Tulp",
     "art_movement": "Baroque", "source": "manual", "api_id": None, "image_url": None},
    {"museum": "NGA (DC)", "artist": "Claude Monet", "title": "Woman with a Parasol",
     "art_movement": "Impressionism", "source": "manual", "api_id": None, "image_url": None},
    {"museum": "NGA (DC)", "artist": "Claude Monet", "title": "The Argenteuil Bridge",
     "art_movement": "Impressionism", "source": "manual", "api_id": None, "image_url": None},
    {"museum": "NGA (DC)", "artist": "J.H. Fragonard", "title": "A Young Girl Reading",
     "art_movement": "Rococo", "source": "manual", "api_id": None, "image_url": None},
    {"museum": "Van Gogh Museum", "artist": "Vincent van Gogh", "title": "Almond Blossom",
     "art_movement": "Post-Impressionism", "source": "manual", "api_id": None, "image_url": None},
    {"museum": "Van Gogh Museum", "artist": "Vincent van Gogh", "title": "The Harvest",
     "art_movement": "Post-Impressionism", "source": "manual", "api_id": None, "image_url": None},
    {"museum": "National Gallery UK", "artist": "Leonardo da Vinci", "title": "The Virgin of the Rocks",
     "art_movement": "Renaissance", "source": "manual", "api_id": None, "image_url": None},
    {"museum": "National Gallery UK", "artist": "Diego Velázquez", "title": "The Toilet of Venus",
     "art_movement": "Baroque", "source": "manual", "api_id": None, "image_url": None},
    {"museum": "Yale Art Gallery", "artist": "Vincent van Gogh", "title": "The Night Café",
     "art_movement": "Post-Impressionism", "source": "manual", "api_id": None, "image_url": None},
]


def http_get_json(url, headers=None):
    h = {"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"}
    if headers:
        h.update(headers)
    req = urllib.request.Request(url, headers=h)
    with urllib.request.urlopen(req, timeout=30) as resp:
        return json.loads(resp.read().decode("utf-8"))


def download_file(url, out_path):
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"})
    with urllib.request.urlopen(req, timeout=60) as resp:
        content = resp.read()
    out_path.parent.mkdir(parents=True, exist_ok=True)
    with open(out_path, "wb") as f:
        f.write(content)


def sanitize_filename(name, max_length=80):
    """Create a filesystem-safe filename from artwork title."""
    name = re.sub(r'[<>:"/\\|?*]', '', name)
    name = re.sub(r'[\0-\31]', '', name)
    name = name.replace('—', '-').replace('–', '-')
    name = name.strip()
    if len(name) > max_length:
        name = name[:max_length].rsplit(' ', 1)[0]  # avoid cutting mid-word
    return name or "unknown"


def make_artwork_id(museum_short, artist, title):
    """Generate unique artworkId like Met-436532 or AIC-27992."""
    prefix = {"Art Institute of Chicago": "AIC", "The Met (NYC)": "Met", "Rijksmuseum": "Rijks",
              "Mauritshuis": "Mauritshuis", "NGA (DC)": "NGA", "Van Gogh Museum": "VGM",
              "National Gallery UK": "NGUK", "Yale Art Gallery": "Yale"}.get(museum_short, "Art")
    safe = re.sub(r'[^a-zA-Z0-9]', '', title)[:30]
    return f"{prefix}-{safe}"


def fetch_aic(api_id):
    """Fetch metadata and image URL from Art Institute of Chicago API."""
    url = f"{AIC_API}/artworks/{api_id}?fields=id,title,artist_display,date_start,date_end,image_id,thumbnail,dimensions_detail"
    data = http_get_json(url)
    d = data.get("data", {})
    if not d:
        return None
    image_id = d.get("image_id")
    if not image_id:
        return None
    image_url = f"{AIC_IIIF}/{image_id}/full/max/0/default.jpg"
    dims = d.get("dimensions_detail", [{}])[0]
    w = dims.get("width") or 1
    h = dims.get("height") or 1
    aspect = w / h if h else 1.0
    return {
        "title": d.get("title", ""),
        "author": d.get("artist_display", "").split("(")[0].strip(),
        "year": d.get("date_start") or 0,
        "description": "",
        "image_url": image_url,
        "aspect_ratio": aspect,
        "raw": d,
    }


def fetch_met(api_id):
    """Fetch metadata and image URL from Met Museum API."""
    url = f"{MET_API}/objects/{api_id}"
    data = http_get_json(url)
    image_url = data.get("primaryImage")
    if not image_url:
        return None
    measurements = data.get("measurements", [{}])
    w, h = 1, 1
    for m in measurements:
        ms = m.get("elementMeasurements", {})
        if "Width" in ms and "Height" in ms:
            w = ms["Width"]
            h = ms["Height"]
            break
    aspect = w / h if h else 1.0
    return {
        "title": data.get("title", ""),
        "author": data.get("artistDisplayName", ""),
        "year": int(data.get("objectBeginDate", 0) or 0),
        "description": data.get("objectName", ""),
        "image_url": image_url,
        "aspect_ratio": aspect,
        "raw": data,
    }


def fetch_rijks(api_id, api_key):
    """Fetch from Rijksmuseum API (requires free API key)."""
    if not api_key:
        return None
    url = f"https://www.rijksmuseum.nl/api/en/collection/{api_id}?key={api_key}"
    try:
        data = http_get_json(url)
    except Exception:
        return None
    art = data.get("artObject", {})
    web = art.get("webImage", {})
    if not web:
        return None
    image_url = web.get("url")
    if not image_url:
        return None
    w = web.get("width", 1)
    h = web.get("height", 1)
    aspect = w / h if h else 1.0
    return {
        "title": art.get("title", ""),
        "author": art.get("principalOrFirstMaker", ""),
        "year": int(art.get("dating", {}).get("yearEarly", 0) or 0),
        "description": art.get("title", ""),
        "image_url": image_url,
        "aspect_ratio": aspect,
        "raw": art,
    }


def build_artwork_definition(entry, meta, filename):
    """Build JSON dict matching ArtworkDefinition fields for Unity."""
    art_movement = entry.get("art_movement", "")
    art_movement_display = ART_MOVEMENT_MAP.get(art_movement, art_movement)
    artwork_id = make_artwork_id(entry["museum"], entry["artist"], entry["title"])

    return {
        "artworkId": artwork_id,
        "title": meta.get("title", entry["title"]),
        "author": meta.get("author", entry["artist"]),
        "year": meta.get("year", 0),
        "description": meta.get("description", ""),
        "museum": entry["museum"],
        "artMovement": art_movement_display,
        "aspectRatio": meta.get("aspect_ratio", 1.0),
        "baseSizePortrait": {"x": 0.5, "y": 0.7},
        "baseSizeLandscape": {"x": 0.7, "y": 0.5},
        "textureFilename": filename,
        "isBaseContent": True,
        "requiresUnlock": False,
        "unlockWeek": 0,
        "complexity": "Medium",
        "colorVariety": 3,
        "detailLevel": 3,
    }


def main():
    os.chdir(PROJECT_ROOT)
    ARTWORKS_DIR.mkdir(parents=True, exist_ok=True)

    rijks_key = os.environ.get("RIJKS_API_KEY", "")

    catalog_json = []
    downloaded = 0
    skipped = 0

    for i, entry in enumerate(CATALOG):
        source = entry.get("source", "").lower()
        title = entry["title"]
        artist = entry["artist"]
        museum = entry["museum"]

        print(f"\n[{i + 1}/{len(CATALOG)}] {title} - {artist} ({museum})")

        meta = None
        image_url = entry.get("image_url")

        if source == "aic":
            meta = fetch_aic(entry["api_id"])
            if meta:
                image_url = meta["image_url"]
        elif source == "met":
            meta = fetch_met(entry["api_id"])
            if meta:
                image_url = meta["image_url"]
        elif source == "rijks" and rijks_key:
            meta = fetch_rijks(entry["api_id"], rijks_key)
            if meta:
                image_url = meta["image_url"]

        if not meta:
            meta = {
                "title": title,
                "author": artist,
                "year": 0,
                "description": "",
                "aspect_ratio": 1.0,
            }

        # Manual entries can have image_url for direct download (e.g. Wikimedia)
        if source == "manual" and entry.get("image_url"):
            image_url = entry["image_url"]
            # Use catalog year if available for known works
            if "Bridge over a Pond" in title:
                meta["year"] = 1899
            elif "Girl with a Pearl" in title:
                meta["year"] = 1665
            elif "View of Delft" in title:
                meta["year"] = 1661

        if not image_url:
            print(f"  SKIP: No image URL (source={source})")
            if source == "manual":
                catalog_json.append(build_artwork_definition(entry, meta, ""))
            skipped += 1
            time.sleep(0.5)
            continue

        safe_name = sanitize_filename(title)
        ext = ".jpg" if "jpg" in image_url.lower() or "jpeg" in image_url.lower() else ".png"
        filename = f"{safe_name}{ext}"
        out_path = ARTWORKS_DIR / filename

        try:
            if out_path.exists():
                print(f"  EXISTS: {filename}")
            else:
                download_file(image_url, out_path)
                print(f"  DOWNLOADED: {filename}")
            downloaded += 1
        except Exception as e:
            print(f"  ERROR: {e}")
            skipped += 1
            time.sleep(1)
            continue

        def_entry = build_artwork_definition(entry, meta, filename)
        catalog_json.append(def_entry)
        time.sleep(0.5)  # rate limit

    OUTPUT_JSON.parent.mkdir(parents=True, exist_ok=True)
    with open(OUTPUT_JSON, "w", encoding="utf-8") as f:
        json.dump(catalog_json, f, ensure_ascii=False, indent=2)

    print(f"\n--- Done ---")
    print(f"Downloaded: {downloaded} | Skipped: {skipped}")
    print(f"Images: {ARTWORKS_DIR}")
    print(f"JSON: {OUTPUT_JSON}")
    print("\nNext steps:")
    print("1. In Unity: Import images with Texture Type=Sprite, Read/Write enabled, Max Size 4096")
    print("2. Create ArtworkDefinition assets from the JSON (or use a custom editor script)")
    print("3. Add them to ArtworkCatalog.asset")
    return 0


if __name__ == "__main__":
    sys.exit(main())
