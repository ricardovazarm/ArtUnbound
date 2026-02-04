# Documento de Arquitectura: Art Unbound
- Version: 0.1 (Borrador inicial)
- Fecha: 8 de junio de 2025
- Autor(es): [Tu Nombre] y Gemini AI
- Plataforma objetivo: Meta Quest 3 y Meta Quest Pro (Exclusivo Realidad Mixta a Color)

## 1. Proposito y Alcance
Este documento describe la arquitectura tecnica de alto nivel de Art Unbound. El objetivo es alinear los sistemas clave, sus responsabilidades y dependencias, y establecer una base para la implementacion.

## 2. Resumen del Producto
Juego de puzzles en Realidad Mixta (RM) basado en hand tracking, con piezas 3D de 0.5 cm que se apoyan y se colocan sobre superficies reales. Incluye decoracion persistente del hogar mediante anclajes espaciales y contenido artistico local.

## 3. Metas No Funcionales
- Rendimiento: Priorizar framerate estable en Quest 3; objetivo minimo de 74 FPS por modo.
- Latencia: Interaccion tactil con respuesta inmediata para gestos de mano.
- Persistencia: Obras y anclajes restaurables entre sesiones.
- Offline: Acceso local a contenido incluido en la app.
- Contenido: Desbloqueo semanal local basado en reloj del sistema.

## 4. Vista General de la Arquitectura
- Cliente Unity (Quest): Render, interaccion, MR, fisica y UX.
- Integraciones MR (Presence Platform): Scene Understanding y anclajes espaciales.

## 5. Componentes Principales (Unity)
### 5.1. MR y Scene Understanding
- Deteccion de superficies: Mapeo de paredes para anclar lienzos.
- Decoracion contextual: Al seleccionar pared se muestra un marco de lineas para delimitar el lienzo. Solo dentro del marco las piezas se adhieren; si se sueltan fuera, regresan al scroll. Los marcos de color aparecen solo al terminar el cuadro. Tamano base: 70 cm de alto en modo portrait con ancho proporcional a la obra; en modo landscape el alto es 50 cm y el ancho se ajusta a la proporcion. El jugador puede redimensionar jalando una esquina.

### 5.2. Sistema de Piezas 3D
- Piezas con grosor real (0.5 cm) y sombras dinamicas.
- Tamano de pieza estandar: 5 cm x 5 cm (ancho x alto).
- Morfologia: pestañas y ranuras triangulares (sin curvas).
- Logica combinatoria: 3 estados por lado (plano, positivo, negativo) con hasta 3^4 = 81 variantes.
- Colocacion directa con pinza; sin deslizamiento sobre superficies, con ajuste por snap/validacion.
- Grid Snapping: rejilla invisible, alineacion al centro de celda, sin colisiones fisicas.

#### 5.2.1. Generacion de Meshes Triangulares
- Cada pieza se genera proceduralmente en runtime basado en su morfologia.
- Triangulo base: 1.5cm de lado, centrado en el borde.
- Positivo: triangulo extruido hacia afuera del borde.
- Negativo: triangulo recortado hacia adentro del borde.
- Plano: borde recto sin modificacion (usado en bordes externos del puzzle).
- UVs calculados para mapear la porcion correcta de la textura del cuadro.

#### 5.2.2. Estados de una Pieza
- InPool: Pieza en el carrusel, disponible para tomar.
- Grabbed: Pieza siendo manipulada por el usuario.
- Placed: Pieza colocada correctamente en el tablero.
- Returning: Pieza animandose de vuelta al carrusel (soltada fuera del lienzo).

### 5.3. Interaccion por Hand Tracking
- Gestos: pellizco para tomar/colocar, deslizamiento para navegar.
- Fallbacks y tolerancias: Alcance de pinza de 1 cm para tomar piezas. Al soltar una pieza a 3 cm o menos del lienzo/pared, se aplica snap magnetico.

### 5.4. Modos de Juego y Flujo
El juego ha unificado su flujo de entrada:

#### 5.4.1. Fase de Armado (Lienzo Flotante)
- **Modo por defecto**: Todos los puzzles comienzan flotando frente al usuario (estilo "Comfort Mode").
- Distancia base: 0.8m desde la cabeza del usuario.
- Angulo de inclinacion: 15° hacia el usuario.
- El lienzo sigue la orientacion inicial del usuario y luego se fija.
- Esto elimina la necesidad de escanear habitaciones complejas antes de jugar.

#### 5.4.2. Fase de Decoracion (Pared)
- Ocurre **despues** de completar el puzzle.
- El usuario selecciona una pared fisica para colgar su obra terminada.
- Utiliza Scene Understanding para detectar verticales.
- Permite persistencia de la posicion decorativa.

### 5.5. UI y Flujo de Juego
- Inicio Simplificado: Sin seleccion de modo "Galeria/Confort" al inicio. Directo a obra.
- Seleccion de obra: Lista de cuadros locales, cuadro semanal destacado.
- Seleccion de dificultad: 64, 144, 256 o 512 piezas.
- HUD durante gameplay: Tiempo, piezas colocadas, indicador de ayuda, boton pausa.
- Post-juego: Pantalla de resultados con stats, animacion de marco, opciones de accion.
- Menu de pausa: Continuar, toggle ayuda, abandonar puzzle.
- Modo ayuda: Opcion que activa feedback extra.
- Validacion de encuentro: verifica triangulos con piezas adyacentes.
- Error en ayuda: brillo rojo sutil si no embona con vecinos.
- Exito en ayuda: destello blanco/haptic solo si pieza correcta en su celda.

### 5.5.2. Galeria Personal
- Tab "Completadas": Lista de obras terminadas con records por dificultad.
- Tab "Colgadas": Cuadros activos en la casa del usuario.
- Tab "Guardadas": Cuadros retirados de las paredes (almacenados).
- Funcionalidades:
  - Ver detalle de obra con records (mejor tiempo/score por cada pieceCount).
  - Rejugar obra completada con selector de piezas.
  - Reubicar cuadro colgado a nueva posicion.
  - Retirar cuadro (mover a "Guardadas").
  - Colgar cuadro guardado de nuevo.
  - Eliminar cuadro permanentemente.

### 5.5.3. Onboarding (Primera Vez)
- Se muestra automaticamente la primera vez que se abre la app.
- Pasos guiados:
  1. Bienvenida y concepto del juego.
  2. Seleccion de modo (forzado a Confort para tutorial).
  3. Posicionamiento del lienzo.
  4. Gesto de pinza para tomar pieza.
  5. Colocar pieza en slot correcto.
  6. Navegacion del carrusel con scroll.
  7. Mini-puzzle de practica (4 piezas).
- Opcion de saltar en cualquier momento.
- Se puede reactivar desde Settings.

### 5.5.4. Configuracion (Settings)
- Modo Ayuda por defecto (on/off).
- Volumen de efectos de sonido.
- Volumen de musica (si aplica).
- Opcion para ver tutorial de nuevo.

### 5.5.1. Sistema de Puntuacion
- Formula base: `score = (pieceCount * 100) / timeSec`
- Multiplicador por dificultad: x1.0 (64p), x1.5 (144p), x2.0 (256p), x3.0 (512p)
- Penalizacion por ayuda: score * 0.5 si helpMode activo.
- Umbrales de marco:
  - Madera: score >= 0 (siempre)
  - Bronce: score >= 50
  - Plata: score >= 100
  - Oro: score >= 200 AND helpMode OFF
  - Ebano: score >= 300 AND helpMode OFF AND pieceCount >= 256

### 5.6. Sistema de Feedback
#### 5.6.1. Feedback Visual
- Sombras dinamicas de piezas sobre el lienzo.
- VFX de snap exitoso (destello blanco).
- VFX de error de colocacion (brillo rojo sutil).
- VFX de puzzle completado (particulas doradas).
- Animacion de marco aparecieno al completar.
- Highlight de paredes detectadas para seleccion.
- Highlight de pieza seleccionada/hover.

#### 5.6.2. Feedback Auditivo
- Sonido al tomar pieza.
- Sonido al colocar pieza (snap).
- Sonido de error (sutil).
- Sonido de puzzle completado (fanfarria).
- Sonido de revelacion de marco.

#### 5.6.3. Feedback Haptico
- Vibracion corta al tomar pieza.
- Vibracion de confirmacion al colocar.
- Vibracion doble al error.
- Vibracion larga al completar puzzle.
- Usa APIs de Meta XR para vibracion de manos.

### 5.7. Gestion de Cuadros Colgados
- Cuadros completados pueden colgarse en cualquier pared/posicion.
- Interaccion con cuadros colgados:
  - Pellizco largo activa menu contextual.
  - Opciones: Mover, Retirar, Eliminar.
- Reubicacion: Cuadro sigue la mano hasta soltar en nueva posicion.
- Retiro: Cuadro se guarda en galeria (tab "Guardadas").
- Limite: Maximo 20 cuadros activos simultaneamente (rendimiento).
- Restauracion al inicio: Se cargan anclajes y se muestran cuadros colgados.

### 5.8. Rendering y Sombras MR
- Sombras dinamicas: Las piezas (0.5cm de grosor) proyectan sombras sobre el lienzo.
- Luz direccional virtual alineada con la iluminacion estimada del entorno (si disponible).
- Fallback: Luz direccional fija desde arriba-izquierda (45°) si no hay estimacion.
- Shadow casting habilitado solo en piezas y marco para optimizar rendimiento.
- Calidad de sombras: Soft shadows con resolucion media (1024) para Quest 3.

### 5.9. Persistencia Local
- Assets locales: Contenido incluido en la app; no se descargan assets en tiempo de ejecucion.
- Datos locales: Preferencias, progreso ligero y ubicacion de cuadros en `Application.persistentDataPath` (persistente entre sesiones y actualizaciones). Datos temporales y cache en `Application.temporaryCachePath` (puede limpiarse por el SO).

## 6. Contenido y Datos Locales
- Distribucion: Actualizaciones trimestrales de la app con nuevos cuadros (12 por trimestre).
- Cuadro semanal: Desbloqueo basado en el reloj del sistema.
- Guardado local: Records y posiciones de cuadros en almacenamiento interno del visor.

## 7. Escalabilidad Futura (Evaluacion)
- Posible uso de servicios en la nube (Firebase) para:
  - Cloud Storage: descarga de cuadros individuales sin actualizar la app completa.
  - Firestore: respaldo de records y persistencia entre dispositivos.
  - Remote Config: eventos globales y ajustes de logica en tiempo real.

## 8. Persistencia de Anclajes
- Anclajes espaciales para obras colocadas en casa.
- Restauracion al iniciar: Se intenta cargar cada anclaje y reponer el cuadro en su lugar. Si falla, se muestra un mensaje y se solicita al usuario re-colocar el cuadro.

## 9. Pipeline de Assets
- Distribucion por actualizaciones de la app (sin descarga dinamica).
- Compresion: Se utiliza la calidad original del asset empaquetado.

## 10. Telemetria y Analitica
- Telemetria local opcional para QA (sin envio a backend).
- Eventos clave (sin PII): inicio/fin de puzzle, tiempo, ayuda_activa, fallos de anclaje y perdida de hand tracking.

## 11. Riesgos Tecnicos
- Variabilidad de iluminacion y deteccion de superficies.
- Consistencia de anclajes entre sesiones.

## 12. Pendientes y Decisiones Abiertas
- Versiones recomendadas (con soporte):
  - Unity 6.0 LTS.
  - OpenXR Plugin 1.16.1.
  - Meta OpenXR (com.unity.xr.meta-openxr) 2.1.0.
  - Meta XR Core/All-in-One SDK 83.0.1.
