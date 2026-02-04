#!/usr/bin/env python3
import json
import os
import sys
import urllib.request
import urllib.parse


MET_API_BASE = "https://collectionapi.metmuseum.org/public/collection/v1"


def http_get_json(url):
    req = urllib.request.Request(url, headers={"User-Agent": "ArtUnbound/1.0"})
    with urllib.request.urlopen(req) as resp:
        data = resp.read().decode("utf-8")
    return json.loads(data)


def download_file(url, out_path):
    req = urllib.request.Request(url, headers={"User-Agent": "ArtUnbound/1.0"})
    with urllib.request.urlopen(req) as resp:
        content = resp.read()
    with open(out_path, "wb") as f:
        f.write(content)


import re

def sanitize_filename(name, object_id, max_length=20):
    # Truncate at common delimiters (first occurrence)
    delimiters = [':', '-', '(', ';', '|']
    for delimiter in delimiters:
        if delimiter in name:
            name = name.split(delimiter)[0]
            break
    
    # Remove invalid characters for Windows/Linux filenames
    name = re.sub(r'[<>:"/\\|?*]', '', name)
    # Remove control characters
    name = re.sub(r'[\0-\31]', '', name)
    # Truncate to max_length
    if len(name) > max_length:
        name = name[:max_length]
    
    # Prefix with object_id
    return f"{object_id}-{name.strip()}"

def main():
    artist = "Vincent van Gogh"
    out_root = os.path.join("Assets", "ArtUnbound", "Artworks", "Met")
    os.makedirs(out_root, exist_ok=True)
    max_count = 10
    if len(sys.argv) > 1:
        try:
            max_count = max(1, int(sys.argv[1]))
        except ValueError:
            print("Usage: python download_met_artwork.py [count]")
            return 1

    params = {
        "q": artist,
        "hasImages": "true",
        "isPublicDomain": "true",
    }
    search_url = f"{MET_API_BASE}/search?{urllib.parse.urlencode(params)}"
    search = http_get_json(search_url)
    object_ids = search.get("objectIDs") or []
    if not object_ids:
        print("No public domain artworks found for:", artist)
        return 1

    downloaded = 0
    for object_id in object_ids:
        if downloaded >= max_count:
            break

        object_url = f"{MET_API_BASE}/objects/{object_id}"
        data = http_get_json(object_url)
        image_url = data.get("primaryImage")
        if not image_url:
            continue

        # Get author name and sanitize for folder name
        author_name = data.get("artistDisplayName", "Unknown")
        safe_author = sanitize_filename(author_name, "", max_length=50).replace("-", "")
        
        # Create directory by author
        art_dir = os.path.join(out_root, safe_author)
        os.makedirs(art_dir, exist_ok=True)

        # Save metadata with object_id in filename
        meta_path = os.path.join(art_dir, f"metadata_{object_id}.json")
        with open(meta_path, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)

        image_ext = os.path.splitext(urllib.parse.urlparse(image_url).path)[1] or ".jpg"
        
        # Use sanitized title for filename
        raw_title = data.get("title", "image")
        safe_title = sanitize_filename(raw_title, object_id)
        image_path = os.path.join(art_dir, f"{safe_title}{image_ext}")
        
        download_file(image_url, image_path)

        downloaded += 1
        print("Downloaded:", data.get("title"), "->", image_path)
        print("Metadata:", meta_path)

    if downloaded == 0:
        print("No downloadable images found.")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
