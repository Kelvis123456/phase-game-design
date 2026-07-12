# PHASE — Documento de Dirección Artística
> Versión 1.0 | Fase 6 completada

---

## 0. Propósito de este documento

Este documento define la identidad visual completa de PHASE. Todo artista, desarrollador o diseñador que trabaje en el juego debe leerlo antes de crear cualquier asset. Las decisiones aquí son **vinculantes** — cualquier cambio requiere revisión del Art Director.

**Premisa central:** PHASE es un juego sobre el tiempo. El arte debe hacer el tiempo *visible*, *físico* y *hermoso*. El mundo es oscuro pero no deprimente; el peligro es claro pero no ruidoso; los ecos son fantasmas pero no aterradores — son compañeros.

---

## 1. Identidad Visual

### 1.1 Concepto artístico en una frase
> "Un void geométrico donde el pasado deja rastros luminosos de colores."

### 1.2 Mood
- **Atmósfera**: Silenciosa, concentrada, casi meditativa. Como resolver un cubo Rubik en la oscuridad, con destellos de neón marcando cada movimiento correcto.
- **Tono**: Elegante y misterioso, no terrorífico. Pixel art con alta calidad de lectura.
- **Sensación al jugar**: El mundo es oscuro → los ecos son los que dan color → cuando llenas la pantalla de ecos activos, se vuelve *vibrante*.

### 1.3 Referentes visuales (descripción — no copias)
| Referente | Qué tomamos |
|-----------|-------------|
| **INSIDE (Playdead)** | Silhouette legibility, profundidad atmospheric, contraste extremo |
| **Hyper Light Drifter** | Accent neón sobre fondos oscuros, worldbuilding silencioso |
| **Celeste** | Lectura pixel art clara, personaje pequeño legible, plataformas definidas |
| **Transistor** | Ecos/reflejos como entidades con carácter, glows narrativos |
| **Dead Cells** | Fluidez de animación pixel art, world que comunica peligro visualmente |

### 1.4 Lo que NO somos
- ❌ **No cyberpunk**: No luces de neón sobre ciudades. Nuestro mundo es vacío, no urbano.
- ❌ **No cute/cozy**: Los ecos son entidades, no mascotas de colores.
- ❌ **No fotorrealista**: Pixel art estricto. Nunca mezclar con assets HD.
- ❌ **No colorido por defecto**: El color es *ganado* — aparece en ecos, cristales y momentos especiales. El mundo base es casi monocromático.

---

## 2. Pilares Artísticos

Análogos a los Pilares de Diseño del GDD, estos cinco principios gobiernan cada decisión visual:

### PILAR A — Legibilidad antes que belleza
En todo momento el jugador debe identificar instantáneamente: personaje, ecos, suelo, peligro. Si hay duda entre "bonito" y "claro", gana "claro".

### PILAR B — El color es información
Cada eco tiene su color. Ese color también aparece en el trigger correspondiente. Nunca usar colores de eco para elementos neutros. El cyan (#3AFFD4) es exclusivamente del jugador/UI — nunca en enemigos o trampas.

### PILAR C — La oscuridad es el lienzo
El fondo oscuro no es "fondo vacío" — es el contexto que hace brillar todo lo demás. Fondo oscuro + objeto luminoso = atención dirigida. No llenar el fondo con decoración.

### PILAR D — El tiempo tiene textura
Los ecos dejan rastros visuales sutiles. Las acciones pasadas tienen *residuo*. El bullet-time cambia visualmente la pantalla completa. El tiempo debe sentirse físico.

### PILAR E — Cada frame cuenta
Pixel art a 480×270 es pequeño. Cada píxel de un sprite de 16×32 equivale a una pincelada en un cuadro grande. No desperdiciar píxeles en detalles invisibles — invertirlos en la silueta y el movimiento.

---

## 3. Resolución y Escala

### 3.1 Resolución base del juego
```
Resolución virtual: 480 × 270 píxeles (16:9)
Tamaño de tile base: 16 × 16 píxeles
Mapa en tiles: 30 tiles ancho × ~17 tiles alto
Pixel size on device: 1 pixel virtual ≈ 1.6 px en pantalla 390pt wide
```

### 3.2 Configuración Unity
- **PixelPerfectCamera**: activado, modo `Stretch`
- **Reference resolution**: 480 × 270
- **PPU (Pixels Per Unit)**: 16
- **Filtro de texturas**: Point (sin anti-aliasing)
- **Compresión sprites**: ninguna (o ETC2 sin pérdida visible)

### 3.3 Capas de renderizado (Unity Sorting Layers)
```
0 — VoidFar       (fondo más lejano, estrellas)
1 — VoidMid       (capa atmosférica media)
2 — Background    (decoración de fondo, pilares, arquitectura lejana)
3 — Terrain       (tiles sólidos — suelo, paredes)
4 — Hazards       (pinchos, láseres, trampas)
5 — Interactives  (plataformas móviles, puertas, triggers)
6 — Collectibles  (cristales, items)
7 — Echoes        (todos los ecos, de más antiguo a más nuevo)
8 — Player        (jugador — siempre encima de ecos)
9 — VFX           (partículas, glows, trails)
10 — UIWorld      (elementos UI anclados al mundo — tutoriales en-game)
11 — HUD          (UI fija — barra de vida, timer, echo strip)
```

---

## 4. Paleta de Colores del Mundo

La paleta del mundo de juego es independiente de la paleta UI (definida en Fase 5).

### 4.1 Fondos (Background layers)
```
Void Deep    #010308   Capa más lejana — casi negro con micro-tinte azul
Void Far     #060B14   Segunda capa — muy oscuro
Void Mid     #0A1222   Tercera capa — oscuro navy, aquí van micro-estrellas
Star Color   #8090A8   Color de los píxeles-estrella (1×1, muy dispersos)
```

### 4.2 Terreno (Terrain tiles)
```
Stone Base   #0E1A2E   Tile base — suelo y paredes
Stone Shade  #080F1E   Sombra interna del tile
Stone Mid    #111E34   Variante media para bloques
Stone Rim    #1C2E48   Borde iluminado (píxel superior del tile)
Stone Top    #223548   Superficie superior del suelo (1 px highlight)
Moss Accent  #0A1818   Tile con micro-textura húmeda (variante rara)
```

### 4.3 Personaje Jugador
```
Player Light  #E8EEF8   Zona iluminada (frente del cuerpo)
Player Mid    #B0BDD4   Zona media
Player Dark   #6878A0   Zona en sombra
Player Outline #000000  Contorno (1px)
```

### 4.4 Ecos (5 slots) — Colores definitivos
```
Eco 1 — CYAN    #3AFFD4   (el primero, más antiguo)
Eco 2 — VIOLET  #A855F7
Eco 3 — EMBER   #F97316
Eco 4 — VERDANT #22C55E
Eco 5 — MAGENTA #EC4899
```

**Regla de opacidad de ecos en gameplay:**
- Eco activo (loop corriendo): 65% opacidad
- Eco en pausa (bullet-time): 85% opacidad (se vuelven más visibles)
- Eco en último frame antes de desaparecer: flash 100% → 0% en 0.3s

**Shader de eco en Unity:**
- Reemplazar color: Sprite Shader custom que mapea blanco → color de eco
- El sprite del jugador es WHITE sobre transparent → shader lo pinta en cualquier color
- Parámetros: `_EchoColor` (Color), `_Opacity` (float 0-1), `_EmissionIntensity` (float 0-1)

### 4.5 Peligros (Hazards)
```
Spike Base    #FF4060   Rojo peligro — pinchos, bordes letales
Spike Glow    #FF406025  Ambient glow (additive)
Laser Core    #FFA030   Naranja — láseres de disparo
Laser Glow    #FFA03020  Ambient del láser
Crusher       #1E2E48   Oscuro — plataformas aplastadoras (son grandes, no necesitan glow)
Crusher Edge  #2A3E5A   Borde ligeramente más claro
```

### 4.6 Interactivos y coleccionables
```
Crystal Core  #3AFFD4   Cristal sólido (mismo que accent UI)
Crystal Glow  #3AFFD445  Halo ambiental del cristal
Portal Ring   #FFFFFF90  Portal de salida — blanco con transparencia
Portal Inside #3AFFD420  Interior del portal
Plate Idle    #1A2E45   Plataforma presión inactiva
Plate Active  #3AFFD4   Plataforma presión activada (mismo cyan)
Gate Locked   #2A1A40   Puerta bloqueada
Gate Open     #3A2060   Puerta abierta
```

---

## 5. Diseño del Personaje Jugador

### 5.1 Especificaciones del sprite
```
Tamaño sprite: 16 × 32 píxeles
Canvas sprite: 16 × 32 (sin padding — hitbox tight)
Hitbox en Unity: 12 × 28 (2px de padding visual en lados)
Origen (pivot): centro-inferior (foot)
PPU: 16
```

### 5.2 Concepto de diseño
El jugador es una **figura encapuchada y etérea** — humanoide, pero sin facciones visibles. Esto por tres razones:
1. Legibilidad a 16×32px — detalles faciales son ruido
2. Proyección del jugador — sin género ni raza definidos
3. Metáfora de la mecánica — "tú eres todos tus momentos pasados y futuros"

**Estructura del sprite (16×32 pixels distribuidos):**
```
Y  0-5:   Cabeza/capucha (forma de lágrima invertida, ~10px ancho)
Y  6-12:  Torso (cuerpo bajo la capucha, ligeramente más angosto)
Y 13-22:  Cadera y piernas (área de movimiento de la animación)
Y 23-31:  Pies (contacto con el suelo)

X  2-14:  Zona del cuerpo (2px de margen lateral)
```

**Paleta de colores usada en sprite (máx. 4 colores + transparente):**
```
Transparente    ——       Background (no pixel)
Player Light    #E8EEF8  Zona frontal / iluminada
Player Mid      #B0BDD4  Transición media
Player Dark     #6878A0  Zonas traseras / en sombra
Outline         #000000  Contorno exterior (1px)
```

### 5.3 Estados de animación
```
Estado         Frames  FPS  Loop  Descripción
─────────────────────────────────────────────────────
idle           2       4    sí    Micro-flotación vertical (±1px)
walk           6       12   sí    Walk cycle completo
run            6       16   sí    Idéntico a walk pero más agresivo
jump_rise      3       12   no    Anticipation + elevación + apex
fall           2       8    sí    Cuerpo extendido hacia abajo
land           2       16   no    Squash (1f) + recovery (1f)
bullet_time    2       4    sí    Ligero "pulso" — echo de outline
hurt           3       12   no    Flash rojo (1f) + knockback (2f)
death          4       8    no    Dissolve en partículas (4 frames)
```

**Total frames del personaje**: ~30 frames  
**Spritesheet recomendado**: 128×64 px (8×2 grid de frames 16×32)

### 5.4 Efecto de bullet-time sobre el sprite del jugador
Cuando el jugador activa bullet-time (dedo quieto):
1. **Outline glow**: se activa un shader que añade 1px de outline en color `#3AFFD480`
2. **Trail effect**: los últimos 3 frames de posición se renderizan a 10% opacidad c/u
3. **Micro-vibración**: el sprite oscila ±0.5px horizontal a 4fps (sentido de tensión temporal)

### 5.5 Eco visual — el mismo sprite, diferente color
```
// Unity C# pseudocode (shader property)
echoSpriteRenderer.material.SetColor("_EchoColor", echoColor);
echoSpriteRenderer.material.SetFloat("_Opacity", 0.65f);
echoSpriteRenderer.material.SetFloat("_EmissionIntensity", 0.2f);
```
Los ecos NO tienen sprite propio. Reusan el SpriteAtlas del jugador con el shader aplicado. Esto garantiza que:
- Animaciones de eco son idénticas al jugador en ese momento del loop
- Memoria: 1 atlas = 5 ecos + jugador (ahorro ~83%)
- Actualizar el sprite del jugador = actualizar todos los ecos automáticamente

---

## 6. Diseño del Entorno — Tilesets

### 6.1 Estructura del tileset principal
```
Atlas: TileAtlas_World01.png
Tamaño: 256 × 256 px
Tile size: 16 × 16 px
Grid: 16 × 16 = 256 tiles máx
PPU: 16
```

### 6.2 Categorías de tiles (posición en atlas)
```
Fila 0 (tiles 0-15):   Ground — Suelo sólido (9 variantes auto-tile)
Fila 1 (tiles 16-31):  Wall — Paredes (izq, centro, der + variantes)
Fila 2 (tiles 32-47):  Platform — Plataformas flotantes (top only, semi-sólido)
Fila 3 (tiles 48-63):  Background — Decoración de fondo (pillars, arcos)
Fila 4 (tiles 64-79):  Hazard — Pinchos (8 orientaciones + bases)
Fila 5 (tiles 80-95):  Interactive — Plates, switches (2 estados cada uno)
Fila 6 (tiles 96-111): Void Detail — Micro-detalles del fondo (grietas, markings)
Fila 7 (tiles 112-127): Special — Portales, crystal spawns, boss markers
```

### 6.3 Reglas de diseño de tiles
1. **Auto-tiling**: Los tiles de suelo y paredes deben ser compatibles con Unity's Rule Tile (9-slice)
2. **Rim lighting**: El pixel superior de cada tile de suelo lleva `Stone Top #223548` para simular luz desde arriba
3. **No gradientes internos**: Los tiles son planos con 2-3 colores máximo
4. **Coherencia de orientación**: La "luz" siempre viene desde arriba y ligeramente a la izquierda
5. **1px outline**: Algunos tiles clave tienen contorno negro de 1px para separarse del fondo

### 6.4 Tipos de sala y su paleta extendida
```
Sala STANDARD (azul-gris):
  Terrain: Paleta base (Stone Base/Rim/Shade)
  Accent: ninguno extra

Sala PELIGRO (rojo):
  Terrain: igual base
  Accent: Spike glow rojo ambient en paredes → tinte rojo muy sutil
  Fondo mid: #160A0A en vez del estándar #0A1222

Sala ECO-PUZZLE (cyan):
  Terrain: igual base pero con Stone Top más claro (#2A4060)
  Accent: platforms con borde cyan tenue #3AFFD420
  Fondo mid: #06101A (tinte más azul)

Sala BOSS:
  Terrain: igual base
  Fondo: todo más oscuro — Void Deep más prominente
  Accent: color del boss según tipo (primer boss: rojo-naranja)
  Efecto especial: vignette oscura en bordes (post-processing)
```

---

## 7. Enemigos y Obstáculos

*(Diseñados en el GDD §5. Aquí solo las specs visuales.)*

### 7.1 Filosofía visual de enemigos
- **No humanoides**: Los enemigos son mecanismos del mundo, no criaturas. Son parte del puzzle.
- **Telegrafía visual**: Antes de activarse, todo enemigo tiene un ciclo de "carga" visible (glow pulsante).
- **Paleta separada**: Enemigos usan rojo/naranja — colores que nunca aparecen en el jugador ni sus ecos.

### 7.2 Specs básicas de sprites
```
Guardián (turret estático):
  Sprite: 16 × 16 px
  Estados: idle (1 frame), charge (3 frames, rojo pulsante), fire (2 frames)
  Color: Hazard #FF4060 con cuerpo en Stone Dark

Perseguidor (móvil básico):
  Sprite: 12 × 16 px (más angosto que el jugador)
  Estados: patrol (4 frames), aggro (4 frames más rápido), stun (1 frame)
  Color: Laser #FFA030

Crusher (plataforma aplastadora):
  Sprite: 32 × 16 px (2 tiles ancho, 1 alto)
  Estados: idle, descend (movimiento), impact (1 frame, squash leve)
  Color: Crusher #1E2E48 con edge #2A3E5A
```

---

## 8. VFX — Especificaciones Completas

### 8.1 Sistema de partículas: reglas generales
- **Sin shader particles**: Usar sprites simples para partículas (batería móvil)
- **Máx partículas simultáneas**: 150 en pantalla completa
- **Duración máxima**: 1.5 segundos por efecto
- **Blend mode**: Additive para glows y trails; Alpha para impacts

### 8.2 Efecto: Creación de eco ("Print Moment")
```
Disparador: Cuando el jugador completa un loop y el eco comienza
Duración: 0.4 segundos
Efecto:
  1. Flash en posición de inicio del loop:
     - Frame 1 (0.0-0.1s): Rectángulo 16×32 en color de eco al 100% opacity
     - Frame 2 (0.1-0.2s): 70% opacity + expand ×1.2
     - Frame 3 (0.2-0.4s): 0% opacity + expand ×1.6
  2. Burst radial: 6 partículas (2×2 pixels) en color eco
     - Dirección: aleatorio radial
     - Velocidad: 40-60 px/s
     - Vida: 0.3s, fade out
```

### 8.3 Efecto: Bullet-time activación
```
Disparador: Jugador deja quieto el dedo
Duración: Mientras dure (no tiene fin propio)
Efectos:
  1. Screen edge vignette:
     - Aparece en 0.2s
     - Gradiente radial desde bordes: #00000040 → transparente
     - Tinte global de pantalla: saturación -20%
  2. Time echo trail del jugador:
     - Muestra las últimas 3 posiciones del jugador
     - Cada posición: sprite del jugador a 8% opacity, tinte blanco
     - Spacing: posición del frame -2, -4, -6 frames atrás
  3. Echo brightening:
     - Todos los ecos: EmissionIntensity de 0.2 → 0.5 en 0.2s
     - Opacity de ecos: 0.65 → 0.85 en 0.2s

Fin del bullet-time: revertir todo en 0.3s
```

### 8.4 Efecto: Eco completado (loop termina)
```
Disparador: Un eco completa su ciclo y desaparece
Duración: 0.6 segundos
Efectos:
  1. Flash en posición actual del eco:
     - Frame 1 (0.0-0.1s): sprite eco al 100%
     - Frame 2 (0.1-0.3s): expand a ×1.5, opacity 40%
     - Frame 3 (0.3-0.6s): opacity 0%
  2. Ripple ring: ellipse expandiéndose
     - Inicio: 16×32 px (tamaño del eco)
     - Fin (0.5s): 80×80 px
     - Color: eco color, opacity 80% → 0%
     - Stroke width: 2px → 0px
  3. 8 partículas radiales (ver 8.2 para specs)
```

### 8.5 Efecto: Colectar cristal
```
Disparador: Jugador toca cristal
Duración: 0.5 segundos
Efectos:
  1. Cristal desaparece (instantáneo)
  2. Burst: 8 partículas de 2×2 px, cyan #3AFFD4
     - Dirección: octogonal radial (cada 45°)
     - Velocidad: 50-70 px/s
     - Vida: 0.4s, fade + gravity(-20)
  3. Número flotante "+N" (N = valor):
     - Font: IBM Plex Mono 8px equivalent
     - Color: #3AFFD4
     - Sube 20px en 0.5s
     - Opacity: 100% → 0%
  4. HUD flash: contador de cristales destella cyan 0.2s
```

### 8.6 Efecto: Daño al jugador
```
Disparador: Colisión con hazard
Duración: 0.6 segundos
Efectos:
  1. Screen flash: #FF406030 durante 0.1s (pantalla completa)
  2. Sprite jugador: material swap a color #FF4060 durante 0.1s
  3. Knockback: fuerza física aplicada
  4. Invincibility frames: jugador parpadea cada 0.1s durante 0.5s
```

### 8.7 Efecto: Muerte del jugador
```
Disparador: HP llega a 0
Duración: 1.2 segundos (luego fade to black)
Efectos:
  1. Sprite jugador se fragmenta en 8 piezas de ~4×4 px
     - Cada pieza: sale en dirección radial aleatoria
     - Velocidad: 30-80 px/s
     - Rotación: aleatoria
     - Vida: 1.0s, fade out
  2. Screen: fade to black 0.8s-1.2s
  3. Audio: cue de muerte + silencio
```

---

## 9. Animación — Principios

### 9.1 Reglas de animación pixel art para PHASE
1. **12 FPS base**: La mayoría de animaciones van a 12fps. Acciones rápidas (land, hurt) a 16fps.
2. **Squash & Stretch como único frame**: En pixel art de 16×32, S&S se logra con 1 frame de altura ±2px.
3. **Anticipación de 1 frame**: Saltar, atacar y aterrizar tienen 1 frame de anticipación antes de la acción.
4. **Silueta primero**: Al diseñar cada frame, el contorno exterior es lo primero. Interior rellena después.
5. **Consistencia entre ecos**: Recordar que los ecos reusan los frames. Nunca hay animaciones exclusivas para ecos.

### 9.2 Walk cycle de 6 frames (especificación detallada)
```
Frame 1: Contact — pie izquierdo adelante, peso en él
Frame 2: Down — cuerpo en punto más bajo
Frame 3: Passing — pies al mismo nivel, cuerpo sube
Frame 4: Contact — pie derecho adelante (espejo del F1)
Frame 5: Down — cuerpo en punto más bajo (derecho)
Frame 6: Passing — cuerpo sube de nuevo
```
La capucha del personaje tiene una oscilación de 1px sincronizada con los pasos (sube en Contact, baja en Passing).

### 9.3 Jump arc visual
El jugador tiene una fase de **"coyote jump"** (GDD §3): puede saltar 0.1s después de salir del borde. Visualmente:
- Si salta desde un borde → animación de salto normal
- Si cae sin saltar → hay un frame de "sorpresa" (pie en el aire sin anticipación)
Esta distinción es narrativa, no mecánica — pero hace el personaje más legible.

---

## 10. Cámara y Presentación

### 10.1 Comportamiento de cámara
```
Tipo: Orthographic, tamaño calculado para 480×270
Follow mode: SmoothDamp con damping 0.15s horizontal, 0.1s vertical
Lead: 30px adelante del personaje en dirección de movimiento
Dead zone: 40px horizontal, 20px vertical (cámara no sigue micro-movimientos)
```

### 10.2 Camera bounds
- La cámara no puede mostrar fuera de los límites de la sala
- Transición entre salas: fade black 0.3s → nuevo room → fade in 0.3s
- Los ecos PERSISTEN durante la transición (se mantiene el loop sonoro)

### 10.3 Parallax layers
```
Layer VoidFar:  0.05x scroll (casi estático — estrellas)
Layer VoidMid:  0.15x scroll (subtle movement)
Layer Background: 0.35x scroll (arcos, pilares lejanos)
Layer Terrain:  1.0x scroll (mismo que cámara)
```

### 10.4 Post-processing (Unity URP)
- **Bloom**: Intensity 0.4, Threshold 0.8 — solo objetos muy brillantes hacen glow
- **Vignette**: 0.3 intensidad permanente (oscurece bordes suavemente)
- **Color Grading**: ligero tinte azul-frío (Lift +0.02 blue), contraste +15
- **Chromatic Aberration**: 0 normalmente → 0.8 durante bullet-time (se añade en 0.2s)

**REGLA**: Todos los post-processing deben tener opción de desactivación (accesibilidad). La cromática y el bloom son las más sensibles.

---

## 11. Restricciones Técnicas

### 11.1 Límites de assets
```
Atlas personaje:   128 × 64 px   (30 frames max, 16×32 cada uno)
Atlas tileworld:   256 × 256 px  (256 tiles de 16×16)
Atlas enemigos:    128 × 128 px  (sprites varios)
Atlas VFX:         128 × 128 px  (partículas y efectos)
Atlas UI:          512 × 512 px  (elementos HUD)
```

### 11.2 Formato de exportación
- **Sprites**: PNG con transparencia alfa, sin compresión, exportar en tamaño exacto
- **Atlas**: Unity Sprite Atlas (auto-packing), max 1024×1024 por atlas
- **Animaciones**: Unity Animator con Sprite Renderer, clips por estado
- **Naming**: `SPR_[Entidad]_[Estado]_[Frame]` → ejemplo: `SPR_Player_Walk_01.png`

### 11.3 Colores prohibidos en sprites
Los siguientes colores están reservados para efectos de shader y no deben usarse en sprites base (o se confundirán con el shader de eco):
```
Prohibido en sprites jugador/eco: #3AFFD4, #A855F7, #F97316, #22C55E, #EC4899
(estos son los colores de los ecos — el shader los generará dinámicamente)
```

### 11.4 Performance targets
```
Target FPS: 60fps estable en dispositivos mid-range (2020+)
Partículas máx: 150 simultáneas
Draw calls objetivo: <50 por frame (usar batching agresivo)
Sprites visibles: <80 simultáneos
```

---

## 12. Anti-patterns — Qué NO hacer

| ❌ Anti-pattern | ✅ Alternativa correcta |
|-----------------|------------------------|
| Usar el cyan #3AFFD4 en enemigos | Enemigos solo en rojo/naranja |
| Sprites con más de 4-5 colores | Reducir paleta, usar dithering si hace falta |
| Tiles con gradientes | Tiles planos con 2-3 valores máx |
| Post-processing siempre activo a full | Efectos sutiles, con off-switch para accesibilidad |
| Ecos que "hablan" o tienen reacciones propias | Los ecos repiten exactamente al jugador, nada más |
| Fondo colorido o con patterns complejos | Fondo oscuro y casi vacío |
| Sprites de diferente resolución mezclados | Todos en PPU=16, pixel size consistente |
| Animaciones de más de 8 frames | Máx 6-8 frames por estado |
| Efectos de partículas con shaders complejos | Solo sprites simples en partículas |

---

## 13. Referencias de Implementación Unity

### 13.1 Estructura de carpetas recomendada
```
Assets/
  _Art/
    Characters/
      Player/
        Sprites/        ← PNGs individuales
        Animations/     ← Unity .anim files
        Atlas/          ← SpriteAtlas
    World/
      Tiles/
        Sprites/
        TileAtlas/
      Backgrounds/
    VFX/
      Particles/
        Sprites/
      Materials/
    Enemies/
    UI/
      Sprites/
      Fonts/
  _Shaders/
    EchoShader.shader
    BulletTimePostProcess.shader
```

### 13.2 Shader de eco — estructura mínima
```hlsl
// EchoShader.shader (fragmento relevante)
Properties {
  _MainTex ("Sprite Texture", 2D) = "white" {}
  _EchoColor ("Echo Color", Color) = (0.23, 1.0, 0.83, 1)
  _Opacity ("Opacity", Range(0,1)) = 0.65
  _EmissionIntensity ("Emission", Range(0,1)) = 0.2
}
// En el fragment shader:
// Reemplazar tonos no-transparentes con _EchoColor
// Aplicar _Opacity sobre el alfa resultante
// Añadir _EmissionIntensity como bloom boost
```

---

## 14. Checklist de Dirección Artística

Antes de dar una tarea de arte por completada, verificar:

### Personaje
- [ ] Silueta legible a 16×32 sin colores
- [ ] Paleta máximo 4 colores + transparente
- [ ] Frames numerados correctamente (SPR_Player_Walk_01...)
- [ ] Animación de eco funciona con el shader (sprite en blanco puro)
- [ ] Walk cycle sin sliding (stride = velocidad en píxeles)
- [ ] Land frame tiene squash de 1-2px

### Entorno
- [ ] Tiles compatibles con Rule Tile (9-slice correcto)
- [ ] Rim lighting en borde superior de tiles de suelo
- [ ] Paleta ≤ 4 colores por tile
- [ ] No gradientes
- [ ] Fondo oscuro no compite con plano de juego

### VFX
- [ ] No supera 150 partículas simultáneas
- [ ] Duración máxima 1.5s
- [ ] Tiene opción de intensidad reducida (accesibilidad)
- [ ] Colores de ecos son SOLO los 5 definidos

### General
- [ ] PPU = 16 en todos los sprites
- [ ] Filtro = Point (sin aliasing)
- [ ] Compresión = ninguna o ETC2 sin pérdida visible
- [ ] Naming convention respetada

---

## 15. Autocrítica de Fase 6

**Fortalezas de este documento:**
- Paleta de colores completamente especificada y diferenciada (mundo vs UI)
- Sistema de eco-shaders con especificación técnica implementable
- VFX con durations, opcidades y frame counts exactos
- Anti-patterns explícitos evitan errores comunes
- Performance targets realistas para mobile mid-range

**Limitaciones y riesgos:**
1. Los sprites aún no existen — este documento es una guía para crearlos. La Fase 9 (Vertical Slice) validará si las decisiones funcionan en práctica.
2. El walk cycle de 6 frames puede ser insuficiente para el feel de fluidez móvil — probar con 8 frames en Vertical Slice.
3. El shader de eco necesita testing en dispositivos Android reales — los shaders HLSL se comportan diferente en GPUs móviles.
4. Limitación real: sin artista pixel art dedicado, estos sprites se generarán con herramientas asistidas. Asegurarse de que el output final respete las specs aquí definidas.

**Decisión deferida a Fase 9:**
- Tamaño final del personaje: puede ser necesario ir a 24×48 si 16×32 resulta demasiado pequeño en pantalla física.
- Количество variantes de tile: 256 puede ser excesivo; reducir a 64 si la memoria lo requiere.
