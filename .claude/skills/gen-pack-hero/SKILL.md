---
name: gen-pack-hero
description: Genera una imagen hero 3x2 para packs de Art Unbound combinando las 6 primeras obras (curadas) del pack. Usalo cuando quieras crear o regenerar la imagen hero de un pack automaticamente sin necesitar un disenador. Soporta los 17 packs de las Wave01/02/03.
argument-hint: <nombre del pack | all>
allowed-tools: [Bash, Read]
---

# Generador de Pack Hero Images — Art Unbound

El usuario invoco este skill con: $ARGUMENTS

## Que hace

Genera una imagen JPG hero para un pack (o todos), combinando las **primeras 6 obras** del array `artworks:` del PackDefinition (ya estan ordenadas curatorialmente, con la mas iconica en posicion 0). Cada tile es **center-cropped a cuadrado** (cover mode, sin huecos) y resizeado a 500x500. Output total: **1500x1000**.

Esto permite usar packs en el StoreView sin necesitar un disenador: la imagen se auto-genera a partir de la ordenacion ya establecida.

## Convenciones del proyecto

- **Pack assets**: `Assets/ArtUnbound/Data/Packs/<wave>/<pack>.asset` — array `artworks:` con orden curatorial.
- **ArtworkDefinition assets**: `Assets/ArtUnbound/Data/Artworks/<pack folder>/<title>.asset` — el filename stem coincide con el JPG fuente.
- **Source images**: `Assets/ArtUnbound/Artworks/SourceImages/<title>.jpg` — flat directory.
- **Output**: `Assets/ArtUnbound/Data/Packs/Hero Images/<wave>/<pack file stem>.jpg`

## Argumentos validos

- `<nombre del pack>` — ej. `Van Gogh - Light & Color in Provence` (filename stem, sin extension). Genera solo ese pack.
- `all` — genera para los 17 packs de las 3 waves.

## Instrucciones

### Paso 1 — Validar argumento

Si `$ARGUMENTS` esta vacio, detente y reporta el error. Si es un nombre de pack, debe existir como `.asset` bajo `Assets/ArtUnbound/Data/Packs/Wave0X/`.

### Paso 2 — Verificar dependencias

```bash
python -c "from PIL import Image; print('PIL OK')"
```

Si falta: `pip install Pillow`

### Paso 3 — Ejecutar el script

Reemplaza `$ARGUMENTS` con el argumento real (nombre del pack o `all`):

```bash
python << 'PYEOF'
import os, re, sys
sys.stdout.reconfigure(encoding='utf-8')
from pathlib import Path
from PIL import Image

ROOT = Path(os.popen('git rev-parse --show-toplevel').read().strip())
PACK_ARG = "$ARGUMENTS".strip()

TILE_SIZE = 500
COLS = 3
ROWS = 2
TILES_NEEDED = COLS * ROWS  # 6
JPEG_QUALITY = 90

packs_root    = ROOT / 'Assets/ArtUnbound/Data/Packs'
artworks_root = ROOT / 'Assets/ArtUnbound/Data/Artworks'
source_dir    = ROOT / 'Assets/ArtUnbound/Artworks/SourceImages'
output_root   = ROOT / 'Assets/ArtUnbound/Data/Packs/Hero Images'

# Build GUID -> artwork filename stem index (for ArtworkDefinitions)
guid_to_name = {}
for meta in artworks_root.rglob('*.asset.meta'):
    content = meta.read_text(encoding='utf-8')
    m = re.search(r'guid:\s*([a-f0-9]+)', content)
    if m:
        name = meta.stem
        if name.endswith('.asset'):
            name = name[:-6]
        guid_to_name[m.group(1)] = name
print(f'Indexed {len(guid_to_name)} artwork GUIDs')

# Resolve which packs to process
all_packs = sorted(packs_root.rglob('Wave0*/*.asset'))
if PACK_ARG.lower() == 'all':
    targets = all_packs
elif PACK_ARG:
    targets = [p for p in all_packs if p.stem == PACK_ARG]
    if not targets:
        print(f'[ERROR] No pack found with stem: "{PACK_ARG}"')
        print('Available packs:')
        for p in all_packs:
            print(f'  - {p.parent.name}/{p.stem}')
        sys.exit(1)
else:
    print('[ERROR] Argument required. Use a pack name or "all".')
    sys.exit(1)

print(f'Processing {len(targets)} pack(s)...')

generated = 0
errors = []

for pack_asset in targets:
    wave = pack_asset.parent.name  # Wave01/Wave02/Wave03
    pack_stem = pack_asset.stem

    text = pack_asset.read_text(encoding='utf-8')
    if 'artworks:' not in text:
        errors.append(f'{wave}/{pack_stem}: no artworks: section')
        continue
    artworks_section = text.split('artworks:', 1)[1]
    artwork_guids = re.findall(r'guid:\s*([a-f0-9]+)', artworks_section)

    if len(artwork_guids) < TILES_NEEDED:
        errors.append(f'{wave}/{pack_stem}: only {len(artwork_guids)} artworks (need {TILES_NEEDED})')
        continue

    # Take first 6 (curated order, iconic first)
    selected_guids = artwork_guids[:TILES_NEEDED]

    images = []
    for guid in selected_guids:
        artwork_name = guid_to_name.get(guid)
        if not artwork_name:
            errors.append(f'{wave}/{pack_stem}: no name for guid {guid}')
            continue

        img_path = None
        for ext in ('.jpg', '.jpeg', '.png'):
            candidate = source_dir / f'{artwork_name}{ext}'
            if candidate.exists():
                img_path = candidate
                break
        if img_path is None:
            errors.append(f'{wave}/{pack_stem}: no source for {artwork_name}')
            continue

        images.append((artwork_name, img_path))

    if len(images) < TILES_NEEDED:
        errors.append(f'{wave}/{pack_stem}: only {len(images)} valid tiles (need {TILES_NEEDED})')
        continue

    # Compose 3x2 grid with cover-mode (center-crop to square, no gaps)
    canvas = Image.new('RGB', (TILE_SIZE * COLS, TILE_SIZE * ROWS), (20, 20, 20))
    for idx, (artwork_name, img_path) in enumerate(images):
        col = idx % COLS
        row = idx // COLS
        with Image.open(img_path) as img:
            img = img.convert('RGB')
            w, h = img.size
            side = min(w, h)
            left = (w - side) // 2
            top  = (h - side) // 2
            cropped = img.crop((left, top, left + side, top + side))
            tile = cropped.resize((TILE_SIZE, TILE_SIZE), Image.LANCZOS)
            canvas.paste(tile, (col * TILE_SIZE, row * TILE_SIZE))

    out_dir = output_root / wave
    out_dir.mkdir(parents=True, exist_ok=True)
    output_path = out_dir / f'{pack_stem}.jpg'
    canvas.save(output_path, 'JPEG', quality=JPEG_QUALITY, optimize=True)

    rel = output_path.relative_to(ROOT)
    generated += 1
    print(f'[OK] {wave}/{pack_stem} -> {rel}')

print(f'\nGenerated {generated} hero(s).')
if errors:
    print(f'\n{len(errors)} error(s):')
    for e in errors:
        print(f'  - {e}')
PYEOF
```

### Paso 4 — Reportar al usuario

Informa:
- Cuantos packs se procesaron y la ruta de cada output JPG
- Si hubo errores (covers no encontrados, source images faltantes), listalos

**Nota Unity:** El JPG se importara automaticamente cuando el usuario refresque Unity. Para usarlo como `packImage` del `ArtworkPackDefinition`, el usuario debe:
1. En Unity, seleccionar el JPG y cambiar Texture Type a "Sprite (2D and UI)"
2. Arrastrar el sprite al campo `packImage` del PackDefinition correspondiente

## Limitaciones

- Center-crop a cuadrado puede recortar contenido lateral en obras muy panoramicas o muy alargadas. Para covers premium, usa una imagen custom en `packImage` del PackDefinition (el campo opcional anula el auto-generado).
- Si reordenas el `artworks:` array del PackDefinition, regenera el hero corriendo el skill de nuevo.
- Cada pack debe tener al menos 6 artworks (los 17 packs actuales tienen 12 cada uno).
