# GDD PHASE v1.1 — Parte 2: Secciones 11–19

**Estado:** Draft v1.1 | Fecha: 2026-06-30
**Autores:** Lead Game Designer + Software Architect
**Documento previo:** GDD-PHASE-parte1.md
**Próxima revisión:** Vertical Slice (Fase 9)

---

## ÍNDICE PARTE 2

- [11. HUD e Interfaz](#11-hud-e-interfaz)
- [12. Audio](#12-audio)
- [13. Arte y Animaciones](#13-arte-y-animaciones)
- [14. Accesibilidad](#14-accesibilidad)
- [15. Guardado y Datos](#15-guardado-y-datos)
- [16. LiveOps y Contenido Futuro](#16-liveops-y-contenido-futuro)
- [17. Arquitectura Técnica](#17-arquitectura-técnica)
- [18. QA y Métricas](#18-qa-y-métricas)
- [19. Autocrítica del GDD](#19-autocrítica-del-gdd)

---

## 11. HUD E INTERFAZ

### 11.1 Filosofía de HUD

PHASE es un juego de coordinación cognitiva: el jugador debe leer el estado de hasta 5 ecos simultáneamente mientras controla al personaje en tiempo real. El HUD sigue el principio **"información mínima, legible al instante"**: nada que requiera más de 150ms de atención sostenida, todo codificado en color y forma además de texto para permitir lectura periférica.

**Regla cardinal:** El mundo es la UI. Los ecos proyectan información de estado en el entorno mediante efectos visuales (ver sección 13). El HUD superpuesto solo muestra lo que el mundo no puede expresar por sí solo.

---

### 11.2 Pantalla de Gameplay — Anatomía Completa

```
┌─────────────────────────────────────────────────────────┐
│ [SAFE ZONE TOP 44px — notch / dynamic island clearance] │
│  ┌──────────┐                          ┌──────────────┐ │
│  │ HP ████░ │                          │ 01:47  SALA  │ │
│  │ ████████ │                          │ 3 / 8  [i]  │ │
│  └──────────┘                          └──────────────┘ │
│                                                         │
│                  ÁREA DE JUEGO                          │
│              (touch pass-through)                       │
│                                                         │
│                                                         │
│  ┌─────────────────────────────────────────────────┐   │
│  │  ECO STRIP — barra de ecos activos               │   │
│  │  [●ECO1][●ECO2][●ECO3][  ][  ]   [BT RING]     │   │
│  └─────────────────────────────────────────────────┘   │
│ [SAFE ZONE BOTTOM 34px — gesture bar / home indicator] │
└─────────────────────────────────────────────────────────┘
```

#### 11.2.1 Zona Superior Izquierda — Salud del Jugador
- **Elemento:** Barra de HP + orbe de vida
- **Dimensiones:** 120×32dp, con orbe circular de 32×32dp a la izquierda
- **Color de relleno:** `#4FFFCE` (verde-cian fase) al >50%, `#FFB840` al 25–50%, `#FF4060` al <25%
- **Comportamiento:** Pulso lento (escala 1.0→1.05→1.0, 2Hz) cuando HP <25%. Sin parpadeo (riesgo epiléptico)
- **Texto:** HP numérico solo visible al tocar la barra (tap-to-expand); normalmente solo barra visual
- **Tap target:** 44×44pt mínimo incluyendo padding invisible

#### 11.2.2 Zona Superior Derecha — Timer y Progresión de Sala
- **Elemento:** Contador de tiempo de run + sala actual
- **Dimensiones:** bloque de 100×40dp
- **Tipografía:** Monospace, 18sp, `tabular-nums` para que los dígitos no salten
- **Datos mostrados:** `MM:SS` tiempo total de run activo | `N / 8` número de sala dentro del run
- **Botón info [i]:** 44×44pt; abre overlay de resumen de run sin pausar (overlay semi-transparente, el juego sigue corriendo). Justificación: pausar sería penalizante en un juego de gestión de ecos; el jugador hábil puede consultar el resumen en bullet-time

#### 11.2.3 Eco Strip — Barra Inferior Central
- **Posición:** Anclada a 34dp sobre el safe zone inferior
- **Ancho:** 70% del ancho de pantalla, centrada
- **Slots:** 5 slots circulares de 44×44dp cada uno, separados por 8dp de gap
- **Estado de slot vacío:** Círculo outline `#2A3040`, opacidad 40%
- **Estado de slot activo:** Círculo relleno con el color identificador del eco (ver §11.3), borde blanco 2px cuando el eco está en su trigger point activo
- **Animación de slot:** Al crearse un eco nuevo, el slot escala de 0→1 con ease-out en 200ms, y emite partículas de color (ver §13.5)
- **Accesibilidad daltónica:** Cada slot tiene además un ícono de forma única (ver §14.2)

#### 11.2.4 Bullet-Time Ring — Indicador de Bullet-Time
- **Posición:** Extremo derecho del Eco Strip, 48×48dp
- **Visual:** Anillo circular. El interior se rellena de color `#C8B8FF` (violeta temporal) de vacío→lleno mientras el jugador mantiene el dedo quieto
- **Tiempo de carga:** 0.3s desde quietar el dedo hasta activación de bullet-time (ventana de tolerancia de 0.08s para micromovimientos táctiles)
- **En bullet-time activo:** El anillo pulsa con un brillo exterior, frec. 1.5Hz, amplitud modesta. Las letras del HUD adquieren un leve halo violeta
- **Al soltar:** El anillo se vacía en 0.15s con una onda de expansión

---

### 11.3 Visualización de Ecos Activos

Cada eco recibe una identidad visual consistente en todo momento.

| Slot | Color Identificador | Hex       | Forma ícono (daltónico) | Nombre interno |
|------|---------------------|-----------|-------------------------|----------------|
| 1    | Cian fase           | `#4FFFCE` | Círculo                 | ECHO_ALPHA     |
| 2    | Magenta temporal    | `#FF5FCB` | Triángulo               | ECHO_BETA      |
| 3    | Ámbar caliente      | `#FFB840` | Cuadrado                | ECHO_GAMMA     |
| 4    | Azul espectral      | `#5BB8FF` | Rombo                   | ECHO_DELTA     |
| 5    | Verde fantasma      | `#A8FF7A` | Estrella de 5 puntas    | ECHO_EPSILON   |

**Diferenciación visual jugador vs ecos en el mundo:**
- Jugador: opacidad 100%, contorno blanco sólido 2px, sombra de contacto con el suelo
- Eco activo: opacidad 65%, contorno del color del slot 1.5px, sin sombra de contacto, trail de 0.1s de longitud detrás de su movimiento
- Eco en trigger point resuelto: opacidad 40%, animación idle "flotando" (±3px vertical, período 3s), partículas de color lento
- Eco expirado/completo: fade a opacidad 0 en 0.5s con destello del color identificador

**Legibilidad con múltiples ecos simultáneos:**
- Los colores se eligieron para separación perceptual: ningún par adyacente comparte tono similar (diferencia de Hue mínima 30° en el espacio HSL)
- En escenas muy cargadas, los ecos no activos bajan a 45% opacidad automáticamente cuando el jugador está a menos de 60px de ellos (fade rápido 100ms para evitar confusión de colisión)

---

### 11.4 Indicador de Bullet-Time — Feedback Global

Cuando bullet-time se activa, el cambio debe ser inconfundible:

1. **Aberración cromática:** Los bordes de pantalla reciben un split RGB de ±3px en X durante el bullet-time (ver §13.4 para especificación técnica de post-process)
2. **Desaturación del entorno:** La capa de entorno baja a 60% de saturación (shader multiplicativo sobre la capa de tiles)
3. **Trail de ecos reforzado:** El trail de cada eco se extiende a 0.3s de longitud y aumenta su opacidad al 80%
4. **Sonido:** ver §12.3

---

### 11.5 Flujo Completo de Pantallas

```
[Splash/Carga]
      │
      ▼
[Pantalla de Inicio]
      │
      ├──► [Opciones / Config] ──► regresa a Inicio
      ├──► [Logros]            ──► regresa a Inicio
      └──► [JUGAR]
                │
                ▼
         [Selección de Run]
         (semilla visible, dificultad, racha actual)
                │
                ▼
         [Tutorial — solo primera vez o si se activa en Config]
         (no salteable en la primera vez; sí salteable en repeticiones)
                │
                ▼
         ┌──────────────────────────────────────┐
         │           GAMEPLAY LOOP              │
         │  Sala 1 ──► Sala N ──► Sala Boss     │
         │  (pool de 50+ salas pre-diseñadas)   │
         └──────────────────────────────────────┘
                │
                ├──► [Muerte / Fallo] ──────────────────────┐
                │                                            │
                └──► [Fin de Run — Boss derrotado]          │
                                │                            │
                                ▼                            ▼
                         [Pantalla de Resultados]    [Pantalla de Resultados]
                         (puntuación, ecos usados,   (xp ganado parcial,
                          tiempo, multiplicador)       mensaje motivacional)
                                │
                                ▼
                         [Tienda de Run]
                         (mejoras permanentes de meta-progresión
                          compradas con Fragmentos de Eco,
                          moneda ganada en runs)
                                │
                                ▼
                         [Meta-Progresión / Árbol de Habilidades]
                         (accesible también desde Inicio)
                                │
                                └──► regresa a [Selección de Run]
```

#### 11.5.1 Pantalla de Inicio
- **Elementos:** Logo PHASE (animado, ver §13.3), tagline, botón JUGAR (CTA primario, 240×56dp), botones secundarios alineados en fila: Opciones | Logros | Tienda Permanente
- **Fondo:** Animación ambiental procedural de ecos circulando (baja CPU; 3 ecos pre-grabados en loop, renderizados a 30fps en background layer)
- **Sin rating de sesión en este punto:** No se pide valoración hasta el día 3 post-instalación Y después de al menos 5 runs completados

#### 11.5.2 Tutorial
- **Estructura:** 7 micro-salas lineales, cada una enseña un concepto aislado
  1. Movimiento básico (arrastra)
  2. Crear primer eco (completa una acción)
  3. El eco repite tu acción
  4. Bullet-time (quieta el dedo)
  5. Coordinar presente + 1 eco
  6. Coordinar presente + 2 ecos
  7. Sala de validación libre (primer puzzle real)
- **Progresión:** Solo avanza cuando el jugador ejecuta la acción correcta, sin timer. No hay texto largo; tooltips máx. 2 líneas, 16sp
- **Skip:** Disponible desde la 2ª vez. Si se salta, se muestra resumen de controles de 1 pantalla

#### 11.5.3 Pantalla de Resultados
- **Datos primarios (grandes):** Tiempo de run, nº salas completadas, multiplicador de puntuación
- **Datos secundarios (medianos):** Ecos creados, resoluciones perfectas, racha
- **Datos terciarios (pequeños):** XP ganado, Fragmentos de Eco ganados
- **CTA:** "Ver árbol" (meta-progresión) | "Jugar de nuevo" | "Tienda"

#### 11.5.4 Tienda
- Moneda: Fragmentos de Eco (no moneda real; sin compras de ventaja)
- Grid de mejoras: 2 columnas, cards de 160×120dp, ícono + nombre + coste + botón comprar
- Mejoras: permanentes (desbloqueables una vez) y consumibles de run (comprados antes de cada run con fragmentos)

#### 11.5.5 Meta-Progresión
- Árbol de nodos conectados (no grid). Nodos visibles aunque no desbloqueados; estado: bloqueado (outline gris), disponible (outline del color del tipo), comprado (relleno)
- Categorías de nodo: Velocidad de eco, Capacidad de ecos (máx 3 al inicio → 5), Duración de bullet-time, Amplificadores de puntuación
- El árbol se guarda en save local + nube (ver §15)

---

### 11.6 UX — Gestos, Tap Targets y Safe Zones

| Gesto              | Acción                        | Zona activa                |
|--------------------|-------------------------------|----------------------------|
| Tap + drag         | Mover personaje               | 100% del área de juego     |
| Quietar dedo       | Activar bullet-time           | Cualquier posición         |
| Tap en Eco Strip   | Ver detalle del eco           | Slot 44×44dp               |
| Swipe down rápido  | Cancelar eco más reciente     | Zona inferior 20%          |
| Long press (0.5s)  | Menú de pausa                 | Zona superior derecha 20%  |

**Tap targets mínimos:** 44×44pt en todos los elementos interactivos, sin excepción. Elementos que visualmente son más pequeños tienen padding táctil invisible hasta alcanzar el mínimo.

**Safe zones:**
- Top: 44dp siempre reservados (cubre notch, Dynamic Island, cámara perforada)
- Bottom: 34dp reservados (cubre la barra de gestos de Android/iOS)
- Lados: 16dp de margen mínimo para evitar activación accidental en pantallas curvadas

**Soporte de una sola mano:**
- Todos los controles críticos son accesibles con el pulgar derecho o izquierdo en un teléfono de 6.1" (90th percentile de tamaño de pantalla actual)
- El menú de pausa se puede activar con long press en cualquier zona cuando el juego detecta que solo hay 1 punto de contacto activo durante >1.5s

---

## 12. AUDIO

### 12.1 Filosofía de Audio

El audio en PHASE no es ambiental ni decorativo: es **información temporal**. Cada capa de sonido codifica el estado del sistema de ecos. Un jugador con auriculares debe ser capaz de saber, sin mirar la pantalla, cuántos ecos están activos, si el bullet-time está activo, y si una resolución de eco ocurrió.

Esta filosofía implica:
1. **Capas de audio sincronizadas al sistema de tiempo:** La música reacciona al estado de bullet-time en tiempo real (pitch shift, no fade)
2. **Lenguaje sonoro consistente:** El mismo motivo melódico representa al mismo eco en toda la sesión de juego
3. **Silencio como dato:** El silencio relativo del bullet-time es intencional; no es ausencia de esfuerzo sino información de estado

---

### 12.2 Música — Estructura y Comportamiento

**Estilo:** Electronic minimalista con influencia de ambient industrial. Referencias de sensación (no de copia): la precisión rítmica y tensión controlada del ambient electrónico de producción moderna donde el ritmo es esquelético y las texturas son el protagonismo. El objetivo es tensión sin agresión: música que ayuda a pensar, no que interrumpe el pensamiento.

**Implementación técnica:** Sistema de capas adaptativas con stems individuales gestionados por FMOD Studio.

#### 12.2.1 Estructura por Zona

Cada zona tiene un set de stems que el motor de audio mezcla en tiempo real:

| Stem      | Descripción                              | Siempre activo |
|-----------|------------------------------------------|----------------|
| BASE      | Drones de bajo y pad armónico (16 bars)  | Sí             |
| RHYTHM    | Percusión electrónica cuantizada         | No — entra al crear el 1er eco |
| ECO_N     | Motivo melódico corto asignado al eco N  | No — entra al crear eco N |
| TENSION   | Layer de high-freq harmonics             | No — entra al 60% de la sala |
| BOSS      | Reemplaza todo el set; es su propia pista| Solo en sala boss |

**Total de stems en memoria simultánea:** máximo 8 (BASE + RHYTHM + 5×ECO + TENSION).
**Presupuesto de audio RAM:** 24MB máximo para stems sin comprimir en buffer.

#### 12.2.2 Bullet-Time y Música

Al activar bullet-time:
- **Pitch shift en tiempo real:** Todos los stems bajan -2 semitones en 0.15s con interpolación suave (sin corte)
- **Filtro low-pass:** Se aplica un low-pass de 4kHz sobre RHYTHM y TENSION (el jugador percibe el mundo "espeso" y "profundo")
- **Los motivos de eco (ECO_N stems) NO se filtran:** Siguen sonando normales, reforzando que los ecos corren a velocidad normal. Esta separación perceptual es central a la mecánica
- **Al desactivar bullet-time:** Pitch shift de vuelta en 0.2s, low-pass se abre en 0.25s

**Implementación:** FMOD Pitch Shifter DSP en cadena de bus master; los stems de eco en bus separado sin la cadena de efectos de bullet-time.

---

### 12.3 Efectos de Sonido Críticos

Todos los SFX tienen variación de pitch de ±5% random para evitar repetición perceptible.

| Evento                    | Descripción del sonido                                       | Duración | Prioridad |
|---------------------------|--------------------------------------------------------------|----------|-----------|
| Creación de eco           | Click metálico corto + tono ascendente del color del eco     | 0.3s     | Alta      |
| Bullet-time ON            | "Whoosh" de desaceleración + subida de reverb room size      | 0.4s     | Alta      |
| Bullet-time OFF           | "Snap" de tensión liberada + reducción de reverb             | 0.2s     | Alta      |
| Eco resolvió trigger      | Tono harmónico del motivo del eco + campanilla breve         | 0.5s     | Alta      |
| Eco expiró sin resolver   | Tono descendente disonante + glitch de audio corto           | 0.3s     | Media     |
| Daño al jugador           | Impact corto + distorsión de 0.1s                            | 0.2s     | Alta      |
| Muerte del jugador        | Silencio de 0.5s → drone de reverberación larga (3s)         | 3.5s     | Alta      |
| Boss aparece              | Silencio de 0.3s → stem BOSS con ataque duro                 | 1.0s     | Alta      |
| Boss derrotado            | Descarga de energía + silencio → fade in de música de victoria| 2.0s    | Alta      |
| Sala resuelta (todos ecos)| Chord mayor del tono de la zona + sweep de partículas        | 0.8s     | Alta      |
| UI: Tap en botón          | Click suave y corto, 440Hz, -24dB                            | 0.05s    | Baja      |
| UI: Compra en tienda      | Tono de cristal con decay                                    | 0.4s     | Media     |

**Pool de variaciones:** Cada SFX de alta prioridad tiene 3 variaciones de pitch/timbre que el motor rota para evitar same-sound fatigue.

---

### 12.4 Accesibilidad — Jugabilidad Completa sin Sonido

**Principio:** El audio mejora la experiencia pero no es su canal de información principal. Cada evento sonoro tiene un equivalente visual que comunica la misma información.

| Evento sonoro             | Equivalente visual (detalle en §13.5)                     |
|---------------------------|-----------------------------------------------------------|
| Creación de eco           | Slot del Eco Strip escala 0→1 + destellos de color        |
| Bullet-time ON            | Aberración cromática en bordes + desaturación de entorno  |
| Bullet-time OFF           | Flash inverso de color + restauración de saturación       |
| Eco resolvió trigger      | Destello blanco en el sprite del eco + texto flotante "!" |
| Eco expiró sin resolver   | Slot del Eco Strip parpadea en X 3 veces → desaparece     |
| Daño al jugador           | Flash rojo breve sobre pantalla completa (2 frames)       |
| Muerte del jugador        | Fade a negro lento (1.5s) con círculo de onda expansiva   |
| Boss aparece              | Corte a negro 0.3s + aparición con sacudida de cámara     |
| Boss derrotado            | Explosión de partículas multicolor + texto BOSS DERROTADO |

**Configuración explícita:** En Options existe el toggle "Modo sin sonido" que activa subtítulos de eventos en esquina inferior izquierda (14sp, fondo semi-transparente) para usuarios con dificultades auditivas.

---

## 13. ARTE Y ANIMACIONES

### 13.1 Estilo Artístico — Especificación para Concept Artist

**Estilo base:** Pixel art de alta densidad a resolución de referencia 480×270 (escala 4× para 1920×1080, 2× para móvil 960×540). Escala: sprites de personaje 16×16px base, animados a 8–12fps.

**El "look" específico de PHASE:** Pixel art con iluminación dinámica de colores complementarios. No es el pixel art de 8-bit retro; es el pixel art de demoscene moderno — alta precisión en dithering, luz volumétrica fakeada con gradientes en los tiles, efectos de brillo (bloom) selectivos en los elementos de interacción. La estética objetivo es: frío, preciso, con momentos de color explosivo cuando los ecos entran en escena. Arquitectura espacial austera (acero, vidrio, vacío) que contrasta con los colores vivos de los ecos para que cada eco sea el evento visual más importante en pantalla.

**Lo que NO es:** Sin pixel art de baja resolución tipo NES. Sin estética de RPG de 16-bit (sin tileset de césped y piedra medieval). Sin minimalismo geométrico sin textura. Sin paleta cálida de ningún tipo.

---

### 13.2 Paleta de Colores — Documento de Referencia

#### Paleta del Jugador
| Rol               | Hex       | Descripción de uso                        |
|-------------------|-----------|-------------------------------------------|
| Cuerpo principal  | `#D8E4F0` | Azul-blanco frío, el jugador es "presente"|
| Contorno          | `#FFFFFF` | Blanco puro, siempre visible sobre fondo  |
| Sombra/interior   | `#8AA0BC` | Volumen del sprite                        |
| Acento de acción  | `#4FFFCE` | Destello al crear eco, punto de contacto  |

#### Paleta de Ecos (ver §11.3 para tabla de colores por slot)
- Todos los ecos tienen: color de cuerpo (ver tabla §11.3), contorno del mismo color a 80% saturación +10% valor, sombra interior a 50% saturación
- **Nunca** blanco puro como contorno de eco: reservado al jugador

#### Paleta del Entorno
| Rol                   | Hex       |
|-----------------------|-----------|
| Fondo lejano          | `#0A0D14` |
| Tiles de piso (base)  | `#1A1F2E` |
| Tiles de pared        | `#232A3D` |
| Borde de tile         | `#2F3854` |
| Elemento interactivo  | `#3A4A6B` |
| Peligro               | `#8B2030` |
| Zona de resolución    | `#1F3A2A` |

**Coherencia clave:** El entorno usa exclusivamente azules oscuros y grises fríos para que cualquier color de eco sea inmediatamente legible en contraste. El único rojo es para peligro, nunca para ecos.

---

### 13.3 Animaciones Obligatorias del Personaje (Jugador)

Todos los estados deben estar animados. Sin estados estáticos de un frame.

| Animación          | Frames | FPS | Loop | Notas                                    |
|--------------------|--------|-----|------|------------------------------------------|
| Idle               | 4      | 4   | Sí   | Respiración sutil, parpadeo ocasional    |
| Run (derecha/izq)  | 6      | 10  | Sí   | Espejear para izquierda (1 sprite set)   |
| Salto ascendente   | 3      | 8   | No   | Anticipación 1fr → acción 2fr            |
| Caída              | 2      | 6   | Sí   | Loop en caída libre                      |
| Aterrizaje         | 3      | 12  | No   | Squash en frame 1, recuperación rápida   |
| Creación de eco    | 5      | 12  | No   | "Copia" que se desprende del cuerpo      |
| Daño recibido      | 4      | 12  | No   | Knockback visual                         |
| Muerte             | 7      | 10  | No   | Colapso + disolución en partículas       |
| Bullet-time idle   | 4      | 3   | Sí   | Versión ralentizada del idle normal      |
| Bullet-time run    | 6      | 3   | Sí   | Versión ralentizada del run              |
| Interactuar        | 3      | 10  | No   | Alcanzar un objeto / activar mecanismo   |

**Animaciones del eco:** Los ecos usan el mismo spritesheet del jugador con shader de color (tint al color del slot) + reducción de opacidad. No se necesita un spritesheet separado por eco. Ahorro de memoria: 5× sprites reducido a 1× con variación por shader.

---

### 13.4 Efecto Visual de Bullet-Time — Especificación Técnica

Al activar bullet-time (dedo quieto durante ≥0.3s):

**1. Aberración cromática (Chromatic Aberration):**
- Post-process shader sobre render texture de pantalla completa
- Separación: canal R desplazado +3px en X, canal B desplazado -3px en X, canal G sin desplazamiento
- Solo en los bordes: la separación se atenúa con una máscara radial (0% en el centro, 100% a 15% de distancia del borde)
- Intensidad: aumenta gradualmente en los 0.15s de carga del bullet-time antes de activarse

**2. Desaturación del entorno:**
- Shader de desaturación aplicado a la capa de tiles del entorno (background layer exclusivamente)
- Saturation multiplier: 0.55 (45% de reducción)
- Transición: 0.12s con curva ease-in-out
- Los ecos y el jugador NO se desaturan; esto es intencional para mantener la legibilidad táctica

**3. Viñeta de profundidad:**
- Viñeta oscura (alpha 0.25) en los bordes durante bullet-time
- Refuerza la sensación de "foco" y contrae perceptualmente el campo de visión hacia el centro de acción

**4. Partículas de ambiente:**
- Durante bullet-time, 8 partículas de color violeta (`#C8B8FF`) flotan desde la posición del jugador en direcciones aleatorias, velocidad 10px/s, fade-out en 1.5s
- Máximo 40 partículas de bullet-time en pantalla simultáneamente; se reciclan en pool

---

### 13.5 Sistema de Partículas — Momentos Clave

**Regla técnica:** Todas las partículas son sprites de 4×4px o 2×2px para mantener draw calls bajos. Pool de 200 partículas compartidas globalmente.

| Momento                   | Tipo       | Color              | Cantidad | Duración | Descripción de movimiento              |
|---------------------------|------------|--------------------|----------|----------|----------------------------------------|
| Creación de eco           | Burst      | Color del eco      | 12–16    | 0.6s     | Explosión radial desde posición del jugador, fade-out |
| Bullet-time activo        | Continuo   | `#C8B8FF`          | 2/seg    | 1.5s     | Float lento hacia arriba               |
| Eco resuelve trigger      | Burst      | Blanco + color eco | 20       | 0.8s     | Explosión + gravedad hacia abajo       |
| Eco expira sin resolver   | Burst      | Rojo + color eco   | 8        | 0.4s     | Implosión hacia el centro del slot     |
| Boss aparece              | Burst global| `#FF4060`         | 60       | 1.2s     | Desde el boss hacia afuera (radial)    |
| Boss recibe daño          | Burst pequeño| Color zona        | 8        | 0.4s     | Desde punto de impacto                 |
| Boss muere                | Burst global| Multicolor         | 100      | 2.0s     | Expansión lenta + gravedad suave       |
| Sala completada           | Shower     | Multicolor         | 30       | 1.5s     | Lluvia desde arriba                    |
| Compra en tienda          | Burst      | `#4FFFCE`          | 10       | 0.5s     | Desde el ícono de la mejora            |

**Implementación:** Pool de partículas gestionado por el `ParticleManager` como servicio singleton. Cada emisión pide N partículas al pool; si el pool está exhausto, se reciclan las más viejas.

---

### 13.6 Diferenciación Jugador vs Ecos — Resumen Visual

Para que en ningún momento el jugador confunda su personaje con un eco:

1. **Contorno:** Jugador = blanco puro; Eco = color del slot
2. **Opacidad:** Jugador = 100%; Eco = 65% (40% si está resuelto)
3. **Trail de movimiento:** Jugador = ninguno; Eco = trail de 0.1s
4. **Sombra de contacto:** Jugador = sombra en el suelo; Eco = ninguna
5. **Shimmer shader:** Los ecos tienen un shimmer temporal muy lento (periodo 4s) que el jugador no tiene
6. **Sprite idle:** La animación idle del eco es 3fps (lenta, "fantasmal"); la del jugador es 4fps normal

---

## 14. ACCESIBILIDAD

### 14.1 Principios Generales

PHASE apunta a un rating de accesibilidad de al menos **Bronze** en el estándar del Game Accessibility Guidelines (gameaccessibilityguidelines.com). Los siguientes features son obligatorios para lanzamiento:

- Jugabilidad completa sin sonido
- Modo daltónico (3 variantes)
- Ajuste de velocidad de bullet-time
- Tamaño mínimo de texto 16sp
- Soporte de una sola mano

---

### 14.2 Modo Daltónico

PHASE tiene 3 tipos de daltonismo cubiertos, seleccionables en Options:

| Modo                    | Cambio aplicado                                                        |
|-------------------------|------------------------------------------------------------------------|
| Normal (por defecto)    | Paleta estándar (ver §13.2)                                            |
| Deuteranopia/Protanopia | Ámbar `#FFB840` → Azul brillante `#00BFFF`; Verde `#A8FF7A` → Naranja `#FF8C42` |
| Tritanopia              | Cian `#4FFFCE` → Amarillo `#FFE566`; Azul `#5BB8FF` → Rosa `#FF86C8`  |
| Alto contraste          | Todos los ecos en blanco; diferenciación solo por forma de ícono       |

**Diferenciación por forma (siempre activa, independiente del modo de color):**
Los 5 ecos tienen íconos de forma única superpuestos sobre sus sprites (ver tabla en §11.3). Esta diferenciación es la capa primaria de información; el color es secundario. Esto garantiza que incluso en el modo de alto contraste o con pantallas de baja calidad de color, el jugador puede distinguir los 5 ecos.

---

### 14.3 Ajuste de Velocidad de Bullet-Time

En Options → Gameplay:

- **Velocidad de ecos en bullet-time:** Slider de 1.0× (velocidad normal) a 0.5× (ecos a mitad de velocidad). Por defecto: 1.0×
- **Justificación de diseño:** El desafío central es coordinar presencia y pasado con disparidad de velocidades. Reducir la velocidad de los ecos elimina parte del desafío pero hace el juego accesible para jugadores con tiempo de reacción más lento. Es una concesión de diseño aceptada.
- **Tiempo de carga de bullet-time:** Slider de 0.1s a 0.5s (por defecto 0.3s). Jugadores con temblor de manos o movilidad reducida pueden aumentar la tolerancia.

---

### 14.4 Tamaño de Texto

**Mínimo absoluto:** 16sp para cualquier texto informativo. No hay excepciones.

| Elemento                  | Tamaño (sp) | Peso     |
|---------------------------|-------------|----------|
| Texto de tutorial         | 18          | Regular  |
| Etiqueta de botón UI      | 16          | Medium   |
| Número de HP/Timer        | 18          | SemiBold |
| Nombre de mejora (tienda) | 16          | Regular  |
| Descripción de mejora     | 14          | Regular* |
| Logros — título           | 16          | Medium   |
| Logros — descripción      | 14          | Regular* |

*Los únicos elementos en 14sp son descripciones secundarias que nunca son críticas para el gameplay.

**Escalado de sistema:** PHASE respeta el `fontScale` del sistema operativo. Si el usuario tiene escalado de texto en 130% en su OS, los textos del juego escalan proporcionalmente. El layout se adapta con scroll cuando el texto no cabe.

---

### 14.5 Soporte de Una Sola Mano

El diseño de controles es thumb-friendly para teléfonos de hasta 6.7" en cualquier mano:

- El área de juego ocupa el 80% central de la pantalla; los controles táctiles de movimiento aceptan entrada en cualquier parte del área
- Los botones de UI críticos (pausa, skip) se posicionan en zonas alcanzables con el pulgar: nunca en las esquinas superiores opuestas a la mano dominante
- En la pantalla de gameplay, ninguna acción requerida (incluyendo activar bullet-time, crear eco, moverse) requiere más de un dedo simultáneamente
- Los gestos de dos dedos se reservan para acciones opcionales de la UI (zoom en árbol de meta-progresión)

---

## 15. GUARDADO Y DATOS

### 15.1 Arquitectura de Guardado

PHASE usa un modelo de **guardado dual**: datos locales para el estado en-sesión y datos en la nube para progresión permanente.

```
[Dispositivo local]                     [Nube — servidor propio / backend BaaS]
  PlayerPrefs (Unity) o ConfigFile      Save permanente encriptado
  ─────────────────────────────         ────────────────────────────
  • Estado de run activo en curso       • Meta-progresión (árbol de habilidades)
  • Configuración de opciones           • Logros desbloqueados
  • Semilla de la run actual            • Estadísticas de runs totales
  • Pool de partículas en curso         • Fragmentos de Eco acumulados
                                        • Preferencias de accesibilidad
                                        • Timestamp de último juego (para GDPR)
```

**Backup local del save permanente:** Se mantiene una copia local del save de nube, actualizada al inicio y fin de cada run. Si hay conflicto (offline juego + online juego en otro dispositivo), la versión más reciente por timestamp gana con aviso al usuario.

---

### 15.2 Estructura del Save File

```json
{
  "version": "1.1",
  "player_id": "uuid-v4-generado-en-instalacion",
  "created_at": "2026-06-30T12:00:00Z",
  "last_synced_at": "2026-06-30T18:34:12Z",

  "meta_progression": {
    "fragments_total": 1240,
    "fragments_spent": 880,
    "skill_tree_nodes": ["NODE_ECO_CAP_2", "NODE_BT_DURATION_1"],
    "highest_run_score": 84200,
    "total_runs_completed": 47,
    "total_runs_attempted": 61
  },

  "achievements": {
    "unlocked": ["FIRST_RUN", "ECO_MASTER_3", "BOSS_DEATHLESS"],
    "progress": {
      "SPEED_RUN": { "current": 312, "target": 500 }
    }
  },

  "accessibility_prefs": {
    "colorblind_mode": "deuteranopia",
    "bt_echo_speed": 0.8,
    "bt_charge_time": 0.35,
    "sound_enabled": true,
    "haptics_enabled": true,
    "font_scale_override": null
  },

  "run_history": [
    {
      "run_id": "uuid",
      "seed": 839271,
      "date": "2026-06-30T17:22:00Z",
      "duration_seconds": 347,
      "rooms_cleared": 7,
      "boss_defeated": true,
      "score": 84200,
      "fragments_earned": 120
    }
  ]
}
```

**Tamaño estimado del save file:** <50KB por jugador en condiciones normales. Los últimos 100 runs se almacenan localmente; los más antiguos se purgan del historial local pero los totales agregados se mantienen.

---

### 15.3 Manejo de Pérdida de Datos

**Escenario: crash durante run activa**
- El estado de la run se escribe a disco local cada 30 segundos (checkpoint rolling)
- Al relanzar, si existe un run_state_checkpoint, se pregunta al jugador: "Continuar run anterior" o "Empezar nueva run"
- La run interrumpida NO otorga recompensas; solo puede continuarse o descartarse

**Escenario: desincronización local/nube**
- Al detectar divergencia, el cliente muestra: "Tus datos están actualizados. Sincronizando con la nube..."
- Si la nube tiene datos más nuevos: se aplican con notificación
- Si el local tiene datos más nuevos (fue offline): se sube con notificación
- En caso de conflicto irreconciliable: se conserva el save con mayor `total_runs_completed` y se notifica al jugador con opción de soporte

**Escenario: pérdida total del save local (reinstalación)**
- Al iniciar sesión con el mismo account, el save de nube se descarga automáticamente
- Si no hay cuenta vinculada: se inicia desde cero con aviso "Vincula una cuenta para hacer backup automático de tu progresión"

---

### 15.4 GDPR — Datos Recopilados

PHASE recopila únicamente datos necesarios para la funcionalidad del juego. No hay perfil publicitario, no hay venta de datos.

| Dato recopilado                          | Por qué se recopila                         | Retención       |
|------------------------------------------|---------------------------------------------|-----------------|
| UUID de jugador (no vinculado a PII)     | Identificar save en nube                    | Mientras cuenta existe |
| Estadísticas de runs (score, duración)   | Leaderboards y balanceo de contenido        | 36 meses        |
| Timestamp de última sesión               | Trigger de notificaciones de re-engagement  | 12 meses        |
| Preferencias de accesibilidad            | Sincronización entre dispositivos           | Mientras cuenta existe |
| Crash reports (anonimizados)             | QA y estabilidad                            | 6 meses         |

**No se recopila:** nombre real, email (a menos que el usuario lo vincule voluntariamente para recuperación de cuenta), datos de ubicación, ID de dispositivo vinculado a PII.

**Derechos GDPR implementados:**
- **Exportación de datos:** botón en Settings → About → "Exportar mis datos" genera un JSON descargable
- **Borrado de cuenta:** botón en Settings → About → "Eliminar mi cuenta" borra todos los datos de la nube en ≤72 horas con confirmación por email si aplica
- **Pantalla de consentimiento:** en primera instalación, antes de cualquier sync de nube, se presenta el resumen de datos recopilados con enlace a la Privacy Policy completa

---

## 16. LIVEOPS Y CONTENIDO FUTURO

### 16.1 Filosofía de LiveOps para PHASE

PHASE es un roguelite de runs cortas. Sus LiveOps deben respetar dos restricciones:

1. **No comprometer la integridad de diseño:** Los eventos no pueden introducir mecánicas que hagan el bullet-time menos relevante ni que diluyan la identidad central del juego
2. **No crear FOMO destructivo:** Los eventos son oportunidades de contenido adicional, no muros de acceso temporal. Todo el contenido del juego base siempre está disponible

---

### 16.2 Tipos de Evento Temporal Compatibles

| Tipo de evento            | Descripción                                                                   | Duración típica |
|---------------------------|-------------------------------------------------------------------------------|-----------------|
| Weekly Challenge          | Semilla de run fija para todos los jugadores esa semana; leaderboard global   | 7 días          |
| Echo Modifier Event       | Una semana, todos los ecos tienen un modificador (ej: ecos con doble duración)| 7 días          |
| Zona Temporal             | Sala especial nueva disponible solo durante el evento, se integra en el pool  | 14 días         |
| Boss Remix                | Un boss existente con moveset modificado; cuentan como boss para el árbol     | 7 días          |
| Fragmentos x2             | Multiplicador de Fragmentos de Eco ganados; sin pay-wall                      | 3 días          |
| Story Vignette            | 3 salas narrativas opcionales que expanden el lore sin afectar runs normales  | 30 días         |

**Eventos NO compatibles (y por qué):**
- Mecánicas de gacha: incompatible con la promesa de "habilidad, no azar en el meta"
- Eventos que requieren jugar N horas al día: el target son sesiones de 5–8 min; exigir más destruye la UX
- Contenido de ventaja temporal por pago: el modelo F2P de PHASE no incluye P2W

---

### 16.3 Cómo Añadir Nuevos Tipos de Eco sin Romper Balance

El sistema de ecos está diseñado para ser extensible. Un "tipo de eco" es una variación de comportamiento sobre la base cinemática:

```
EcoBase {
    ruta grabada: List<Vector2> con timestamps
    trigger_points: List<TriggerPoint>
    color_slot: int (0–4)
    duración: float
}

EcoComportamiento (extensión):
    EcoCinemático   — ruta exacta (base, siempre implementado)
    EcoMirror       — ruta espejada en X (unlock vía árbol)
    EcoRetrasado    — inicia N segundos después de ser creado
    EcoAcelerado    — reproduce la ruta al 1.5× velocidad
    EcoInverso      — ruta reproducida al revés
```

**Protocolo para añadir un nuevo EcoComportamiento:**
1. Definir su parámetro de variación sin cambiar la interfaz base
2. Implementar `PlayRoute(EcoBase base, EcoComportamiento params)` sin tocar el sistema de grabación
3. QA checklist específico de eco nuevo (ver §18.1)
4. Ajuste de balance: el eco nuevo debe coexistir con los 5 slots; no puede ser estrictamente mejor que el cinemático base
5. Introducción en el árbol de meta-progresión como unlock, no disponible desde el inicio

---

### 16.4 Roadmap

#### Lanzamiento (Mes 0) — Target: Q1 2027
- Pool completo de 50 salas distribuidas en 3 zonas
- 3 bosses (1 por zona)
- 5 slots de eco, tipo base: Cinemático
- Tutorial completo (7 salas)
- Meta-progresión: árbol de 24 nodos
- LiveOps: Weekly Challenge activo desde día 1

#### 3 Meses (Patch 1.1)
- Pool ampliado a 65 salas (+15 nuevas)
- Nuevo tipo de eco: EcoMirror (primer tipo alternativo, disponible vía árbol)
- Boss Remix del boss de Zona 1
- 2 Story Vignettes de lore
- Correcciones de balance basadas en datos de D7/D30 retention (ver §18.3)
- Android: corrección de problemas de compatibilidad reportados en los primeros 90 días

#### 6 Meses (Patch 1.2 — Zona 4: Fragmento de Tiempo)
- Zona 4 completa: 20 salas nuevas, 1 boss nuevo, nueva música, nuevo tileset
- Nuevo tipo de eco: EcoRetrasado
- Árbol de meta-progresión expandido: 12 nodos nuevos relacionados con Zona 4
- Sistema de Daily Challenge (más frecuente que Weekly)
- QoL: estadísticas detalladas de runs en pantalla de resultados

#### 1 Año (v2.0 — Modo Espejo)
- Modo Espejo: cada sala de las zonas 1–4 en versión espejada, con modificadores de dificultad
- Tipo de eco: EcoInverso (el más complejo; requiere diseño de salas específico)
- Coleccionables cosméticos: skins de color para el personaje (sin impacto en gameplay)
- Leaderboards de temporada con premios cosméticos
- Revisión de balance mayor basada en datos de 1 año de juego

---

### 16.5 Qué Incluye una Nueva Zona

Una zona es la unidad de contenido mayor de PHASE. Para estar completa y pasar QA:

| Componente                    | Especificación mínima                                              |
|-------------------------------|---------------------------------------------------------------------|
| Salas de zona                 | 15–20 salas pre-diseñadas (al menos 3 de dificultad alta)           |
| Boss de zona                  | 1 boss con 3 fases, moveset que requiera uso de ecos para derrotar  |
| Sala de tutorial de zona      | 1 sala introductoria de las mecánicas nuevas de la zona             |
| Tileset de entorno            | 1 set completo: piso, pared, borde, prop decorativo, elemento interactivo |
| Paleta de color de zona       | Variación sobre la paleta base; fondo distinto pero sin romper legibilidad de ecos |
| Stems de música               | BASE + RHYTHM + TENSION nuevos para la zona; los stems ECO_N se reutilizan del set global |
| Efecto ambiental              | 1 efecto de partícula o shader ambiental único a la zona            |
| Lore                          | 3–5 fragmentos de texto encontrables en las salas (no intrusivos, opcionales) |
| Mecánica de zona              | Opcional: 1 variación de mecánica que solo aparece en esta zona     |

---

## 17. ARQUITECTURA TÉCNICA

### 17.1 Evaluación Unity vs Godot para PHASE

#### Unity (versión: 2022 LTS)

**Pros para PHASE:**
- FMOD Studio integration nativa y madura: crítico para el sistema de audio adaptativo de bullet-time (§12.2.2)
- Mejor ecosistema de herramientas de profiling en mobile (Memory Profiler, Frame Debugger)
- SpriteSkin/2D Animation maduro para animaciones de personaje pixel art
- Il2CPP en iOS/Android reduce overhead de scripting en ~30% vs Mono
- Asset Store: tilemap tools, particle systems, y post-process stack disponibles sin implementar desde cero

**Cons para PHASE:**
- Overhead de memoria base del engine: ~70MB de RAM baseline vs ~35MB de Godot
- Unity Runtime Fee (2024+): modelo de precios incierto para un indie F2P de alto volumen de instalaciones. Umbral actual: 200k instalaciones + $200k de ingresos

#### Godot 4.x

**Pros para PHASE:**
- RAM footprint significativamente menor: ~35MB baseline, relevante para el target de <512MB total
- Open source: sin runtime fees, sin cambios de licencia futuros
- GDExtension permite C++ para los sistemas críticos de performance
- Hot reload en editor genuinamente más rápido

**Cons para PHASE:**
- FMOD integration en Godot 4 es comunidad-mantenida, menos robusta que la integration oficial de Unity. El audio adaptativo de PHASE es complejo; un bug en esta integration bloqueará features críticas
- Profiling tools en mobile son menos maduros: el target de <7% de batería/10min es difícil de verificar con precisión
- Godot 4.x en Android/iOS tiene issues conocidos de estabilidad en dispositivos Snapdragon 685 específicamente (al momento de este documento)

#### Recomendación: Unity 2022 LTS

**Justificación basada en 3 factores no-negociables para PHASE:**

1. **FMOD nativo:** El sistema de audio adaptativo de bullet-time es una feature de diferenciación del juego. Comprometerla por una integration comunitaria sería un riesgo de producto inaceptable.

2. **Profiling de batería en Snapdragon 685:** El target de <7% batería/10min solo se puede verificar y optimizar con herramientas maduras. Android GPU Inspector + Unity Profiler es la cadena más fiable disponible hoy.

3. **Riesgo de Runtime Fee:** Con el modelo F2P de PHASE (sin P2W, monetización por cosméticos), proyectamos <200k instalaciones en los primeros 12 meses. Si el juego supera esas cifras, el problema de Runtime Fee será un problema bienvenido de éxito, y el modelo de ingresos de un juego con >200k instalaciones generará suficiente margen para absorberlo.

**Mitigación de memoria:** La diferencia de 35MB entre motores se manejará con disciplina de asset budgeting (ver §17.5).

---

### 17.2 Sistema de Ecos Cinemáticos — Especificación Técnica

#### 17.2.1 Grabación de Ruta

```csharp
public class EchoRecorder : MonoBehaviour
{
    // Graba la ruta cada SAMPLE_INTERVAL segundos de juego REAL (no escalado)
    // Usa Time.realtimeSinceStartup para ser independiente de Time.timeScale
    private const float SAMPLE_INTERVAL = 0.033f; // 30 samples/seg = ~30fps
    private const int MAX_SAMPLES = 1024; // 34 segundos de grabación máxima

    private struct RouteSample
    {
        public Vector2 position;    // Posición en world space
        public float realTimestamp; // Tiempo real de grabación
        public AnimState animState; // Estado de animación en ese momento
        public bool isTriggerPoint; // ¿Este sample activa un trigger?
        public string triggerID;    // ID del trigger a activar si isTriggerPoint
    }

    private List<RouteSample> _samples = new(MAX_SAMPLES);
    private float _lastSampleTime;
    private bool _isRecording;
}
```

**Cuándo grabar:** Solo se graba mientras el jugador está en contacto con la pantalla. Al levantar el dedo, la grabación se pausa pero el buffer persiste. La acción se "sella" como un eco cuando el jugador completa una acción definida (llegar a un punto de destino, activar un mecanismo).

**Compresión de ruta:** Antes de crear el eco, se aplica una simplificación de Ramer-Douglas-Peucker con epsilon = 2px. Esto elimina muestras redundantes en movimientos rectilíneos, reduciendo el buffer en un 40–60% sin pérdida perceptible de fidelidad.

#### 17.2.2 Reproducción de Ruta

```csharp
public class EchoPlayback : MonoBehaviour
{
    private List<RouteSample> _route;
    private int _currentSampleIndex;
    private float _playbackStartRealTime;

    // Los ecos corren a Time.timeScale = 1.0 SIEMPRE.
    // Usan un delta time propio basado en realtimeSinceStartup
    // para ser inmunes al Time.timeScale del bullet-time del jugador.

    void Update()
    {
        float elapsed = Time.realtimeSinceStartup - _playbackStartRealTime;
        
        while (_currentSampleIndex < _route.Count &&
               _route[_currentSampleIndex].realTimestamp <= elapsed)
        {
            ApplySample(_route[_currentSampleIndex]);
            _currentSampleIndex++;
        }

        if (_currentSampleIndex >= _route.Count)
            OnRouteComplete();
    }

    private void ApplySample(RouteSample sample)
    {
        transform.position = sample.position;
        _animator.SetState(sample.animState);

        if (sample.isTriggerPoint)
            TriggerManager.Activate(sample.triggerID, this);
    }
}
```

#### 17.2.3 Trigger Points

```csharp
public class TriggerPoint : MonoBehaviour
{
    public string triggerID;
    public TriggerType type; // LEVER, BUTTON, WEIGHT_PLATE, DOOR, BEAM_BLOCKER

    // El TriggerPoint solo acepta activación de un eco con el triggerID correcto
    // Un eco llega al triggerID si lo grabó durante la acción original del jugador
    public void ActivateByEcho(EchoPlayback echo)
    {
        ApplyWorldEffect();
        EchoManager.Instance.OnEchoResolved(echo.echoSlot);
    }
}
```

**¿Qué pasa cuando el mundo cambió?**
Los ecos son cinemáticos puros: no tienen física de colisión ni rigidbody. La ruta se reproduce igual aunque el mundo haya cambiado. Esto es intencional de diseño: los ecos son "fantasmas del pasado" que atraviesan el mundo sin interactuar con él salvo en sus trigger points específicos.

Si el trigger point del eco fue destruido o desactivado: el eco completa su ruta sin resolver nada y expira normalmente con el feedback visual y sonoro de "eco expiró sin resolver".

Las salas se diseñan para que los trigger points sean siempre accesibles durante la ventana de vida del eco. Esta es una restricción de diseño de sala, no un problema técnico.

#### 17.2.4 Gestión de Memoria de Ecos

- Máximo 5 ecos activos simultáneamente (5 slots)
- Pool de 10 `EchoPlayback` objetos pre-instanciados al cargar la sala (5 activos + 5 en reserva para transición suave)
- Un buffer de `RouteSample` de 1024 entries × 5 ecos = 5120 structs. Tamaño por struct: ~22 bytes → Total: ~110KB de RAM. Negligible dentro del presupuesto de 350MB.

---

### 17.3 Sistema de Bullet-Time — Separación de Time.timeScale

```csharp
public class BulletTimeManager : MonoBehaviour
{
    [Header("Bullet-Time Config")]
    [SerializeField] private float playerTimeScale = 0.1f;    // Jugador a 10% velocidad
    [SerializeField] private float echoTimeScale   = 1.0f;    // Ecos a 100% velocidad
    [SerializeField] private float activateTime    = 0.15f;   // Transición suave ON
    [SerializeField] private float deactivateTime  = 0.12f;   // Transición suave OFF

    public void Activate()
    {
        // Time.timeScale afecta a: física, animaciones con Time.deltaTime,
        // el Update del jugador si usa Time.deltaTime
        StartCoroutine(LerpTimeScale(1.0f, playerTimeScale, activateTime));
        // Los ecos usan Time.unscaledDeltaTime → inmunes a Time.timeScale
        // El audio FMOD usa realtimeSinceStartup → inmune a Time.timeScale
        PostProcessManager.SetChromaticAberration(true);
        AudioManager.ActivateBulletTimeFilter();
    }

    public void Deactivate()
    {
        StartCoroutine(LerpTimeScale(playerTimeScale, 1.0f, deactivateTime));
        PostProcessManager.SetChromaticAberration(false);
        AudioManager.DeactivateBulletTimeFilter();
    }

    private IEnumerator LerpTimeScale(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Usamos unscaledDeltaTime para que la transición no se ralentice a sí misma
            elapsed += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        Time.timeScale = to;
    }
}
```

**Capas de tiempo en PHASE:**

| Sistema            | deltaTime usado          | Afectado por bullet-time |
|--------------------|--------------------------|--------------------------|
| Personaje jugador  | `Time.deltaTime`         | Sí (se ralentiza)        |
| Ecos cinemáticos   | `Time.unscaledDeltaTime` | No (siempre a 1.0×)      |
| Física del mundo   | `Time.deltaTime` via FixedUpdate | Sí               |
| Audio FMOD         | Reloj real interno       | No (pitch shift manual)  |
| Partículas jugador | `Time.deltaTime`         | Sí (se ralentizan)       |
| Partículas de ecos | `Time.unscaledDeltaTime` | No                       |
| UI y HUD           | `Time.unscaledDeltaTime` | No                       |
| Timers de run      | `Time.unscaledDeltaTime` | No (tiempo real)         |

---

### 17.4 Procedural de Salas — Especificación

**Modelo:** Pool de salas pre-diseñadas (hand-crafted rooms) ensambladas en orden procedural. NO hay generación de terreno.

#### 17.4.1 Estructura de una Sala

```
Sala = {
    sala_id: string,          // "ZONA1_SALA_012"
    zona: int,                // 1, 2, 3, 4
    dificultad: int,          // 1 (fácil) a 5 (difícil)
    tipo: RoomType,           // PUZZLE, COMBAT, MIXED, BOSS, TRANSITION
    ecos_requeridos: int,     // mínimo de ecos necesarios para resolver
    ecos_maximos: int,        // máximo de ecos que la sala puede gestionar limpiamente
    tiempo_estimado: float,   // segundos para un jugador hábil
    tags: List<string>,       // ["VERTICAL", "WATER", "DOUBLE_JUMP_REQUIRED"]
    entrada: DoorPosition,    // N, S, E, W
    salida: DoorPosition,     // N, S, E, W
}
```

#### 17.4.2 Algoritmo de Selección de Run

```
Al iniciar run(semilla):
    rng = Random(semilla)
    run_rooms = []
    
    // Sala de apertura: siempre dificultad 1, tipo TRANSITION (calentamiento)
    run_rooms.append( pool.getFiltered(zona=1, dificultad=1, tipo=TRANSITION)[rng] )
    
    // Salas 2..N-1: progresión de dificultad
    for i in 2..N-1:
        target_difficulty = lerp(1, 4, i / N)  // Rampa de dificultad
        candidates = pool.getFiltered(
            zona = getZonaForRoom(i, run_length),
            dificultad = [target_difficulty-1, target_difficulty, target_difficulty+1],
            NOT IN recently_seen_ids  // ventana de 100 runs recientes del jugador
        )
        run_rooms.append(candidates[rng])
    
    // Sala final: Boss
    run_rooms.append(pool.getBoss(zona=currentZona)[rng])
    
    return run_rooms
```

**Anti-repetición:** El sistema mantiene un historial por jugador de los últimos 100 `sala_id` usados. Ningún `sala_id` puede repetirse dentro de esa ventana. Con 50 salas en el pool y runs de 7 salas promedio, esto garantiza variedad por ~14 runs consecutivas (14 días a 1 run/día).

**Semilla visible:** La semilla de cada run se muestra en la pantalla de selección. Esto permite a la comunidad compartir runs específicas y recrear runs con dificultad conocida. Las semillas son enteros de 6 dígitos [100000–999999].

#### 17.4.3 Conexión entre Salas

Las salas tienen puertas de entrada y salida en posiciones predefinidas (N/S/E/W). El algoritmo conecta la salida de la sala N con la entrada de la sala N+1. Si no hay compatibilidad de puerta, se inserta una sala de transición de 1 pantalla de tamaño. Todas las salas tienen al menos una puerta E (salida) y una W (entrada), garantizando compatibilidad mínima.

---

### 17.5 Performance Targets — Detallados

**Dispositivo de referencia:** Qualcomm Snapdragon 685, 4GB RAM, Android 12, pantalla 1080×2400

| Métrica                    | Target           | Método de medición                          |
|----------------------------|------------------|---------------------------------------------|
| Framerate                  | 60fps estable    | 0 frames <55fps en gameplay normal          |
| Framerate en boss + 5 ecos | ≥55fps           | Peor escenario; burst de partículas máximo  |
| Batería / 10 min           | <7%              | Android Battery Historian                   |
| RAM en gameplay            | <350MB           | Pico en sala de boss                        |
| RAM en menús               | <180MB           | Sin sala cargada                            |
| APK size (Google Play AAB) | <100MB           | AAB comprimido en Play Console              |
| Tiempo de carga inicial    | <4s              | Desde tap en ícono hasta pantalla de inicio |
| Tiempo de carga de sala    | <0.8s            | Desde fin de sala anterior hasta gameplay   |

**Budget de assets para cumplir APK <100MB:**

| Categoría            | Budget  |
|----------------------|---------|
| Spritesheets         | 28MB    |
| Música (OGG)         | 24MB    |
| SFX (OGG)            | 8MB     |
| Tilemaps y fondos    | 12MB    |
| Shaders compilados   | 6MB     |
| Engine y código      | 18MB    |
| Overhead y margen    | 4MB     |
| **Total**            | **100MB** |

---

## 18. QA Y MÉTRICAS

### 18.1 Criterios de Calidad para Cada Sala Nueva

Antes de que una sala pase al pool de producción, debe superar **todos** los siguientes criterios:

**Criterios de jugabilidad:**
- [ ] La sala es resoluble en ≤2 intentos por un QA tester sin conocimiento previo
- [ ] La sala es resoluble sin usar bullet-time (bullet-time es facilitador, no requisito)
- [ ] La sala es resoluble usando exactamente los ecos mínimos declarados en sus metadatos
- [ ] No hay estado de bloqueo irrecuperable: el jugador nunca puede crear una configuración donde la sala sea imposible de resolver sin morir
- [ ] El tiempo de resolución medido en QA está dentro del ±30% del `tiempo_estimado` en los metadatos
- [ ] La sala funciona correctamente con el modo daltónico activado (los 3 modos)
- [ ] Crear al menos 1 eco es necesario para la resolución (sin rutas de "fuerza bruta" sin ecos)

**Criterios técnicos:**
- [ ] La sala mantiene ≥58fps en el dispositivo de referencia con 5 ecos activos simultáneamente
- [ ] Todos los trigger points responden correctamente al eco que los grabó
- [ ] No hay clips de geometría: el personaje y los ecos no pueden salirse del área jugable
- [ ] La sala carga en <0.8s desde el dispositivo de referencia
- [ ] Los colores de la sala no entran en conflicto perceptual con ninguno de los 5 colores de eco

**Criterios de diseño:**
- [ ] La sala enseña o ejercita al menos un concepto claro (declarado en sus metadatos como tags)
- [ ] La sala tiene al menos 1 momento de satisfacción visual claro (la resolución del puzzle es visualmente obvio)
- [ ] La sala no repite el patrón exacto de solución de otra sala ya en el pool

---

### 18.2 Checklist de Lanzamiento Completo

#### 4 Semanas Antes del Lanzamiento
- [ ] 50 salas en pool completo: todas superan el checklist de §18.1
- [ ] 3 bosses con 3 fases cada uno: probados 20 veces por testers distintos
- [ ] Tutorial completo: D1 retention del tutorial ≥70% en beta cerrada (N≥50 testers)
- [ ] Meta-progresión: árbol de 24 nodos sin loops imposibles ni dead ends
- [ ] FMOD integration: todos los SFX y música funcionan en Android e iOS sin crackling
- [ ] GDPR: pantalla de consentimiento, exportación de datos, y borrado de cuenta funcionales
- [ ] Save/load: sin pérdida de datos en 100 ciclos de save/kill/restore en Android e iOS
- [ ] Modo daltónico: los 3 modos probados en testers daltónicos reales (mínimo 2 por tipo)

#### 2 Semanas Antes del Lanzamiento
- [ ] Build en Google Play Internal Testing: sin crasheos en las primeras 10 sesiones para el 95% de los usuarios
- [ ] Build en TestFlight: misma estabilidad
- [ ] APK/IPA size: ≤100MB confirmado en build final
- [ ] Performance: todos los targets de §17.5 cumplidos en 5 dispositivos distintos (incluyendo Snapdragon 685)
- [ ] Tutorial: time-to-first-run-completa ≤12 minutos (medido en beta)
- [ ] Accesibilidad: todos los tap targets ≥44pt confirmados con audit de Accessibility Inspector (iOS) y Accessibility Scanner (Android)
- [ ] Localización: Español e Inglés completos; sin texto cortado en ningún tamaño de pantalla

#### Semana del Lanzamiento
- [ ] Store listings aprobados: Google Play y App Store
- [ ] Screenshots y trailers en tienda: actualizados con build final
- [ ] Política de privacidad: URL activa y accesible
- [ ] Soporte en juego: enlace a email de soporte funcional
- [ ] Analytics: eventos configurados para las métricas de §18.3
- [ ] Backend de save: prueba de carga con 500 usuarios simultáneos sin degradación
- [ ] Weekly Challenge: primer evento configurado y listo para activar en Day 1
- [ ] Crash reporting: Firebase Crashlytics activo y enviando datos al dashboard

---

### 18.3 Métricas Objetivo Post-Lanzamiento

#### Retención

| Métrica     | Target | Umbral de alarma | Acción si alarma |
|-------------|--------|-----------------|-----------------|
| D1 Retention | >40%  | <35%            | Revisar tutorial: ¿cuántos usuarios abandonan antes de completarlo? |
| D7 Retention | >15%  | <10%            | Revisar curva de dificultad salas 3–5 de run; revisar meta-progresión |
| D30 Retention| >5%   | <3%             | Revisar variedad de salas; considerar evento temporal acelerado |
| D90 Retention| >2%   | <1%             | Planear Zona 4 más temprano en el roadmap |

#### Monetización (modelo F2P con cosméticos)

| Métrica | Target | Notas |
|---------|--------|-------|
| ARPU (promedio de todos los usuarios) | $0.15/mes | Cosmético promedio $2.99; conversión estimada 5% |
| ARPPU (solo pagadores) | $3.50/mes | Asume 1 compra cosmética + fragmentos de conveniencia |
| LTV a 6 meses (cohort de instalación) | >$0.80 | Basado en D30 retention × monetización mensual |
| Conversion rate (instala → cualquier pago) | >4% | Industria F2P mobile: 2–5%; target conservador |

#### Tutorial

| Métrica | Target | Cómo medir |
|---------|--------|------------|
| Tutorial completion rate | >75% | Evento `TUTORIAL_COMPLETED` en analytics |
| Time-to-first-bullet-time | <3 min de juego | Evento `BT_FIRST_USE` con timestamp desde inicio tutorial |
| Drop-off point en tutorial | <15% en cualquier sala individual | Evento `TUTORIAL_ROOM_ABANDONED` por sala |
| First run completion | >50% de usuarios que completan tutorial | Evento `RUN_COMPLETED` en el mismo día |

#### Performance en Campo (Post-Lanzamiento)

| Métrica | Target |
|---------|--------|
| Crash-free sessions | >99.2% |
| ANR rate (Android) | <0.47% (límite de Play Store para Featured) |
| Avg session length | 8–12 min (implica al menos 1 run completa + menús) |
| Sessions per DAU | >1.5 (los jugadores vuelven a jugar más de 1 run por sesión) |

#### Qué Medir para Saber si el Tutorial Funciona

El tutorial "funciona" cuando cumple todos los siguientes criterios simultáneamente, medidos en los primeros 7 días post-lanzamiento con cohort de N≥500 usuarios:

1. **Comprensión de bullet-time:** >70% de usuarios usan bullet-time al menos 1 vez en su primera run completa
2. **Comprensión de ecos:** >60% de usuarios crean al menos 3 ecos en su primera run
3. **Coordinación eco+presente:** >40% de usuarios completan al menos 1 sala usando ≥2 ecos simultáneos en sus primeras 3 runs
4. **Tiempo hasta primer run completa:** Mediana <15 minutos desde instalación
5. **D1 retention de usuarios que completaron tutorial vs. no completaron:** Delta >15 puntos porcentuales (si completar el tutorial no mejora la retención, el tutorial tiene un problema de diseño)

Si cualquiera de estos criterios falla, hay un proceso de triage en 48 horas: se revisan grabaciones de sesión de UXCam de usuarios que fallaron en el criterio específico.

---

## 19. AUTOCRÍTICA DEL GDD

### 19.1 Secciones que Necesitan Más Iteración

**§11 HUD — Eco Strip específicamente:**
El diseño del Eco Strip asume que 5 slots visuales en la barra inferior son suficientes para comunicar el estado de 5 ecos simultáneos. Esto no ha sido probado con usuarios reales. En el Vertical Slice, la primera prueba de usabilidad debe poner a testers que nunca han visto el juego frente a 3 ecos simultáneos y medir cuántos entienden cuál eco corresponde a cuál slot sin instrucción. Si la tasa de comprensión es <60%, el sistema de identificación necesita un rediseño completo.

**§13 Arte — Paleta de ecos:**
Los 5 colores de eco fueron elegidos para separación perceptual máxima y compatibilidad con los 3 modos de daltonismo. Sin embargo, no han sido validados contra los colores reales del entorno (§13.2) en situaciones de movimiento rápido en pantalla física. Es posible que ECHO_DELTA (`#5BB8FF`) sea difícil de leer sobre los tiles de pared (`#232A3D`) en pantallas con baja saturación de color. Esta validación debe ocurrir en la primera semana del Vertical Slice con dispositivos físicos reales.

**§16 LiveOps — Modelo de monetización:**
El modelo de cosméticos sin P2W es la decisión de diseño correcta, pero los ARPU y conversion rate target son estimaciones basadas en referencias de la industria, no en datos propios. El modelo financiero del proyecto debe ser revisado por el productor ejecutivo con escenarios pesimista/base/optimista antes del Vertical Slice.

---

### 19.2 Decisiones que Pueden Cambiar en el Vertical Slice

**Velocidad del eco en bullet-time (actualmente: 1.0× siempre):**
La decisión de que los ecos corran a velocidad normal mientras el jugador va a 0.1× es el corazón de la tensión de coordinación. Sin embargo, no hemos probado si esto resulta en estrés excesivo vs. satisfacción táctica. Si el playtesting del Vertical Slice muestra que >40% de los testers sienten el juego "imposible" en salas de 3+ ecos, el valor de velocidad de ecos se ajustará a 0.7× como default (manteniendo el slider de accesibilidad).

**Pool mínimo de salas (actualmente: 50):**
El pool de 50 debe entenderse como "50 salas que superan el QA checklist", no "50 salas en desarrollo". El conteo de producción debe apuntar a 65 para tener margen de descarte.

**Bullet-time activado por quietar el dedo (actualmente: 0.3s de carga):**
En dispositivos con pantallas táctiles de baja calidad, un micromovimiento involuntario de 2–3px puede cancelar la carga repetidamente. El Vertical Slice debe probar el control en al menos 10 modelos de teléfono distintos, incluyendo 3 de gama baja. Si la tasa de activación fallida supera el 15%, se añadirá un modo alternativo: "tap doble para activar bullet-time".

---

### 19.3 Riesgos de Diseño No Resueltos

**Riesgo 1 — Curva de aprendizaje de coordinación:**
PHASE requiere que el jugador piense en dos capas temporales simultáneamente. El benchmark objetivo es que el "click" de comprensión (donde la coordinación se siente natural en lugar de estresante) ocurra antes de la run 5. Si el análisis de retención D1–D3 muestra caída pronunciada antes del run 5, la curva de aprendizaje es demasiado empinada y el tutorial necesita 2 salas adicionales de práctica gradual.

**Riesgo 2 — Fatiga de bullet-time:**
Si el jugador usa bullet-time como respuesta de pánico ante cualquier dificultad (en lugar de usarlo tácticamente), el bullet-time puede convertirse en un "modo fácil de facto" que reduce el desafío sin eliminarlo. Contramedida actual: no hay cooldown ni costo. Si el análisis muestra que los jugadores pasan >50% del tiempo en bullet-time, se debe considerar un costo de energía o duración máxima. Esta decisión está diferida al Vertical Slice.

**Riesgo 3 — Ecos como decoración vs. ecos como herramienta:**
Un jugador puede potencialmente ignorar el sistema de ecos y completar algunas salas por fuerza bruta. El diseño de sala debe garantizar que crear al menos 1 eco sea necesario para la resolución en el 100% de las salas de puzzle y el 70% de las salas de combate. Esta restricción ha sido añadida al checklist de §18.1 (criterio "Crear al menos 1 eco es necesario para la resolución").

**Riesgo 4 — Rendimiento del pool con 5 ecos en Snapdragon 685:**
La estimación de reducción del ~85% de carga vs. rigidbodies completos es pre-implementación. El Vertical Slice debe incluir una escena de prueba de estrés con los 5 ecos activos + sistema de partículas máximo + boss activo para medir el framerate real en el dispositivo de referencia en la primera semana de desarrollo. Si el resultado no alcanza los 55fps mínimos, el techo de ecos activos simultáneos puede reducirse a 4 (ajuste de balance menor pero factible).

---

### 19.4 Deuda de Diseño para Fase 9 (Vertical Slice)

Las siguientes decisiones están activas en el GDD pero marcadas para refinamiento obligatorio durante el Vertical Slice antes de producción completa:

| Deuda                                      | Por qué diferida                                     | Decisión requerida en |
|--------------------------------------------|------------------------------------------------------|-----------------------|
| Velocidad default de ecos en bullet-time   | Requiere playtesting con usuarios reales             | Semana 3 del VS       |
| Costo/cooldown de bullet-time              | Requiere datos de comportamiento de jugador          | Semana 4 del VS       |
| Diseño de animación del logo PHASE         | Asset de producción; no crítico para VS              | Fin del VS            |
| Número exacto de nodos en árbol de meta-progresión | 24 es estimación; se revisará tras probar curva de progresión | Mes 2 del VS |
| Balance de Fragmentos de Eco ganados por sala | Requiere modelo financiero validado               | Semana 6 del VS       |
| Sistema de logros completo (lista de 30+)  | Logros dependen de comportamiento real de jugadores  | Fin del VS            |
| Localización a idiomas adicionales (FR, DE, PT-BR) | No crítico para VS; sí para lanzamiento        | Mes 3 post-VS         |
| Soporte de controllers Bluetooth           | Edge case; no es el input primario del juego         | Post-lanzamiento v1.1 |
| Narrativa y lore (nombres de zonas definitivos) | Decisión de producción creativa                 | Inicio de producción completa |

---

*Fin del GDD PHASE v1.1 — Parte 2*
*Las secciones 1–10 están documentadas en GDD-PHASE-parte1.md*
*Próxima revisión: al inicio de Fase 9 (Vertical Slice) — incorporar hallazgos de playtesting*
