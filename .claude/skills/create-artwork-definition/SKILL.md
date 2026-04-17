---
name: create-artwork-definition
description: Genera los archivos .asset de ArtworkDefinition para todos los artworks de una colección de Art Unbound. Úsalo cuando el usuario quiera crear o regenerar los ArtworkDefinition assets para una colección específica o para todas.
argument-hint: <nombre_coleccion | "all">
allowed-tools: [Bash, Read, Write]
---

# Generador de ArtworkDefinition Assets — Art Unbound

El usuario invocó este skill con: $ARGUMENTS

## Instrucciones

Ejecuta el siguiente script Python que genera todos los `.asset` y sus `.meta` para la colección indicada.

```bash
python - << 'PYEOF'
import os, sys, json, math, uuid, re
sys.stdout.reconfigure(encoding='utf-8')
from PIL import Image

ROOT       = "C:/Users/rvazq/Documents/Mac/AjoloteStudios/ArtUnbound"
COLLECTION = "$ARGUMENTS"

SCRIPT_GUID   = "32110b9219b62be46b10a97c773a56f7"
VAN_GOGH_MUSEUM = "Van Gogh Museum"
VAN_GOGH_CREDIT = "Van Gogh Museum, Amsterdam (Vincent van Gogh Foundation)"
BASE_SET_FOLDER = "01 Base Set"

TARGETS = [64, 121, 196, 289]

def calc_pieces(target, w, h):
    ratio = w / h
    rows = max(2, round(math.sqrt(target / ratio)))
    cols = max(2, round(rows * ratio))
    return rows * cols

def calc_aspect(w, h):
    return round(w / h, 4)

def get_guid_from_meta(meta_path):
    if not os.path.exists(meta_path):
        return None
    with open(meta_path, "r", encoding="utf-8") as f:
        for line in f:
            m = re.match(r"^guid:\s*(\w+)", line.strip())
            if m:
                return m.group(1)
    return None

def make_asset_guid():
    return uuid.uuid4().hex

def build_description(museum, art_movement, year):
    # Use double-quoted YAML string on a single line with \n escape sequences.
    # Apostrophes are safe in double-quoted YAML; \n\n gives newlines when parsed.
    # We build the separator from bytes to avoid bash heredoc backslash processing.
    sep = bytes([0x5C, 0x6E]).decode('ascii')  # literal \n (single newline in YAML)
    lines = [
        f"Museum: {museum}",
        f"Art Movement: {art_movement}",
        f"Year: {year}"
    ]
    if museum == VAN_GOGH_MUSEUM:
        lines.append(f"Credits: {VAN_GOGH_CREDIT}")
    escaped = [l.replace("\\", "\\\\").replace('"', '\\"') for l in lines]
    return sep.join(escaped)

def patch_meta_as_sprite(meta_path):
    """Update a texture .meta to import as Sprite (textureType 8, spriteMode 1, isReadable 1)."""
    if not os.path.exists(meta_path):
        return
    with open(meta_path, "r", encoding="utf-8") as f:
        content = f.read()
    # Only patch if it's a TextureImporter and not already a Sprite
    if "TextureImporter" not in content:
        return
    content = re.sub(r"(spriteMode:)\s*\d+", r"\g<1> 1", content)
    content = re.sub(r"(textureType:)\s*\d+", r"\g<1> 8", content)
    content = re.sub(r"(isReadable:)\s*\d+", r"\g<1> 1", content)
    with open(meta_path, "w", encoding="utf-8", newline="\n") as f:
        f.write(content)

def write_asset(path, data):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(data)

def write_meta(path, asset_guid):
    content = f"""fileFormatVersion: 2
guid: {asset_guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(content)

def make_tex_ref(guid):
    if guid:
        return f"{{fileID: 21300000, guid: {guid}, type: 3}}"
    return "{fileID: 0}"

def process_collection(col, artworks):
    src_dir   = os.path.join(ROOT, "Assets/ArtUnbound/Artworks/SourceImages", col)
    thumb_dir = os.path.join(ROOT, "Assets/ArtUnbound/Artworks/Thumbnails", col)
    out_dir   = os.path.join(ROOT, "Assets/ArtUnbound/Data/Artworks", col)
    is_base   = (col == BASE_SET_FOLDER)

    ok, skip, errors = 0, 0, []

    for aw in artworks:
        title = aw["title"]

        # Find source image
        src_img = os.path.join(src_dir, title + ".jpg")
        if not os.path.exists(src_img):
            # Try .png
            src_img = os.path.join(src_dir, title + ".png")
        if not os.path.exists(src_img):
            errors.append(f"Imagen no encontrada: {title}")
            continue

        # Get source GUID
        src_guid = get_guid_from_meta(src_img + ".meta")
        if not src_guid:
            errors.append(f"Sin .meta para imagen: {title}")
            continue

        # Patch source image meta to Sprite
        patch_meta_as_sprite(src_img + ".meta")

        # Get thumbnail GUID (fallback to source), patch its meta too
        thumb_img  = os.path.join(thumb_dir, title + ".jpg")
        patch_meta_as_sprite(thumb_img + ".meta")
        thumb_guid = get_guid_from_meta(thumb_img + ".meta") or src_guid

        # Image dimensions
        try:
            with Image.open(src_img) as img:
                w, h = img.size
        except Exception as e:
            errors.append(f"No se pudo leer imagen {title}: {e}")
            continue

        # Piece counts
        pieces = [calc_pieces(t, w, h) for t in TARGETS]

        # Aspect ratio and base sizes
        ratio = calc_aspect(w, h)
        if ratio >= 1.0:  # landscape
            bsp = "{x: 0.5, y: 0.7}"
            bsl = "{x: 0.7, y: 0.5}"
        else:             # portrait
            bsp = "{x: 0.5, y: 0.7}"
            bsl = "{x: 0.7, y: 0.5}"

        # Description
        desc = build_description(aw["museum"], aw["artMovement"], aw["year"])

        asset_path = os.path.join(out_dir, title + ".asset")
        meta_path  = asset_path + ".meta"

        # Reuse existing GUID if asset already exists
        existing_guid = get_guid_from_meta(meta_path) or make_asset_guid()

        asset_content = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {SCRIPT_GUID}, type: 3}}
  m_Name: {title}
  m_EditorClassIdentifier: Assembly-CSharp::ArtUnbound.Data.ArtworkDefinition
  artworkId: {title}
  title: {title}
  author: {aw["author"]}
  year: {aw["year"]}
  description: "{desc}"
  museum: {aw["museum"]}
  artMovement: {aw["artMovement"]}
  aspectRatio: {ratio}
  baseSizePortrait: {bsp}
  baseSizeLandscape: {bsl}
  thumbnail: {make_tex_ref(thumb_guid)}
  fullImage: {make_tex_ref(src_guid)}
  previewTexture: {{fileID: 0}}
  puzzleTexture: {{fileID: 0}}
  isBaseContent: {1 if is_base else 0}
  requiresUnlock: {0 if is_base else 1}
  unlockWeek: 0
  pieceCountEasy: {pieces[0]}
  pieceCountNormal: {pieces[1]}
  pieceCountHard: {pieces[2]}
  pieceCountExpert: {pieces[3]}
  complexity: 1
  colorVariety: 3
  detailLevel: 3
"""
        write_asset(asset_path, asset_content)
        write_meta(meta_path, existing_guid)
        print(f"  [OK] {title}  {w}x{h}  piezas={pieces[0]}/{pieces[1]}/{pieces[2]}/{pieces[3]}")
        ok += 1

    return ok, skip, errors

# --- Main ---
data_path = os.path.join(ROOT, "scripts/artworks_data.json")
with open(data_path, "r", encoding="utf-8") as f:
    all_artworks = json.load(f)

if COLLECTION == "all":
    collections = sorted(set(a["collection"] for a in all_artworks))
else:
    collections = [COLLECTION]

total_ok = 0
total_errors = []

for col in collections:
    col_artworks = [a for a in all_artworks if a["collection"] == col]
    if not col_artworks:
        print(f"[SKIP] Coleccion no encontrada en JSON: {col}")
        continue
    print(f"\n[{col}]")
    ok, _, errs = process_collection(col, col_artworks)
    total_ok += ok
    total_errors.extend(errs)

print(f"\nTOTAL assets generados: {total_ok}  |  Errores: {len(total_errors)}")
if total_errors:
    print("\nErrores:")
    for e in total_errors:
        print(f"  {e}")
PYEOF
```

Reporta al usuario:
- Cuántos assets se generaron por colección
- Los piece counts de cada obra
- Cualquier error (imagen no encontrada, .meta faltante, etc.)
- La ruta de salida: `Assets/ArtUnbound/Data/Artworks/<coleccion>/`
