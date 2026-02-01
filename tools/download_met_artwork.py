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


def main():
    artist = "Vincent van Gogh"
    out_root = os.path.join("Assets", "ArtUnbound", "Artworks", "Met")
    os.makedirs(out_root, exist_ok=True)
    max_count = 1
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

        art_dir = os.path.join(out_root, str(object_id))
        os.makedirs(art_dir, exist_ok=True)

        meta_path = os.path.join(art_dir, "metadata.json")
        with open(meta_path, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)

        image_ext = os.path.splitext(urllib.parse.urlparse(image_url).path)[1] or ".jpg"
        image_path = os.path.join(art_dir, f"image{image_ext}")
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
