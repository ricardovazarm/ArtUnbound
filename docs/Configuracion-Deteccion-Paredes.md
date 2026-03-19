# Guía de Configuración: Detección de Paredes en Art Unbound

Esta guía explica cómo configurar los servicios de detección de paredes para que funcionen correctamente en Meta Quest.

## Archivos Creados

Se han creado dos nuevos servicios en `Assets/ArtUnbound/Scripts/MR/`:

1. **`SpatialPermissionService.cs`** - Solicita el permiso `USE_SCENE` en runtime
2. **`WallDetectionService.cs`** - Detecta y cuenta las paredes del Scene Model

## Configuración en Unity Editor

### 1. Configurar GameBootstrap en la Escena Main

1. Abre la escena `Assets/ArtUnbound/Scenes/Main.unity`
2. Selecciona el GameObject `GameBootstrap` en la jerarquía
3. En el Inspector, busca la sección **"MR Services"**
4. Arrastra los siguientes GameObjects a los campos:
   - **Spatial Permission Service**: Arrastra el GameObject que contenga el componente `SpatialPermissionService`
   - **Wall Detection Service**: Arrastra el GameObject que contenga el componente `WallDetectionService`

### 2. Agregar los Servicios a la Escena

#### Opción A: Agregar a un GameObject existente (Recomendado)

1. Selecciona el GameObject `XR Origin` en la jerarquía
2. Click en `Add Component`
3. Busca y agrega `Spatial Permission Service`
4. Click en `Add Component` nuevamente
5. Busca y agrega `Wall Detection Service`

**Configurar referencias:**
- Ambos componentes buscarán automáticamente el `ARPlaneManager` si no está asignado
- Opcionalmente, arrastra el `ARPlaneManager` del `XR Origin` a los campos correspondientes

#### Opción B: Crear GameObjects separados

1. Click derecho en la jerarquía > `Create Empty`
2. Nómbralo `SpatialPermissionService`
3. Click en `Add Component` y agrega el script `Spatial Permission Service`
4. Repite para `WallDetectionService`

### 3. Configurar las Referencias en GameBootstrap

Una vez que los componentes están en la escena:

1. Selecciona `GameBootstrap` en la jerarquía
2. En el Inspector, en la sección **"MR Services"**:
   - Arrastra el GameObject con `SpatialPermissionService` al campo **Spatial Permission Service**
   - Arrastra el GameObject con `WallDetectionService` al campo **Wall Detection Service**

### 4. Verificar ARPlaneManager

1. Selecciona `XR Origin` en la jerarquía
2. Verifica que tenga el componente `AR Plane Manager`
3. Asegúrate de que:
   - **Detection Mode** = `Vertical` (solo paredes)
   - El componente está habilitado ✓

## Cómo Funciona

### Flujo de Ejecución

```
1. App inicia
   └─> SpatialPermissionService.Start()
       ├─> Revisa si permiso USE_SCENE está concedido
       │   ├─ SI → Habilita ARPlaneManager
       │   └─ NO → Solicita permiso al usuario
       │           ├─ Grant → Habilita ARPlaneManager
       │           └─ Deny → Muestra warning en logs

2. Usuario completa puzzle O entra a ver puzzle terminado
   └─> GameBootstrap.DetectAndLogWalls()
       └─> WallDetectionService.DetectWalls()
           ├─> Itera ARPlaneManager.trackables
           ├─> Filtra planos verticales
           └─> Devuelve conteo
               └─> PostGameController muestra botón "Colgar en pared"
```

### Criterios de Detección de Paredes

Un plano se considera "pared" si cumple **cualquiera** de estos criterios:

1. **Alignment Vertical**: `plane.alignment == PlaneAlignment.Vertical`
2. **Clasificación Semántica**: Contiene alguna de estas etiquetas de Meta:
   - `WallFace`
   - `InnerWallFace`
   - `DoorFrame`
   - `WindowFrame`

## Troubleshooting

### Problema: "No planes from Scene Model"

**Causas posibles:**
1. ❌ Space Setup no completado en el Quest
2. ❌ Permiso `USE_SCENE` denegado
3. ❌ Planes feature no habilitado en XR Plug-in Management

**Soluciones:**
1. En el Quest: `Settings > Physical Space > Space Setup` → Completar escaneo
2. Aceptar el diálogo de permiso cuando aparezca
3. En Unity: `Edit > Project Settings > XR Plug-in Management > Meta Quest` → Activar `Planes`

### Problema: "WallDetectionService not found"

**Causa:**
- Los servicios no están asignados en el Inspector de GameBootstrap

**Solución:**
- Seguir la sección "Configuración en Unity Editor" de arriba

### Problema: Con Meta Horizon Link no detecta paredes

**Causa conocida:**
- Link puede transmitir planos vacíos incluso con todo configurado correctamente

**Solución:**
- Usar **Build and Run** en el Quest para verificar
- En la app de Meta Quest Link (PC): `Settings > Developer > Spatial Data over Meta Horizon Link` → `Turn On`

### Logs de Diagnóstico

El sistema genera logs detallados para troubleshooting:

```
[SpatialPermission] Permission already granted, enabling plane manager.
[WallDetection] Found 4 wall(s) in the room. (Total planes: 8)
[WallDetection] Plane #1 align=Vertical vert=true cls=WallFace
[WallDetection] Plane #2 align=Horizontal vert=false cls=Floor
```

Si ves `wallCount=0`, revisa los logs para identificar la causa.

## Testing Checklist

Antes de hacer build final, verifica:

- [ ] `SpatialPermissionService` está en la escena y asignado en GameBootstrap
- [ ] `WallDetectionService` está en la escena y asignado en GameBootstrap
- [ ] `ARPlaneManager` existe en XR Origin con `DetectionMode=Vertical`
- [ ] Permiso `USE_SCENE` está en el manifest (ya incluido en `SpatialPermission.androidlib`)
- [ ] Al completar un puzzle, se muestra el conteo de paredes en logs
- [ ] Al abrir un puzzle completado, se detectan las paredes inmediatamente
- [ ] En Build and Run, el diálogo de permiso aparece al iniciar la app

## Referencias

- Documentación Meta: [Unity Spatial Data Permission](https://developers.meta.com/horizon/documentation/unity/unity-spatial-data-perm)
- AR Foundation: [Plane Detection](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.0/manual/features/plane-detection/)
