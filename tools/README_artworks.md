# Descarga de Obras para Art Unbound

## Uso

```bash
python tools/download_artworks.py
```

El script:
1. **Descarga imágenes** a `Assets/ArtUnbound/Artworks/` con el nombre de la obra
2. **Genera** `Assets/ArtUnbound/Data/artworks_catalog.json` con metadata para ArtworkDefinition

## Museos soportados

| Museo | Método | Notas |
|-------|--------|-------|
| **Art Institute of Chicago** | API | Puede dar 403 en algunas redes; prueba con VPN o en otro equipo |
| **The Met (NYC)** | API | Funciona; algunas obras no tienen imagen en API (ej. Bridge over a Pond → usa Wikimedia) |
| **Rijksmuseum** | API | Requiere API key: `set RIJKS_API_KEY=tu_clave` |
| **Mauritshuis, NGA, Van Gogh, NG UK, Yale** | Manual | Añade `image_url` en el catálogo del script para descargar |

## Rijksmuseum API Key

1. Regístrate en [Rijksstudio](https://www.rijksmuseum.nl/en/rijksstudio)
2. Obtén tu API key
3. Ejecuta: `$env:RIJKS_API_KEY="tu_clave"; python tools/download_artworks.py` (PowerShell)

## Siguiente paso en Unity

1. Importa las imágenes: **Texture Type = Sprite**, **Read/Write = enabled**, **Max Size = 4096**
2. Crea ArtworkDefinition desde el JSON (o manualmente con los datos)
3. Añádelos a `ArtworkCatalog.asset`
