# Guía: Usar la fuente Inter (recomendada por Meta) en Art Unbound

> Meta recomienda **Inter** (Meta Horizon OS UI Set) para legibilidad en MR.  
> Ref: [Meta-Quest-MR-UI-Interaction-Guidelines.md](Meta-Quest-MR-UI-Interaction-Guidelines.md) § 8 Visual Design.

---

## Estado actual (actualizado)

| Fuente | Ubicación | Uso actual |
|--------|-----------|------------|
| **Inter** | `Assets/MRTemplateAssets/Fonts/Inter/Inter-Regular_SDF.asset` | **Todos los textos de Art Unbound** (Main.unity, RecordItem, TMP Settings default) |
| **Cormorant** | `Assets/ArtUnbound/UI/Fonts/Cormorant-Regular SDF.asset` | Ya no se usa; Inter reemplazó todos los textos |
| **LiberationSans** | `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset` | Solo en prefabs de MRTemplate/XR Toolkit que no modificamos |

---

## Qué necesitas para que todos los textos usen Inter

### 1. La fuente Inter ya está en el proyecto

No hace falta descargar nada. Inter viene con **MRTemplateAssets** en:
- `Assets/MRTemplateAssets/Fonts/Inter/Inter-Regular.ttf`
- `Assets/MRTemplateAssets/Fonts/Inter/Inter-Regular_SDF.asset` ← **este es el que debes asignar**

### 2. Cambiar los componentes TextMeshPro

Cada `TextMeshPro - Text (UI)` tiene un campo **Font Asset**. Hay que cambiarlo de Cormorant/LiberationSans a **Inter-Regular_SDF**.

**Dónde cambiar:**

| Archivo / Ubicación | Elementos a actualizar |
|---------------------|-------------------------|
| `Assets/ArtUnbound/Scenes/Main.unity` | ~25 componentes de texto en UnifiedMainMenuPanel, LeftZone, RightZone, etc. |
| `Assets/ArtUnbound/Prefabs/RecordItem.prefab` | 2 textos |
| Prefabs que referencien Cormorant | Cualquier prefab de ArtUnbound que use texto |

**Pasos en Unity:**

1. Abre la escena `Main.unity`.
2. En la jerarquía, selecciona el objeto con el texto (ej. un `TextMeshPro - Text`).
3. En el Inspector, en el componente **TextMeshPro - Text (UI)**:
   - **Font Asset** → clic en el círculo/buscar
   - Busca `Inter-Regular_SDF` (en `MRTemplateAssets/Fonts/Inter/`)
   - Selecciónalo.
4. Repite para todos los textos de la escena.
5. Guarda la escena (Ctrl+S).
6. Haz lo mismo en `RecordItem.prefab` y cualquier otro prefab con texto.

### 3. (Opcional) Cambiar el default de TextMesh Pro

Para que los **nuevos** textos usen Inter por defecto:

1. **Window → TextMeshPro → Font Asset** (o busca `TMP Settings` en el Project).
2. Abre `Assets/TextMesh Pro/Resources/TMP Settings.asset`.
3. En **Default Font Asset**, asigna `Inter-Regular_SDF`.
4. Guarda.

Así, cualquier nuevo `TextMeshPro - Text` que crees usará Inter automáticamente.

### 4. Verificar tamaños de fuente (guía Meta)

Meta recomienda:
- **Mínimo 14 px** para legibilidad.
- **18 px** para lectura cómoda.

Revisa que ningún texto esté por debajo de 14 px.

---

## Resumen rápido

1. **Inter ya está** en `MRTemplateAssets/Fonts/Inter/`.
2. **Asignar** `Inter-Regular_SDF` como Font Asset en cada TextMeshPro de ArtUnbound.
3. **Opcional:** Cambiar Default Font Asset en TMP Settings a Inter.
4. **Revisar** que los tamaños de fuente sean ≥ 14 px.
