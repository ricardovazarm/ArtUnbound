---
name: gen-bundle-hero
description: Genera una imagen hero 3x2 para bundles de Art Unbound combinando las cover iconicas (primera obra) de cada pack en una wave. Usalo cuando quieras crear o regenerar la imagen hero de un bundle automaticamente sin necesitar un disenador. Solo soporta Wave01 y Wave02 (Wave03 tiene menos de 6 packs).
argument-hint: <Wave01 | Wave02 | all>
allowed-tools: [Bash, Read]
---

# Generador de Bundle Hero Images — Art Unbound

El usuario invoco este skill con: $ARGUMENTS

## Que hace

Genera una imagen JPG hero para el bundle de una wave, combinando las 6 obras iconicas (la primera obra del array `artworks:` de cada PackDefinition) en un grid 3 columnas x 2 filas. Cada tile es **center-cropped a cuadrado** (cover mode, sin huecos) y resizeado a 500x500. Output total: **1500x1000**.

Esto permite usar bundles sin necesitar un disenador: la imagen se auto-genera a partir de la ordenacion ya establecida (la primera obra de cada pack es la cover iconica).

**Orden de tiles en el grid**: el skill lee el orden de packs desde el `BundleDefinition.asset` correspondiente (`Assets/ArtUnbound/Data/Bundles/<wave> - *.asset`). Esto respeta la decision curatorial del bundle. Si no existe BundleDefinition para esa wave (caso Wave02 antes de Mes 6), cae a orden alfabetico con warning.

## Convenciones del proyecto

- **Pack assets**: `Assets/ArtUnbound/Data/Packs/<wave>/<pack>.asset` — cada uno tiene un array `artworks:` cuyo PRIMER elemento es la obra cover iconica.
- **ArtworkDefinition assets**: `Assets/ArtUnbound/Data/Artworks/<pack folder>/<title>.asset` — el filename stem coincide con el nombre del archivo JPG fuente.
- **Source images**: `Assets/ArtUnbound/Artworks/SourceImages/<title>.jpg` — flat directory, sin subcarpetas.
- **Output**: `Assets/ArtUnbound/Data/Bundles/Hero Images/<wave>.jpg`

## Argumentos validos

- `Wave01` — genera hero para Wave 1 (Founder's Collection)
- `Wave02` — genera hero para Wave 2
- `all` — genera para ambas

**Nota**: Wave03 actualmente tiene 5 packs, no se puede formar grid 3x2. Si se agrega un sexto pack a Wave03, este skill se podria extender para incluirlo.

## Instrucciones

### Paso 1 — Validar argumento

Si `$ARGUMENTS` no es uno de `Wave01`, `Wave02`, `all`, detente y reporta el error al usuario.

### Paso 2 — Verificar dependencias

```bash
python -c "from PIL import Image; print('PIL OK')"
```

Si falta: `pip install Pillow`

### Paso 3 — Ejecutar el script

Reemplaza `$ARGUMENTS` con el argumento real (Wave01, Wave02, o all):

```bash
python << 'PYEOF'
import os, re, sys
sys.stdout.reconfigure(encoding='utf-8')
from pathlib import Path
from PIL import Image

ROOT = Path(os.popen('git rev-parse --show-toplevel').read().strip())
WAVE_ARG = "$ARGUMENTS".strip()

TILE_SIZE = 500
COLS = 3
ROWS = 2
JPEG_QUALITY = 90

packs_root = ROOT / 'Assets/ArtUnbound/Data/Packs'
artworks_root = ROOT / 'Assets/ArtUnbound/Data/Artworks'
bundles_root = ROOT / 'Assets/ArtUnbound/Data/Bundles'
source_dir = ROOT / 'Assets/ArtUnbound/Artworks/SourceImages'
output_dir = ROOT / 'Assets/ArtUnbound/Data/Bundles/Hero Images'
output_dir.mkdir(parents=True, exist_ok=True)

# Whitelist of supported waves (Wave03 has fewer than 6 packs)
SUPPORTED = ['Wave01', 'Wave02']
if WAVE_ARG.lower() == 'all':
    waves = SUPPORTED
elif WAVE_ARG in SUPPORTED:
    waves = [WAVE_ARG]
else:
    print(f'[ERROR] Argumento invalido: "{WAVE_ARG}". Usa Wave01, Wave02, o all.')
    sys.exit(1)

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

# Build pack GUID -> pack file path index (for PackDefinitions across all waves)
pack_guid_to_file = {}
for pack_meta in packs_root.rglob('*.asset.meta'):
    content = pack_meta.read_text(encoding='utf-8')
    m = re.search(r'guid:\s*([a-f0-9]+)', content)
    if m:
        # Strip .meta to get the .asset path
        asset_path = pack_meta.with_suffix('')  # drops .meta
        if asset_path.exists():
            pack_guid_to_file[m.group(1)] = asset_path

print(f'Indexed {len(pack_guid_to_file)} pack GUIDs')

for wave in waves:
    wave_dir = packs_root / wave
    if not wave_dir.exists():
        print(f'[SKIP] Wave dir not found: {wave_dir}')
        continue

    # Try to read pack order from BundleDefinition for this wave
    bundle_files = list(bundles_root.glob(f'{wave} - *.asset'))
    pack_assets = []
    if bundle_files:
        bundle_text = bundle_files[0].read_text(encoding='utf-8')
        if 'packs:' in bundle_text:
            packs_section = bundle_text.split('packs:', 1)[1]
            # Extract guids in order they appear in the bundle
            bundle_pack_guids = re.findall(r'guid:\s*([a-f0-9]+)', packs_section)
            for guid in bundle_pack_guids:
                pack_file = pack_guid_to_file.get(guid)
                if pack_file and pack_file.parent.name == wave:
                    pack_assets.append(pack_file)
        print(f'[INFO] {wave}: using bundle order from {bundle_files[0].name} ({len(pack_assets)} packs)')
    else:
        pack_assets = sorted(wave_dir.glob('*.asset'))
        print(f'[WARN] {wave}: no BundleDefinition found, falling back to alphabetical order')

    if len(pack_assets) < 6:
        print(f'[SKIP] {wave} has only {len(pack_assets)} packs (need 6)')
        continue

    pack_assets = pack_assets[:6]

    images = []
    for pack_asset in pack_assets:
        text = pack_asset.read_text(encoding='utf-8')
        if 'artworks:' not in text:
            print(f'[ERR] No artworks: section in {pack_asset.name}')
            continue
        artworks_section = text.split('artworks:', 1)[1]
        m = re.search(r'guid:\s*([a-f0-9]+)', artworks_section)
        if not m:
            print(f'[ERR] No first artwork guid in {pack_asset.name}')
            continue
        guid = m.group(1)

        artwork_name = guid_to_name.get(guid)
        if not artwork_name:
            print(f'[ERR] No artwork name for guid {guid} (pack: {pack_asset.name})')
            continue

        img_path = None
        for ext in ('.jpg', '.jpeg', '.png'):
            candidate = source_dir / f'{artwork_name}{ext}'
            if candidate.exists():
                img_path = candidate
                break
        if img_path is None:
            print(f'[ERR] No source image for {artwork_name} (pack: {pack_asset.name})')
            continue

        images.append((pack_asset.stem, artwork_name, img_path))

    if len(images) < 6:
        print(f'[ERR] {wave}: only {len(images)} valid covers, skipping')
        continue

    # Compose 3x2 grid with cover-mode (center-crop to square, no gaps)
    canvas = Image.new('RGB', (TILE_SIZE * COLS, TILE_SIZE * ROWS), (20, 20, 20))
    for idx, (pack_name, artwork_name, img_path) in enumerate(images):
        col = idx % COLS
        row = idx // COLS
        with Image.open(img_path) as img:
            img = img.convert('RGB')
            w, h = img.size
            # Center-crop to square (cover mode, fills tile completely)
            side = min(w, h)
            left = (w - side) // 2
            top = (h - side) // 2
            cropped = img.crop((left, top, left + side, top + side))
            tile = cropped.resize((TILE_SIZE, TILE_SIZE), Image.LANCZOS)
            canvas.paste(tile, (col * TILE_SIZE, row * TILE_SIZE))

    output_path = output_dir / f'{wave}.jpg'
    canvas.save(output_path, 'JPEG', quality=JPEG_QUALITY, optimize=True)

    rel = output_path.relative_to(ROOT)
    print(f'\n[OK] {wave} -> {rel}')
    for pack_name, artwork_name, _ in images:
        print(f'      {pack_name}  (cover: {artwork_name})')

print('\nDone.')
PYEOF
```

### Paso 4 — Reportar al usuario

Informa:
- Cuantas waves se procesaron y la ruta de cada output JPG
- Que packs y que cover se uso para cada uno
- Si hubo errores (covers no encontrados, source images faltantes), listalos

**Nota Unity:** El JPG se importara automaticamente cuando el usuario refresque Unity. Para usarlo como `bundleImage` del `BundleDefinition`, el usuario debe:
1. En Unity, seleccionar el JPG y cambiar Texture Type a "Sprite (2D and UI)"
2. Arrastrar el sprite al campo `bundleImage` del BundleDefinition correspondiente

## Limitaciones

- Solo soporta Wave01 y Wave02. Wave03 tiene 5 packs, no se puede formar grid 3x2 hasta que se agregue un sexto pack.
- Center-crop a cuadrado puede recortar contenido lateral en obras muy panoramicas o muy alargadas. Para covers premium o casos especiales, usa una imagen custom en `bundleImage` del BundleDefinition (el campo opcional anula el auto-generado).
- Si la cover de un pack cambia (reordenas el `artworks:` array), regenera el hero corriendo el skill de nuevo.
