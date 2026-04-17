---
name: gen-thumbnails
description: Genera thumbnails a 1/4 del tamaño original para todas las imágenes de una colección de Art Unbound. Úsalo cuando el usuario quiera generar thumbnails para una colección específica o para todas las colecciones.
argument-hint: <nombre_coleccion | "all">
allowed-tools: [Bash, Read]
---

# Generador de Thumbnails — Art Unbound

El usuario invocó este skill con: $ARGUMENTS

## Instrucciones

### Paso 1 — Resolver el argumento

El argumento puede ser:
- Un nombre de colección exacto, e.g. `Monet Gardens Through Time`
- La palabra `all` para procesar todas las colecciones

Rutas base del proyecto (usa siempre rutas absolutas):
- **Source**: `Assets/ArtUnbound/Artworks/SourceImages/<coleccion>/`
- **Output**: `Assets/ArtUnbound/Artworks/Thumbnails/<coleccion>/`

Para obtener la ruta absoluta de la raíz del proyecto:
```bash
cd "$(git rev-parse --show-toplevel)" && pwd
```

### Paso 2 — Verificar que PIL está disponible

```bash
python -c "from PIL import Image; print('PIL OK')"
```

Si no está disponible, instalar:
```bash
pip install Pillow
```

### Paso 3 — Listar imágenes a procesar

Si el argumento es `all`, lista todas las subcarpetas de SourceImages:
```bash
ls "Assets/ArtUnbound/Artworks/SourceImages/"
```

Si es una colección específica, verifica que la carpeta exista:
```bash
ls "Assets/ArtUnbound/Artworks/SourceImages/$ARGUMENTS/"
```

### Paso 4 — Generar los thumbnails con Python

Ejecuta este script para procesar la colección (reemplaza `<COLLECTION>` con el nombre real y `<ROOT>` con la ruta absoluta del proyecto):

```bash
python - << 'EOF'
import os
import sys
sys.stdout.reconfigure(encoding='utf-8')
from PIL import Image

ROOT = "<ROOT>"
COLLECTION = "<COLLECTION>"

src_base = os.path.join(ROOT, "Assets/ArtUnbound/Artworks/SourceImages")
out_base = os.path.join(ROOT, "Assets/ArtUnbound/Artworks/Thumbnails")

if COLLECTION == "all":
    collections = [d for d in os.listdir(src_base)
                   if os.path.isdir(os.path.join(src_base, d)) and not d.endswith(".meta")]
else:
    collections = [COLLECTION]

total_ok = 0
total_skip = 0
errors = []

for col in collections:
    src_dir = os.path.join(src_base, col)
    out_dir = os.path.join(out_base, col)

    if not os.path.isdir(src_dir):
        print(f"[SKIP] Colección no encontrada: {col}")
        continue

    os.makedirs(out_dir, exist_ok=True)

    images = [f for f in os.listdir(src_dir)
              if f.lower().endswith((".jpg", ".jpeg", ".png")) and not f.endswith(".meta")]

    for fname in images:
        src_path = os.path.join(src_dir, fname)
        out_path = os.path.join(out_dir, fname)

        # Skip if thumbnail already exists and is newer than source
        if os.path.exists(out_path) and os.path.getmtime(out_path) >= os.path.getmtime(src_path):
            total_skip += 1
            continue

        try:
            with Image.open(src_path) as img:
                w, h = img.size
                new_w = max(1, w // 4)
                new_h = max(1, h // 4)
                thumb = img.resize((new_w, new_h), Image.LANCZOS)
                # Save as JPEG at quality 85 regardless of original format
                out_jpg = os.path.splitext(out_path)[0] + ".jpg"
                thumb = thumb.convert("RGB")
                thumb.save(out_jpg, "JPEG", quality=85, optimize=True)
                print(f"[OK] {col}/{fname}  {w}x{h} -> {new_w}x{new_h}")
                total_ok += 1
        except Exception as e:
            errors.append(f"{col}/{fname}: {e}")
            print(f"[ERR] {col}/{fname}: {e}")

print()
print(f"Generados: {total_ok}  |  Omitidos (ya existían): {total_skip}  |  Errores: {len(errors)}")
if errors:
    print("\nErrores:")
    for e in errors:
        print(f"  {e}")
EOF
```

### Paso 5 — Presentar resumen al usuario

Informa:
- Cuántos thumbnails se generaron y en qué colección(es)
- Tamaño original → tamaño thumbnail para cada imagen
- Si hubo errores, detállalos
- La ruta de salida donde quedaron los thumbnails

**Nota**: Los archivos `.meta` los generará Unity automáticamente al detectar los nuevos archivos en el Editor. No es necesario crearlos manualmente.
