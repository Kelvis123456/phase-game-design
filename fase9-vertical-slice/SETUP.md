# PHASE VS — Guía de Setup en Unity

Sigue estos pasos en orden. Cada sección corresponde a un sprint.

---

## Paso 0 — Crear el proyecto

1. Unity Hub → **New Project** → template **"2D (URP)"** → nombre: `PHASE` → Create
2. Esperar que abra. Ignorar las advertencias iniciales del template.

### Packages a instalar (Window → Package Manager)
| Package | Cómo encontrarlo |
|---------|-----------------|
| Input System 1.6+ | Unity Registry → buscar "Input System" |
| 2D PixelPerfect | Unity Registry → buscar "2D Pixel Perfect" |

Cuando instales Input System: Unity pregunta si reiniciar → **Yes**.

### FMOD (después de Sprint 1)
- Descargar **FMOD for Unity** desde fmod.com → tu cuenta → Downloads
- Instalar el `.unitypackage` arrastrándolo al proyecto
- Crear una cuenta FMOD gratuita, conectar el plugin en Unity

---

## Paso 1 — Project Settings

**Edit → Project Settings:**

### Player
- Company Name: `[TuNombre]Studio`
- Product Name: `PHASE`
- Default Orientation: **Landscape Left**
- Other Orientations: activar también **Landscape Right**
- Active Input Handling: **Both** (Input System + legacy, durante desarrollo)

### Physics 2D
- Gravity Y: `-35`
- Simulation Mode: **Update**

### Layers (Edit → Project Settings → Tags and Layers)
Crear estos layers:
- Layer 6: `Ground`
- Layer 7: `Player`
- Layer 8: `Echo`
- Layer 9: `Hazard`
- Layer 10: `Platform`

**Matrix de colisiones** (Physics 2D → Layer Collision Matrix):
- Echo NO colisiona con: Ground, Player, Hazard, Platform (los ecos son cinemáticos)

### Sorting Layers (mismo lugar, pestaña Sorting Layers)
En este orden exacto:
```
Background_Far
Background_Mid
Background_Near
Terrain
Hazard
Echo_3
Echo_2
Echo_1
Player
VFX
UI_World
UI_HUD
```

### Tags
Crear tags: `Player`, `Echo`, `Hazard`, `Ground`

---

## Paso 2 — Configurar URP y Post-Processing

1. En `Assets/Settings/` busca el archivo **UniversalRenderPipelineAsset**
2. Hacer clic → en Inspector:
   - HDR: **ON**
   - MSAA: **Disabled**
   - Post Processing: **ON**

### Global Volume (Post-Processing)
1. Hierarchy → Create → Volume → Global Volume
2. Add Override:
   - **Bloom**: Threshold=0.8, Intensity=1.2, Scatter=0.7 ✓
   - **Vignette**: Color=#000000, Intensity=0.25, Smoothness=0.4 ✓
   - **Chromatic Aberration**: Intensity=0.0 ✓
3. Guardar el Profile como `Assets/Settings/PHASE_PostProcess.asset`

### Pixel Perfect Camera
1. Seleccionar la **Main Camera**
2. Add Component → **Pixel Perfect Camera**:
   - Asset Pixels Per Unit: `16`
   - Reference Resolution: `480` × `270`
   - Upscale Render Texture: **ON**
   - Crop Frame: **Both**
3. Camera → Projection: Orthographic, Size: `8.4375` (= 270/2/16)

---

## Paso 3 — Importar scripts

1. En el proyecto Unity, crear carpeta: `Assets/_PHASE/`
2. Copiar la carpeta `Scripts/` de esta carpeta (`fase9-vertical-slice/Scripts/`) dentro de `Assets/_PHASE/`
3. Copiar `Shaders/EchoShader.shader` a `Assets/_PHASE/Shaders/`
4. Esperar a que Unity compile (barra de progreso abajo)

Si hay errores de compilación relacionados con FMOD: comentar temporalmente las líneas de FMOD en `TimeManager.cs`.

---

## Paso 4 — Crear el Tilemap de la sala

1. Hierarchy → Create → **2D Object → Tilemap → Rectangular**
   - Esto crea: Grid → Tilemap
2. Seleccionar **Tilemap** → Inspector:
   - Sorting Layer: `Terrain`
   - Order in Layer: `0`
3. Add Component → **Tilemap Collider 2D**
4. Add Component → **Composite Collider 2D** (esto agrega Rigidbody2D automáticamente)
5. Rigidbody2D que se agregó → Body Type: **Static**
6. Tilemap Collider 2D → **Used By Composite**: activar ✓
7. Layer del GameObject Grid: `Ground`

### Crear un Tile placeholder
1. Crear un sprite 16×16 blanco o de color solido: puedes usar `Assets/Create → Sprite (Square)`
2. Window → **2D → Tile Palette** → Create New Palette → nombre `PHASE_Tiles`
3. Arrastrar el sprite al Tile Palette → se crea el tile
4. Pintar el suelo de la sala con el tile

### Diseño de la sala (referencia del VS doc)
- Suelo base: y=0, ancho 30 tiles
- Plataforma A (zona tutorial): y=3, 6 tiles de ancho
- Plataforma B (con gap): y=0, dejar gap de 3 tiles en el centro
- Pinchos en el gap: y=-1 (debajo del gap, visibles desde arriba)
- Plataformas escalonadas: y=2, 4, 6 en zigzag
- Meta (llegada): plataforma al final derecho

---

## Paso 5 — Setup de GameObjects

### Hierarchy resultado esperado:
```
Bootstrap          → VSBootstrap.cs, TimeManager.cs, InputReader.cs, VFXPool.cs
Main Camera        → Camera + PixelPerfectCamera + AudioListener
Global Volume      → Volume con PHASE_PostProcess.asset
Grid               → (Tilemap de la sala)
  Tilemap          → TilemapRenderer + TilemapCollider2D + CompositeCollider2D
Player             → PlayerController + PlayerStats + InputRecorder + SpriteRenderer + Animator + CapsuleCollider2D
EchoManager        → EchoManager.cs (referenciar al Player y LoopTimer)
LoopTimer          → LoopTimer.cs
VSRoomController   → VSRoomController.cs
HazardSpikes       → (GameObject padre vacío)
  Spike_1          → Sprite + HazardSpike.cs + PolygonCollider2D
Canvas_HUD         → Canvas (Screen Space Overlay)
  TimerRing        → Image (Filled, Radial 360)
  DeathFlash       → Image (negro, alpha 0, stretch to fill)
  DeathCount       → Text
```

### Bootstrap Setup
1. Create Empty → nombre `Bootstrap`
2. Add Component → `VSBootstrap`
3. Add Component → `TimeManager` → arrastrar el Global Volume al campo _globalVolume
4. Add Component → `InputReader`
5. Add Component → `VFXPool` (por ahora sin entries — agregar en Sprint 4)
6. En VSBootstrap: arrastrar referencias de TimeManager, InputReader, VFXPool

### Player Setup
1. Create Empty → nombre `Player`
2. Layer: `Player` | Tag: `Player`
3. Add Component → `PlayerController`
4. Add Component → `PlayerStats`
5. Add Component → `InputRecorder`
6. Add Component → `SpriteRenderer`:
   - Sorting Layer: `Player`
   - Sprite: cualquier sprite 16×32 (placeholder)
7. Add Component → `CapsuleCollider2D`:
   - Size: (0.75, 1.75) — ligeramente más pequeño que el sprite
   - Capsule Direction: Vertical
8. En PlayerController → arrastrar: SpriteRenderer, y configurar Ground Mask = `Ground`
9. Posición inicial: (0, 1)

---

## Paso 6 — Primera Build en Android (Sprint 1)

Antes de continuar al Sprint 2, probar en dispositivo real:

1. File → Build Settings → Android → Switch Platform
2. Player Settings:
   - Minimum API Level: Android 8.0 (API 26)
   - Target API Level: Automatic (highest)
   - Scripting Backend: **IL2CPP**
   - Target Architectures: **ARM64** ✓ (desmarcar ARMv7)
3. Build And Run (conectar dispositivo con USB + activar Developer Options + USB Debugging)

Si falla la build por FMOD: eliminar el plugin temporalmente hasta Sprint 3.

---

## Sprint 2 — Bullet-Time: cosas a hacer en Unity

Después de que el movimiento se sienta bien:

1. `TimeManager` ya está configurado — verificar que los valores en Inspector se pueden ajustar en runtime
2. En Play Mode: probar Bullet-Time con **Z** en teclado (InputReader lo maneja)
3. Ajustar en Inspector mientras juegas:
   - `_bulletTimeScale`: 0.1 (empezar aquí)
   - `_smoothSpeed`: 10
   - `_vignetteNormal`: 0.25 / `_vignetteBulletTime`: 0.55
4. Hacer build en Android y probar el touch: mantener dedo quieto = bullet-time

---

## Sprint 3 — Echo: cosas a hacer en Unity

1. Crear prefab `Echo.prefab`:
   - Empty GameObject
   - Add: SpriteRenderer (mismo sprite que el jugador, Sorting Layer: Echo_1)
   - Add: Animator (mismo Animator Controller que el jugador)
   - Add: EchoPlayer → arrastrar SpriteRenderer y Animator
   - **Arrastrar EchoShader.shader**: crear un Material `EchoMaterial.mat` con el shader, asignarlo al SpriteRenderer del prefab
2. EchoManager → arrastrar: prefab Echo, el componente InputRecorder del Player, y LoopTimer
3. LoopTimer → configurar: _loopDuration=8, arrastrar la Image del TimerRing del HUD

---

## Checklist de Go / No-Go (rellenar después del playtesting)

```
[ ] Movimiento 60fps en Android real
[ ] Bullet-time se activa predeciblemente
[ ] Eco se entiende como "yo del pasado"
[ ] Momento aha ocurre en > 60% de los testers
[ ] > 50% pide seguir jugando
[ ] 0 abandonos por frustración en 5 muertes
[ ] Decisiones Memory Decay / BT duration / Timer framing documentadas
```
