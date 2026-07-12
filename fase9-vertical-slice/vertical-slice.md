# PHASE — Fase 9: Vertical Slice
> El VS no es una demo pulida. Es un laboratorio de validación. El objetivo es responder una sola pregunta: **¿es PHASE divertido con sus mecánicas reales?** Todo lo que no ayude a responder esa pregunta está fuera de alcance.

---

## 0. Qué es (y qué NO es) el Vertical Slice de PHASE

### ESTÁ en el VS
- 1 sala de prueba diseñada a mano (no procedural)
- Movimiento del jugador completo: caminar, saltar, caer, colisiones
- Touch input en dispositivo real (no solo Editor)
- TimeManager + bullet-time funcional (Layer.Player = 0.1×, Layer.Echo = 1.0×)
- InputRecorder + 1 slot de eco activo
- EchoPlayer reproduciendo en bucle
- LoopTimer de 8 segundos
- 1 tipo de hazard (pinchos, estáticos)
- Muerte → reset de la sala (sin RunManager completo)
- Echo Shader funcional en el eco
- FMOD: pitch/filter durante bullet-time (parámetro BulletTimeAmount)
- Post-processing URP: Bloom activo en ecos, Vignette en bullet-time
- HUD mínimo: timer, slot de eco, HP (sin animaciones)

### NO está en el VS
- Procedural room generation
- Meta-progresión, upgrades, cristales
- Más de 1 eco simultáneo (se prueba 1 primero)
- Enemigos con IA (solo hazards estáticos)
- Save system
- Main Menu, Run End, Meta-Progression screens
- Arte final (se usa placeholder con forma correcta)
- Todas las animaciones (idle + walk + jump son suficientes para VS)
- Múltiples salas o transiciones
- Monetización, analytics

---

## 1. La Sala de Prueba del VS

La sala debe ser diseñada específicamente para forzar el uso del eco y bullet-time. No es un nivel de juego — es un banco de pruebas.

```
┌──────────────────────────────────────────────────────────────────────┐
│                                                                      │
│  [PLATAFORMA ALTA]   ──────────────          ──────────────          │
│                                                                      │
│              ^^^^^^^^                                                │
│              (pinchos)                                               │
│                                                                      │
│  ──────────────────────    ┌────┐    ────────────────────────────    │
│  SPAWN                     │GAP │                        META       │
│  [JUGADOR]                 └────┘                        [SALIDA]   │
│                                                                      │
│  Tile: suelo base + plataformas a 3 alturas distintas                │
└──────────────────────────────────────────────────────────────────────┘
```

**Zonas de validación por sector:**

| Zona | Qué valida | Diseño |
|------|-----------|--------|
| A — Inicio abierto | Feel básico del movimiento y salto | Suelo plano, 1 plataforma simple |
| B — Gap con pinchos | ¿Sirve el eco para ayudar sin planearlo? | Un gap que el eco puede cruzar y el jugador usa de referencia |
| C — Plataformas escalonadas | Bullet-time como herramienta de lectura | Plataformas con timing, BT ayuda a ver el momento correcto |
| D — Reto eco-requerido | El momento aha | Puzzle que solo se resuelve coordinando con el eco de la zona A |

**Tamaño de la sala:** 30 tiles de ancho × 12 tiles de alto (480×192 px en virtual res, 1920×768 en pantalla)

---

## 2. Orden de Implementación

Cada sprint es 1 sesión de trabajo. Nunca avanzar al siguiente sin que el actual funcione y se sienta bien.

### Sprint 1 — Jugador se mueve y salta (sin eco, sin BT)
**Objetivo:** Que el movimiento se sienta satisfactorio en dispositivo real.

```
Implementar:
  □ Proyecto Unity 2022 LTS creado con URP
  □ PixelPerfectCamera: referenceResolutionX=480, referenceResolutionY=270, PPU=16
  □ PlayerController.cs completo (Fase 8 como referencia)
  □ InputReader.cs con touch: drag horizontal = move, tap = jump
  □ Tilemap de la sala de prueba (con TileMap Collider 2D + Composite)
  □ Sprite placeholder del jugador: 16×32 px, color blanco puro (#E8EEF8)
  □ Animaciones mínimas: Idle (2f), Walk (4f), Jump (1f), Fall (1f)

Parámetros a tunear en este sprint:
  _moveSpeed:  probar 5 / 7 / 9 — elegir el más satisfactorio
  _jumpForce:  probar 14 / 16 / 18
  _gravity:    probar -28 / -35 / -42

Criterio de paso:
  ✅ Mover y saltar se siente fluido y responsivo en un dispositivo Android real
  ✅ No hay sliding ni input lag perceptible
  ✅ La cámara sigue al jugador sin jitter (PixelPerfect activo)
```

### Sprint 2 — TimeManager + Bullet-time
**Objetivo:** Que el bullet-time se sienta como poder, no como bug.

```
Implementar:
  □ TimeManager.cs con Layer.Player y Layer.World
  □ InputReader: hold stationary (0.15s threshold) → dispara OnBulletTimeChanged
  □ Vignette intensifica durante BT (Volume.profile lerp)
  □ Chromatic Aberration: 0.0 → 0.35 en BT
  □ Feedback visual simple: ring blanco alrededor del jugador en BT
  □ FMOD básico: 1 sonido ambiental de sala con low-pass al activar BT
    (si FMOD tarda, usar AudioMixer con LowPass filter como placeholder)

Parámetros a tunear:
  BT_HOLD_DURATION: probar 0.1s / 0.15s / 0.25s — cuándo se activa
  BT scale Player:  probar 0.05 / 0.1 / 0.15 — velocidad del jugador en BT
  BT_VELOCITY_THRESHOLD: probar 3px/s / 5px/s / 8px/s — sensibilidad del dedo quieto

Preguntas a responder:
  ❓ ¿El jugador entiende sin tutorial que quietar el dedo hace algo?
  ❓ ¿El efecto visual comunica "tiempo lento" de manera inmediata?
  ❓ ¿La activación involuntaria (mano temblorosa) es un problema real?

Criterio de paso:
  ✅ El bullet-time se activa de manera predecible y controlable
  ✅ El feedback visual es inmediato (< 1 frame de delay perceptible)
  ✅ Activación involuntaria < 10% de los intentos de movimiento normal
```

### Sprint 3 — InputRecorder + 1 Eco
**Objetivo:** Que ver a tu eco sea sorprendente y útil, no confuso o molesto.

```
Implementar:
  □ InputRecorder.cs (ring buffer 24fps, 10s máximo)
  □ EchoManager.cs simplificado: 1 solo slot, genera eco al final del primer loop
  □ EchoPlayer.cs: reproduce posiciones en bucle, usa Layer.Echo (1.0× siempre)
  □ Echo Shader: cargar el .shader de Fase 8, aplicar color Cyan #3AFFD4, Opacity 0.65
  □ LoopTimer: 8 segundos, al llegar a 0 → dispara creación de eco + reset timer
  □ HUD mínimo: barra de timer (rectangle que se vacía), indicador de slot

Parámetros a tunear:
  Loop duration:   probar 6s / 8s / 10s — ¿cuánto tiempo es el correcto?
  Echo opacity:    probar 0.5 / 0.65 / 0.8 — ¿visible pero no distractor?
  Echo trail:      ¿necesita trail de partículas para distinguirse del jugador?

Preguntas críticas a responder en este sprint:
  ❓ ¿El jugador entiende que eso soy "yo mismo del pasado"?
  ❓ ¿Es frustrante o satisfactorio cuando el eco hace algo que el jugador no esperaba?
  ❓ ¿El eco se siente como personaje o como ruido visual?
  ❓ ¿8 segundos es el tiempo correcto del loop o es muy corto/largo?

Criterio de paso:
  ✅ El jugador dice espontáneamente "ah, eso lo hice yo antes"
  ✅ La primera vez que el eco resuelve algo sin que el jugador lo planeara,
     el jugador reacciona con sorpresa positiva (no frustración)
  ✅ El jugador puede distinguir visualmente dónde está él y dónde está el eco
```

### Sprint 4 — Hazard + Muerte + Reset
**Objetivo:** Que morir no sea frustrante sino motivador para intentar de nuevo.

```
Implementar:
  □ Hazard_Spike.cs: trigger 2D que llama a PlayerStats.TakeDamage(999) → muerte inmediata
  □ PlayerStats.cs: HP simple (para VS: solo 1 vida, muere al tocar pinchos)
  □ Muerte: freeze 0.3s → flash rojo → fade out → reset sala (no run end screen todavía)
  □ Reset: reaparece el jugador en spawn, el eco se borra, timer reinicia
  □ VFX muerte: al menos el flash de pantalla (Screen overlay rojo, 0.3s)

Decisión sobre el eco al morir:
  OPCIÓN A: el eco desaparece con el jugador al morir
  OPCIÓN B: el eco sigue reproduciéndose mientras el jugador reaparece
  → Probar ambas. La que se sienta menos injusta = la correcta.

Criterio de paso:
  ✅ Morir y reintentar tarda < 2 segundos en total (sin pantallas intermedias)
  ✅ La causa de la muerte es siempre clara para el jugador
  ✅ El jugador intenta la sala al menos 5 veces sin sentirse frustrado en exceso
```

### Sprint 5 — Primera Sesión de Playtesting
**Objetivo:** Validación externa. Al menos 3 personas que no conocen PHASE.

```
Protocolo de playtesting:
  □ Tiempo de sesión: 15-20 minutos máximos
  □ Observar sin intervenir (no explicar controles ni mecánicas)
  □ Grabación de pantalla + cámara de manos (ver cómo tocan la pantalla)
  □ Think-aloud: pedir que digan en voz alta lo que piensan
  □ Formulario post-sesión (ver Sección 4)

Lo que se observa:
  - ¿Descubren el bullet-time solos? ¿En cuánto tiempo?
  - ¿Entienden para qué sirve el eco?
  - ¿Cuánto tiempo antes de usar BT + eco coordinadamente?
  - ¿Cuántas muertes antes de completar la sala? ¿Se rinden?

Criterio de paso:
  ✅ > 70% de los testers descubren el bullet-time sin instrucciones en < 3 minutos
  ✅ > 60% de los testers coordinan eco + BT al menos una vez en 15 minutos
  ✅ Ningún tester abandona por frustración en las primeras 5 muertes
  ✅ > 50% de los testers pide jugar "un poco más" al terminar los 20 minutos
```

---

## 3. La Decisión de Memory Decay

Esta decisión fue explícitamente diferida al Vertical Slice en la Fase 3 (Validación).

### ¿Qué es Memory Decay?
Los ecos "decaen" con el tiempo: sus posiciones se vuelven más imprecisas, su opacidad baja, eventualmente desaparecen aunque el jugador no haya muerto. Añade presión temporal y significado narrativo ("los recuerdos se desvanecen").

### Marco de decisión

**Probar Memory Decay en el Sprint 3:**
```
Variante A (Sin Decay):
  - El eco es estable e idéntico hasta que el jugador muere o se crea uno nuevo
  - Predecible, más fácil de usar, menos narrativa
  - Riesgo: el eco se vuelve una herramienta mecánica, no un personaje

Variante B (Con Decay visual, sin efecto en gameplay):
  - La opacidad del eco baja gradualmente (0.65 → 0.2 en 3 loops)
  - Puramente visual/narrativo — no afecta lo que el eco hace
  - Comunica "el pasado se desvanece" sin añadir frustración mecánica

Variante C (Con Decay mecánico):
  - El eco pierde frames de grabación: empieza a "saltear" posiciones
  - Gameplay impact real: el eco se vuelve menos confiable con el tiempo
  - Riesgo: puede ser frustrante si el jugador dependía del eco para un puzzle
```

**Criterio de decisión:**
- Si el playtesting muestra que el eco se siente como herramienta robótica → implementar Variante C
- Si el playtesting muestra que el eco ya se siente como personaje → Variante B (solo visual)
- Si Variante C genera frustración > 30% de sesiones → volver a Variante B
- **Default recomendado: Variante B** — menor riesgo, misma narrativa

**Decisión final registrar aquí:**
```
[ ] Variante A — Sin decay
[ ] Variante B — Solo visual
[ ] Variante C — Mecánico
Razón: _______________________________________________
```

---

## 4. Formulario de Playtesting

Para usar con cada tester al finalizar la sesión (respuestas 1-5).

```
PHASE — Formulario de Feedback (Vertical Slice)
Fecha: ___________  Tester #: ___

COMPRENSIÓN
1. ¿Entendiste qué era la figura que te seguía (el eco)?
   1 (nada) - 2 - 3 - 4 - 5 (completamente)
   Comentario: ___

2. ¿Entendiste para qué servía mantener el dedo quieto?
   1 (nada) - 2 - 3 - 4 - 5 (completamente)
   Comentario: ___

FEEL
3. ¿Cómo se sintió el movimiento del personaje?
   1 (torpe) - 2 - 3 - 4 - 5 (preciso y responsivo)

4. ¿El eco te ayudó o te distrajó/confundió?
   Ayudó mucho / Ayudó poco / Neutro / Confundió un poco / Confundió mucho

5. ¿Hubo algún momento en que el eco hizo algo y dijiste "oh!"?
   Sí / No — ¿Qué pasó?: ___

FRUSTRACIÓN
6. ¿En algún momento quisiste dejar de jugar?
   Sí / No — ¿Por qué?: ___

7. Cuántas veces moriste aproximadamente: ___
   ¿Te pareció justo? Sí / No — ¿Por qué?: ___

GENERAL
8. ¿Qué fue lo más interesante del juego? ___
9. ¿Qué fue lo más frustrante o confuso? ___
10. Si esto fuera un juego completo, ¿lo descargarías? Sí / Tal vez / No
```

---

## 5. Parámetros Ajustables — Tabla de Control

Todos estos valores se exponen como `[SerializeField]` en el Inspector de Unity. Nunca hardcodear hasta después del playtesting.

| Parámetro | Valor inicial | Rango de prueba | Sistema |
|-----------|--------------|-----------------|---------|
| `_moveSpeed` | 7.0 | 5.0 – 9.0 | PlayerController |
| `_jumpForce` | 16.0 | 12.0 – 20.0 | PlayerController |
| `_gravity` | -35.0 | -25.0 – -45.0 | PlayerController |
| `_coyoteTime` | 0.12 | 0.08 – 0.18 | PlayerController |
| `_jumpBuffer` | 0.1 | 0.05 – 0.2 | PlayerController |
| `BT_HOLD_DURATION` | 0.15 | 0.1 – 0.3 | InputReader |
| `BT_VELOCITY_THRESHOLD` | 5.0 | 3.0 – 10.0 | InputReader |
| `playerBulletTimeScale` | 0.1 | 0.05 – 0.2 | TimeManager |
| `bulletTimeSmoothSpeed` | 8.0 | 4.0 – 16.0 | TimeManager |
| `loopDuration` | 8.0 | 5.0 – 12.0 | LoopTimer |
| `echoOpacity` | 0.65 | 0.4 – 0.85 | EchoPlayer |
| `echoSampleRate` | 24 | 12 – 30 | InputRecorder |
| `vignetteNormal` | 0.25 | 0.15 – 0.35 | PostProcess |
| `vignetteBulletTime` | 0.55 | 0.4 – 0.7 | PostProcess |

---

## 6. Decisiones Pendientes del GDD — Responder en el VS

El GDD dejó estas sin resolver:

### 6.1 Duración máxima del Bullet-Time
**Pregunta:** ¿El BT puede mantenerse indefinidamente o tiene un límite?

```
Opción A — Infinito: el jugador puede mantenerlo todo el tiempo que quiera
  Riesgo: "cheese" — parar siempre para analizar la situación elimina la tensión

Opción B — Barra de energía: BT consume una barra, se recarga con movimiento
  Riesgo: añade un sistema más para gestionar, puede frustrar en el tutorial

Opción C — Limitado por tiempo pero con penalidad suave: BT no puede activarse
  dos veces seguidas sin un cooldown de 0.5s entre usos
  Balance: limita el "cheese" sin añadir UI/sistema extra

→ Probar A primero en Sprint 2. Si los testers "abusan" en > 60% del tiempo
  de juego sin presión → migrar a C. Opción B solo si C no es suficiente.
```

**Decisión final:**
```
[ ] A — Infinito
[ ] B — Barra de energía
[ ] C — Cooldown suave
Cooldown valor: ___s   Razón: ___
```

### 6.2 ¿Qué pasa cuando el eco te mata?
**Pregunta:** Si el eco pasa por donde estás y "choca" contigo, ¿qué pasa?

```
Los ecos son cinemáticos — no tienen física ni colisión real. Pero narrativamente,
¿pueden ser peligrosos?

Opción A — El eco no interactúa con el jugador (pasan uno por el otro)
  Más simple, menos traicionero, mejor para el tutorial

Opción B — El eco puede "empujar" ligeramente al jugador
  Físicamente imposible (son cinemáticos) — habría que añadir trigger y impulso
  Riesgo: el jugador siente que su propio pasado lo traiciona, frustración

Opción C — El eco es un obstáculo visual pero sin gameplay impact

→ Recomendación: Opción A para el VS. Si el playtesting muestra que los
  testers esperan interacción eco-jugador → evaluar en Fase 10.
```

**Decisión final:**
```
[ ] A — Sin interacción
[ ] B — Empuje leve
[ ] C — Obstáculo visual
Razón: ___
```

### 6.3 Loop Timer: ¿Qué comunica visualmente?
**Pregunta:** ¿El timer debe verse como tiempo o como "longitud del loop que se está grabando"?

```
Framing A — "Tiempo de vida del run": cuenta hacia atrás, presión de muerte
  Problema: es mentira. El jugador no muere al llegar a 0, solo se crea un eco.

Framing B — "Ciclo de grabación": indicador circular que muestra cuánto del loop
  actual se ha grabado. Al llegar a 360° → se crea el eco y empieza de nuevo.
  Ventaja: comunica exactamente lo que pasa, sin expectativa incorrecta.

→ Recomendación: Framing B. Renombrar el HUD de "LOOP: 8s" a un ring circular
  que se llena. Más honesto con la mecánica.
```

**Decisión final:**
```
[ ] A — Countdown de tiempo
[ ] B — Ring circular de grabación
Razón: ___
```

---

## 7. Criterios de Go / No-Go para Fase 10

Al finalizar el VS y el playtesting, evaluar cada criterio. Si alguno es NO-GO, iterar en el VS antes de continuar.

### GO — Continuar a Fase 10

| # | Criterio | Métrica | Resultado |
|---|---------|---------|-----------|
| 1 | El movimiento se siente bien | > 4.0 promedio en pregunta 3 del formulario | |
| 2 | El eco se entiende | > 3.5 promedio en pregunta 1 | |
| 3 | El BT se descubre solo | > 70% lo descubre en < 3 min sin instrucciones | |
| 4 | Momento "aha" ocurre | > 60% reportan un momento de sorpresa positiva con el eco (pregunta 5) | |
| 5 | Retención mínima | > 50% pide seguir jugando al terminar los 20 minutos | |
| 6 | No hay frustración crítica | 0 testers abandonan por frustración en las primeras 5 muertes | |
| 7 | Performance | 60fps estables en dispositivo Android mid-range (Snapdragon 665 o equivalente) | |
| 8 | Descarga potencial | > 50% responde "Sí" a la pregunta 10 | |

### NO-GO — Iterar el VS antes de continuar

| Problema | Acción |
|---------|--------|
| Eco no se entiende | Añadir 1 línea de tutorial al inicio (no pantalla — texto en el mundo) |
| BT se activa involuntariamente mucho | Subir BT_HOLD_DURATION a 0.25s y BT_VELOCITY_THRESHOLD a 8px/s |
| El juego se siente vacío sin eco | La sala de prueba no fuerza el uso del eco → rediseñar zona D |
| > 50% abandona antes de completar la sala | La sala es demasiado difícil → añadir un checkpoint en la mitad |
| Framerate < 60fps | Revisar la cámara PixelPerfect + shadow settings + particle count |
| El eco "arruina" un intento justo | Revisar framing del eco (¿necesita decay o aviso visual de posición?) |

---

## 8. Setup del Proyecto Unity — Checklist de Configuración Inicial

```
□ Unity Hub → New Project → 2D (URP) Template → nombre: "PHASE"
□ Package Manager instalar:
    □ Universal RP (ya incluido en template)
    □ Input System 1.6+ → Edit → Project Settings → Player → Active Input = Both (durante transición)
    □ 2D PixelPerfect 5.x
    □ FMOD for Unity (descargar desde fmod.com/download, instalar el .unitypackage)
    □ GameAnalytics SDK (desde Package Manager → Add from URL: com.gameanalytics.sdk)

□ Project Settings → Player:
    □ Company Name: [TuNombre]Studio
    □ Product Name: PHASE
    □ Default Orientation: Landscape Left
    □ Allow Orientation: Landscape Left + Landscape Right

□ Project Settings → Physics 2D:
    □ Gravity Y: -35 (ajustar en Sprint 1)
    □ Simulation Mode: Update
    □ Layers crear:
        □ Ground (layer 6)
        □ Player (layer 7)
        □ Echo (layer 8)
        □ Hazard (layer 9)
        □ Platform (layer 10)
    □ Layer collision matrix: Echo ignora Ground, Player, Hazard

□ URP Asset:
    □ HDR: ON
    □ MSAA: Disabled
    □ Post Processing: ON

□ PixelPerfectCamera en Main Camera:
    □ Asset Pixels Per Unit: 16
    □ Reference Resolution: 480 × 270
    □ Upscale Render Texture: ON
    □ Crop Frame: Both
    □ Filter: Point (no filtro)

□ Global Volume (PostProcess):
    □ Bloom: threshold=0.8, intensity=1.2
    □ Vignette: intensity=0.25 (se sube por código en BT)
    □ Chromatic Aberration: intensity=0.0 (se sube por código en BT)
    □ Color Grading: contrast=105%, blue lift=+0.02

□ Sorting Layers (en ese orden):
    □ Background_Far, Background_Mid, Background_Near
    □ Terrain
    □ Hazard
    □ Echo_3, Echo_2, Echo_1 (del más viejo al más reciente)
    □ Player
    □ VFX
    □ UI_World, UI_HUD, UI_Modal

□ Tags crear: Player, Echo, Hazard, Ground, Pickup

□ Git + Git LFS:
    □ git init en la carpeta del proyecto
    □ Copiar .gitignore de Unity (github.com/github/gitignore)
    □ git lfs track "*.png" "*.wav" "*.bank" "*.psd"
    □ Primer commit: "Initial Unity project setup"
```

---

## 9. Cronograma Estimado del VS

El VS es trabajo real en Unity. Las horas son estimaciones para un dev solo.

| Sprint | Contenido | Horas estimadas |
|--------|-----------|-----------------|
| 0 | Setup Unity, packages, layers, URP config | 2-3h |
| 1 | Movimiento + jump + tilemap sala | 4-6h |
| 2 | TimeManager + bullet-time + feedback visual | 3-5h |
| 3 | InputRecorder + EchoManager + EchoPlayer + shader | 6-8h |
| 4 | Hazards + muerte + reset | 2-3h |
| 5 | Playtesting + análisis + ajuste de parámetros | 4-6h |
| **Total** | | **21-31 horas** (~3-4 días de trabajo intenso) |

---

## 10. Autocrítica

**¿Qué puede salir mal en el VS?**

- **El eco no genera el momento "aha"**: Si la sala de prueba no está bien diseñada para forzar la coordinación eco-jugador, el eco solo se ve como decoración. Solución: zona D debe ser imposible sin coordinación con el eco de la zona A — si el tester puede completar la sala ignorando el eco, la sala está mal diseñada.

- **El bullet-time no se siente como superpoder**: Si el efecto visual no es suficientemente dramático, el jugador no lo percibe como mecánica intencional. Solución: subir la vignette y el chromatic aberration. El audio (low-pass de FMOD) es crítico aquí — sin audio, el BT se siente débil.

- **Los ecos son confusos en lugar de fascinantes**: Si el jugador no entiende que el eco es "él mismo del pasado", el sistema falla. Solución: considerar una línea de texto en el mundo la primera vez que aparece el eco: "Tu primer eco." No más. No pantalla de tutorial.

- **Performance en Android**: Unity con URP + PixelPerfect puede tener overhead inesperado en dispositivos low-end. Si el VS no llega a 60fps en un Snapdragon 665, hay que revisar el render path antes de Fase 10.

- **Touch controls no se sienten bien**: La activación del bullet-time por "dedo quieto" es una mecánica no probada en producción real. Si genera activación involuntaria frecuente, puede arruinar la experiencia completa. El VS tiene que responder esto.

---

*El VS no termina hasta que exista un registro escrito de las decisiones tomadas (Sección 3 y 6 completadas) y los criterios de Go/No-Go evaluados (Sección 7). Un VS sin playtesting documentado no cuenta como completado.*

*Próxima fase: Fase 10 — Desarrollo completo (solo si todos los Go/No-Go son GO)*
