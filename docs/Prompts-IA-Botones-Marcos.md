# Prompts para generar botones y marcos con IA (DALL-E, Nano Banano, etc.)

> **Estrategia recomendada para marcos:** Pide **fondo blanco** (no transparente). Luego ejecuta:
> ```bash
> python scripts/white_to_transparent.py
> ```
> Ese script reemplaza el blanco por transparencia real.
>
> **Botones:** Usa `scripts/generate_ui_sprites.py` — genera botones con transparencia desde cero.

---

## Botones

### ButtonPill.png
```
UI button, pill shape, horizontal capsule, rounded corners, dark gray semi-transparent fill (#505050), subtle white glow on edges, no background, transparent PNG, flat design, 256x96 pixels, centered, no text, no icons
```

**Alternativa más corta:**
```
Pill-shaped UI button, dark gray with white glow border, transparent background, flat design, no text
```

---

### ButtonCircleGlossy.png
```
UI button, perfect circle, glossy bubble effect, spherical highlight top-left, dark gray base (#464B55), subtle white border, transparent background, 128x128 pixels, no text, no icons, flat design
```

**Alternativa:**
```
Circular glossy UI button, bubble style, dark gray with highlight, transparent background, no text
```

---

### ButtonPrimary.png
```
UI button, elongated rectangle, rounded corners, dark gray fill (#3C4150), white glow on edges, transparent background, 256x64 pixels, flat design, no text, no icons
```

---

## Marcos (9-slice)

**Flujo recomendado:** Los marcos se definen primero como **materiales Unity** (para uso en 3D con profundidad). Los sprites 2D para grid y detalle se generan desde esos materiales:

1. **Materiales** en `Assets/ArtUnbound/Materials/`: `Frame_Madera`, `Frame_Bronce`, `Frame_Plata`, `Frame_Oro`, `Frame_Ebano`
2. **Bake sprites:** Menú `Art Unbound > Bake Frame Sprites from Materials` — renderiza cada material sobre un mesh de marco y guarda PNG en `Assets/ArtUnbound/UI/Sprites/`

Así los sprites del grid/detalle coinciden visualmente con los marcos 3D en paredes.

**Alternativa legacy:** `python scripts/generate_ui_sprites.py` — genera marcos planos sin material.

Si prefieres IA con fondo blanco: pide **fondo blanco sólido** (#FFFFFF). Después: `python scripts/white_to_transparent.py`

### FrameThumbnail.png / FrameDetail.png (generados por script)
```
Flat golden picture frame, multi-layered profile: outer band, middle recessed band, inner highlight band. Clean geometric layering, no ornate carvings or scrollwork. Golden metallic finish, empty center, transparent background. Minimal so it doesn't overshadow the artwork.
```

### FrameMadera.png
```
Ornate wooden picture frame, classic carved wood style, warm brown oak (#8B5A2B), decorative border, empty center, WHITE background (#FFFFFF), 128x128 pixels, flat 2D UI asset
```

### FrameOro.png
```
Ornate gold metallic picture frame, luxury baroque style, bright gold (#DAA520), decorative scrollwork, empty center, WHITE background (#FFFFFF), 128x128 pixels, flat 2D UI asset
```

### FramePlata.png
```
Ornate silver metallic picture frame, elegant baroque style, silver (#C0C0C0), decorative border, empty center, WHITE background (#FFFFFF), 128x128 pixels, flat 2D UI asset
```

### FrameBronce.png
```
Ornate bronze metallic picture frame, classic baroque style, bronze (#CD7F32), decorative border, empty center, WHITE background (#FFFFFF), 128x128 pixels, flat 2D UI asset
```

---

## Importar en Unity: configuración obligatoria

Para que la transparencia funcione en Unity, en cada PNG de marco:

1. **Texture Type:** Sprite (2D and UI)
2. **Alpha Is Transparency:** ✓ (activado)
3. **Sprite Mode:** Single

Si no se activa *Alpha Is Transparency*, Unity mostrará el fondo blanco aunque el PNG tenga canal alpha.

---

## Si la IA no genera transparencia

1. **remove.bg** — Sube la imagen y descarga la versión sin fondo.
2. **Photoshop / GIMP** — Selección por color (fondo blanco/gris) → Borrar → Guardar como PNG.
3. **Script Python** — Ejecuta `python scripts/white_to_transparent.py` para reemplazar blanco por alpha.
