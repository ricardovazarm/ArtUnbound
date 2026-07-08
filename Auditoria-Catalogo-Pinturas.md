# Auditoría del Catálogo de Pinturas — Inconsistencias

**Fecha:** 2026-07-07
**Estado:** ✅ **COMPLETADA** — las 17 correcciones de metadata + el duplicado quedaron aplicadas y verificadas en disco (2026-07-07).
**Alcance:** las (antes 252, ahora **251**) obras (`Assets/ArtUnbound/Data/Artworks/`).
**Criterio:** la **IMAGEN asignada es la fuente de verdad**. Para cada inconsistencia se identificó qué obra/versión muestra realmente la imagen y se corrigió el **metadata** (autor / título / museo / año). Solo errores claros + dudosos. Se ignoraron variantes de nombre de museo, acentos y años dentro de ±2.

**Resumen:** 252 revisadas · **17 obras corregidas** · **1 duplicado eliminado** (→ 251). Ningún caso requirió reemplazar la imagen: todas eran pinturas legítimas y solo hubo que ajustar el metadata.

> Verificación final: cada campo (`museum` / `year` / `author` / `title`) y su línea en `description` coinciden en disco tras *Save Project* en Unity.

---

## Ya corregido antes de esta auditoría (verificado OK)

- Irises → The Met (versión del jarrón, 1890) ✔
- Café Terrace at Night → Kröller-Müller ✔
- The Bedroom → Art Institute of Chicago (versión 1889) ✔
- Still Life with Quinces → Galerie Neue Meister, Dresden ✔

---

## 🔴 Alta confianza

| Hecho | Obra | Pack | Campo(s) | Metadata CORRECTO (según la imagen) | Estado |
|---|---|---|---|---|---|
| [x] | Portrait of Dr. Gachet | VG – Mirrors of the Soul | TÍTULO | title = "Portrait of Dr. Gachet" (2º óleo, Orsay/1890 ya OK) | ✅ Aplicado |
| [x] | The Virgin of the Rocks | High Renaissance & Mannerism | MUSEO + AÑO | National Gallery, London · 1508 | ✅ Aplicado |
| [x] | Portrait of Henry VIII | 01 Base Set | MUSEO (+autor opc.) | Walker Art Gallery, Liverpool · 1537 | ✅ Museo aplicado. Autor se dejó "Hans Holbein the Younger" (aceptable; lo exacto sería "After Hans Holbein, workshop") |
| [x] | Snow at Argenteuil | Monet – Gardens of Light | MUSEO | Museum of Fine Arts, Boston | ✅ Aplicado |
| [x] | Mont Sainte-Victoire | Post-Impressionist Dreams | MUSEO | Princeton University Art Museum | ✅ Aplicado (se corrigió el nombre que había quedado cortado) |
| [x] | Bacchus and Ariadne | High Renaissance & Mannerism | MUSEO | National Gallery, London | ✅ Aplicado |
| [x] | The Beach at Sainte-Adresse | Monet – Gardens of Light | MUSEO | Art Institute of Chicago | ✅ Aplicado |
| [x] | The Kiss (Hayez) | Reason & Revolution | AÑO | 1859 | ✅ Aplicado |
| [x] | Village of Eragny (Pissarro) | The Impressionist Family | AÑO | 1885 | ✅ Aplicado |

## 🟠 Media confianza

| Hecho | Obra | Pack | Campo(s) | Metadata CORRECTO (según la imagen) | Estado |
|---|---|---|---|---|---|
| [x] | The Descent from the Cross (Rubens) | Grand Masters | MUSEO + AÑO | Siegerlandmuseum, Siegen · 1602 | ✅ Aplicado |
| [x] | Napoleon Crossing the Alps | 01 Base Set | MUSEO + AÑO | Palace of Versailles · 1802 | ✅ Aplicado |
| [x] | The Last Supper (Copy) | High Renaissance & Mannerism | MUSEO | Royal Academy of Arts, London | ✅ Aplicado |
| [x] | Susanna and the Elders | High Renaissance & Mannerism | MUSEO | Museo del Prado | ✅ Aplicado |
| [x] | Judith Slaying Holofernes | Caravaggio & The Baroque Drama | AÑO | 1620 | ✅ Aplicado |

## 🟡 Baja confianza / atribución o datación disputada

| Hecho | Obra | Pack | Campo(s) | Metadata CORRECTO (según la imagen) | Estado |
|---|---|---|---|---|---|
| [x] | The Annunciation (Copy) | Birth of the Renaissance | AÑO | 1425 (~1425-26) | ✅ Aplicado |
| [x] | Still Life with Ham | The Art of Stillness | AUTOR | Gerret Willemsz Heda | ✅ Aplicado |
| [x] | Poplars (Three Pink Autumn Trees) | Monet – Gardens of Light | MUSEO | *Sin cambio* — se dejó The Met (versión no confirmable de forma independiente) | ✅ Revisado (sin cambio intencional) |

## ♻️ Duplicado

| Hecho | Obra | Pack | Situación | Estado |
|---|---|---|---|---|
| [x] | View of Houses in Delft **+** The Little Street | Vermeer & The Dutch Interior | Eran la misma obra (el "Straatje" del Rijksmuseum, 1658; nombre canónico = "The Little Street") | ✅ Se quitó "View of Houses in Delft" de ArtworkCatalog (se conserva "The Little Street"). Catálogo → 251. Opcional: borrar el .asset huérfano y/o agregar una obra nueva para volver a 252 |

---

## Notas de método

- Cada obra fue verificada **viendo la imagen real** (`Assets/ArtUnbound/Artworks/SourceImages/{título}.jpg`), no por inferencia de año/aspecto.
- Las 6 obras cuya imagen no correspondía a su metadata se identificaron individualmente para dar el museo/año correctos: Dr. Gachet (2º óleo, Orsay), The Virgin of the Rocks (NG Londres), Henry VIII (Walker Art Gallery), Napoleon (Versalles 1802), Snow at Argenteuil (MFA Boston), Mont Sainte-Victoire (Princeton/Pearlman), y The Descent from the Cross (Siegen 1602).
- Pendiente opcional a futuro: (1) autor de Henry VIII a "After Hans Holbein (workshop)"; (2) agregar una obra nueva para reponer el duplicado y volver a 252; (3) borrar el `.asset` huérfano de "View of Houses in Delft".
