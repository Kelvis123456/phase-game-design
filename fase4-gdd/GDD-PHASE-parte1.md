# PHASE — Game Design Document v1.1
## Primera Mitad (Secciones 1–10)

**Clasificación:** Documento interno de producción  
**Versión:** 1.1  
**Fecha:** 2026-06-30  
**Autor:** Lead Game Designer  
**Estado:** En desarrollo activo — valores [VS] pendientes de Vertical Slice

> Nota de valores [VS]: Los valores marcados con [VS] son decisiones de diseño con justificación, pero deben medirse en el Vertical Slice antes de consolidarse. No son placeholders — son hipótesis específicas.

---

## ÍNDICE

1. Visión del Juego
2. Gameplay Core
3. Loop Principal
4. Loop Secundario
5. Tutorial
6. Progresión
7. Sistema de Runs
8. Sistema de Boss
9. Economía y Monetización
10. Balance y Dificultad

---

## SECCIÓN 1 — VISIÓN DEL JUEGO

### 1.1 Concept Statement

PHASE es un puzzle-roguelite táctil para Android e iOS donde el jugador crea fantasmas de sus propias acciones pasadas y debe coordinar su presente con esos fantasmas para resolver salas físicas.

Cada acción completada genera un "eco" cinemático: una entidad que repite infinitamente la ruta grabada. El jugador puede en cualquier momento soltar el dedo para activar bullet-time, reduciéndose a sí mismo a 0.1x de velocidad mientras los ecos continúan al 100%. La tensión central del juego no es "soy más rápido que los enemigos" sino "¿estoy en el lugar correcto para que mi pasado resuelva esto por mí?"

**Tagline:** "Tu pasado ya sabe la respuesta."

**Precedente más cercano:** Super Time Force (Capybara Games, PC 2014) — shooter de plataformas con mecánica de clones temporales. PHASE no es un shooter ni plataformas; el precedente es puramente mecánico. Ningún juego táctil mobile ha ejecutado esta idea.

**Género comercial:** Puzzle-roguelite táctil  
**Plataformas:** Android 8.0+ / iOS 14+  
**Duración de sesión:** 5–8 minutos por run  
**Público objetivo:** Jugadores de puzzle casual-core que disfrutan Baba Is You, Monument Valley, y cualquier roguelite con runs cortas (Slay the Spire mobile, Hades)

---

### 1.2 Pilares de Diseño

Los cinco pilares son filtros de decisión: cuando una mecánica, sala, o feature entre en conflicto con un pilar, el pilar gana.

#### PILAR 1 — El "Aha" es retroactivo

El jugador debe terminar salas y en retrospectiva entender que su eco resolvió algo que él no planeó. La primera sala de cada run debe tener exactamente un momento así. El diseño de sala trabaja hacia ese momento, no al revés.

*Implicación de diseño:* Las salas no se diseñan como "coloca los ecos aquí para resolver X". Se diseñan como "¿qué ruta natural tomaría el jugador que, en eco, resuelva Y?".

#### PILAR 2 — Bullet-time es lectura, no reacción

Bullet-time no existe para reflejos más lentos. Existe para que el jugador lea el estado del mundo y sus ecos con claridad. La solución siempre debe ser visible en bullet-time; nunca debe requerir bullet-time como ejecución precisa.

*Implicación de diseño:* Cualquier puzzle que solo sea solucionable con timing de precisión en tiempo real es un fallo de diseño.

#### PILAR 3 — Los ecos son personaje, no herramienta

Los ecos son versiones del jugador. Tienen skins. Tienen movimiento reconocible. Cuando un eco falla (intenta cruzar un puente que ya no existe), no es un error — es drama. El sistema trata el fallo de eco como evento narrativo, no como bug.

*Implicación de diseño:* Los ecos no pueden ser HUD abstractos ni marcadores de posición. Deben tener la misma fidelidad visual que el jugador.

#### PILAR 4 — Cada run enseña algo nuevo sobre tus ecos anteriores

La meta-progresión no da poder crudo. Da vocabulario. Un nuevo slot de eco no hace al jugador más fuerte; le da más cosas que decir con el sistema. La curva de habilidad es conceptual, no estadística.

*Implicación de diseño:* Los upgrades de run no deben tener versiones "estrictamente mejores". Cada upgrade cambia el estilo de juego posible.

#### PILAR 5 — El teléfono es el controlador correcto

PHASE se diseña para una mano, una pantalla, sin botones. Bullet-time se activa soltando el dedo — la acción más natural de descanso táctil. El movimiento es swipe o tap-and-drag. Nunca habrá una mecánica que requiera dos toques simultáneos de alta precisión.

*Implicación de diseño:* Toda mecánica se prototipa primero en mobile, no se porta desde PC.

---

### 1.3 Experiencia Objetivo

**Al terminar la primera run:** "No entendí todo lo que pasó, pero algo se sintió mágico en la segunda sala."

**A la run 10:** "Ya empiezo a ver cómo los ecos se van a mover antes de crearlos."

**A la run 30:** "Puedo leer toda la sala en bullet-time en tres segundos."

**Al conseguir el quinto slot:** "Con cinco ecos soy una orquesta."

La progresión emocional es: confusión encantada → reconocimiento → maestría → virtuosismo.

---

### 1.4 Qué NO es PHASE

Estas definiciones negativas son de producción. Si alguien propone una feature que cae en estas categorías, la justificación de diseño debe ser extraordinaria.

| No es | Por qué importa |
|---|---|
| Un juego de acción/reflejos | Bullet-time no es para reaccionar más rápido; es para leer más claro |
| Un juego de gestión de recursos | Los ecos no se "gastan"; el jugador no toma decisiones de inventario |
| Un roguelite de poder acumulativo | Los upgrades cambian el estilo, no la potencia cruda |
| Un juego narrativo con historia explícita | No hay texto de historia en pantalla durante gameplay |
| Un juego multijugador o asíncrono PvP | Los ecos son del jugador, no de otros jugadores |
| Un juego con enemigos que atacan al jugador | Las amenazas son ambientales (puertas, trampas temporizadas, gravedad) |
| Un port de PC con controles adaptados | Se diseña táctil desde el día uno |

---

## SECCIÓN 2 — GAMEPLAY CORE

### 2.1 Definición Técnica del Ciclo de Acción que Genera Eco

#### Vocabulario técnico

- **Jugador (Player Entity):** Entidad física activa. Posición en tiempo real, colisiones habilitadas, responde a input táctil.
- **Eco (Echo Entity):** Entidad cinemática. Sigue una PathData grabada. Sin física completa — solo trigger points de colisión en momentos específicos de la grabación.
- **PathData:** Array de `(timestamp_ms, position_x, position_y, action_type, action_param)` generado durante la acción del jugador.
- **Trigger Point:** Entrada en PathData marcada como `action_type: TRIGGER`. En ese timestamp, el eco ejecuta la acción grabada (activar palanca, empujar objeto, abrir puerta).
- **Grabación Activa:** Estado en que el sistema registra el PathData. Se inicia al primer movimiento del jugador en una sala y se cierra al completar la acción de sala.
- **Loop de Eco:** Una vez cerrada la grabación, el eco nace en la posición inicial de la grabación y ejecuta el PathData en bucle infinito con timestamp reiniciando a 0 cada ciclo.

#### Flujo técnico de generación de eco

```
ESTADO: Jugador entra a sala
  → Sistema inicia Grabación Activa (PathData = [])
  → Cada frame: PathData.append(tiempo_actual, pos_jugador, tipo_acción)
  
CONDICIÓN: Jugador completa acción de sala (llega a punto de salida O activa último trigger)
  → Sistema cierra Grabación Activa
  → Sistema crea Echo Entity con PathData grabado
  → Echo Entity posición inicial = PathData[0].position
  → Echo Entity comienza ejecución de PathData desde timestamp 0
  → Grabación Activa = null
  
CADA FRAME del Echo Entity:
  → Echo avanza en PathData según tiempo transcurrido desde último loop
  → Si PathData[i].action_type == TRIGGER: ejecutar acción grabada
  → Si timestamp > PathData[último].timestamp: reiniciar loop (timestamp = 0, posición = PathData[0])
```

#### Regla de Trigger Points

Los Trigger Points son la interfaz entre los ecos cinemáticos y la física del mundo. Un eco no empuja objetos con física — en el timestamp del trigger, el objeto cambia de estado directamente (palanca: ON→OFF, bloque: posición A→posición B interpolada en 0.3s). Esto elimina la complejidad de física completa para los ecos y hace el comportamiento 100% determinista y reproducible.

**Excepción:** Si un objeto interactuable fue destruido o movido permanentemente por el jugador antes de que el eco llegue al Trigger Point, ver Sección 2.4 (Fallo de Eco).

---

### 2.2 Mecánica de Bullet-Time

#### Definición

Bullet-time es el estado en que el jugador reduce su velocidad de percepción y movimiento sin afectar la velocidad de los ecos.

- **Velocidad del jugador en bullet-time:** 0.1x [VS — probar 0.08x–0.15x, objetivo: sentirse "casi quieto" pero con control]
- **Velocidad de ecos en bullet-time:** 1.0x (sin cambio)
- **Velocidad del mundo (físicas ambientales, trampas):** 0.3x [VS — probar 0.2x–0.4x, objetivo: visible pero manejable]
- **Velocidad de audio:** Pitch del jugador baja a 0.6x; ecos mantienen pitch normal

#### Activación

**Acción:** El jugador levanta el dedo de la pantalla en cualquier momento durante gameplay activo.

**Desactivación:** El jugador pone el dedo en la pantalla y comienza a mover.

**Latencia:** La transición de velocidad normal → bullet-time es inmediata (un frame). La transición de regreso a normal es un ease-out de 0.15s [VS] para evitar "teleportación" visual.

**Disponibilidad:**
- Run 1: NO DISPONIBLE (el sistema existe pero el input está deshabilitado)
- Run 2+: Disponible desde el inicio de la run
- Cooldown: Ninguno. Bullet-time es ilimitado — la restricción es táctil (no puedes moverte con fluidez mientras el dedo está levantado)

**Nota de diseño:** La restricción de bullet-time no es un recurso limitado — es una restricción física ergonómica. El jugador que levanta el dedo no se mueve bien. Eso es suficiente restricción.

#### Visualización

| Elemento | Estado Normal | Estado Bullet-Time |
|---|---|---|
| Jugador | Color normal, sin efecto | Borde pulsante azul suave (echo-blue, 0.8 alpha) |
| Ecos | Semi-transparentes (alpha 0.7) | Completamente opacos (alpha 1.0) |
| Fondo de sala | Saturación 100% | Saturación 60%, viñeta oscura sutil en bordes |
| Partículas de movimiento | Trails cortos (0.1s) | Trails largos (0.4s) en ecos únicamente |
| UI / HUD | Visible | Icono de bullet-time activo en esquina superior derecha |
| Tiempo transcurrido | Corre normal | Pausa el contador de récord personal |

**Nota visual crítica:** En bullet-time, los ecos se ven más "reales" que el jugador (más opacos, más detalle). Esto refuerza la identidad del Pilar 3: los ecos son el protagonista, el jugador es el observador.

---

### 2.3 Reglas de Interacción Jugador-Eco

Las interacciones entre el jugador y sus ecos son deliberadamente limitadas. La claridad supera la expresividad aquí.

#### Lo que el jugador PUEDE hacer con sus ecos

| Interacción | Comportamiento | Propósito |
|---|---|---|
| Pasar a través de un eco | El jugador traspasa físicamente los ecos (sin colisión) | Evitar frustración de bloqueo |
| Observar ruta de eco | En bullet-time, el trail del eco es claramente visible | Permite planificación |
| Usar objetos que el eco activó | Si un eco abrió una puerta, el jugador puede cruzarla | Cooperación fundamental |
| Llegar antes que un eco | El jugador puede estar en posición antes que el eco active un trigger | Permite setups avanzados |

#### Lo que el jugador NO PUEDE hacer con sus ecos

| Acción bloqueada | Razón |
|---|---|
| Empujar o mover un eco | Los ecos son cinemáticos; aplicarles física crearía inconsistencias |
| Cancelar un eco activo | El pasado no se puede borrar dentro de una run (el meta-slot de "borrar eco" es una decisión separada) |
| Cambiar la ruta de un eco existente | La ruta se graba una vez; es inmutable |
| Recibir daño de un eco | Los ecos no son amenaza para el jugador |

#### Regla de Sincronización Temporal

Los ecos corren en tiempo de mundo absoluto desde que nacen, no en tiempo del jugador. Esto significa que si el jugador activa bullet-time, los ecos no esperan — siguen su loop. El jugador debe entender que sus ecos están siempre "en movimiento" independientemente de lo que él haga.

**Consecuencia de diseño:** La solución de muchos puzzles requiere que el jugador llegue a un punto CUANDO el eco esté en un punto específico. Bullet-time sirve para ver cuándo está el eco en ese punto y maniobrar al jugador hacia su posición.

---

### 2.4 Fallo de Eco — Cuando el Mundo Ya No Permite la Acción Grabada

Un eco puede llegar a un Trigger Point y encontrar que el objeto interactuable ya no está en el estado que estaba cuando el jugador lo activó. Esto se llama **Eco Frustrado**.

#### Causas de Eco Frustrado

1. **Objeto destruido:** El jugador destruyó un objeto que el eco necesita activar (poco común en diseño de sala intencionado, pero posible en salas avanzadas)
2. **Objeto ya activado:** Otro eco activó el mismo trigger antes (timing conflict)
3. **Objeto bloqueado:** El jugador está parado en el path del eco impidiéndole llegar al trigger (no aplica — el jugador no bloquea a los ecos físicamente)
4. **Objeto en estado diferente:** La sala fue reseteada (imposible dentro de una run; relevante para el sistema de retry)

#### Comportamiento del Sistema ante Eco Frustrado

```
SI eco llega a Trigger Point Y estado_objeto != estado_grabado:
  → Eco ejecuta animación de "frustración" (0.5s — vibración suave, destello rojo)
  → Eco CONTINÚA su path (no se detiene, no se destruye)
  → Trigger Point marcado como FRUSTRADO para esta iteración del loop
  → En la siguiente iteración del loop: intenta de nuevo
  → Sistema registra evento FrustrationEvent para analítica
```

**Nota de diseño crítica:** El eco NO desaparece ni se rompe. El eco continúa su loop. En la siguiente vuelta, si el estado del objeto volvió al estado grabado (porque otro eco lo reseteó, o porque el objeto es cíclico), el trigger se ejecuta normalmente.

#### Comunicación al Jugador

El eco frustrado comunica su estado con la animación de vibración + destello, pero **sin texto de error**. El jugador debe interpretar "mi eco no pudo hacer algo" y decidir si eso es problema. En muchos casos no lo es. En algunos casos es la clave del puzzle.

**Regla de Fairness:** Ninguna sala de las Runs 1–5 puede tener una solución donde el eco frustrado sea el estado correcto deliberado. El Eco Frustrado como mecánica de puzzle se introduce en zonas avanzadas (Zona 3+) una vez el jugador tiene vocabulario para leerlo.

---

## SECCIÓN 3 — LOOP PRINCIPAL

### 3.1 Diagrama del Loop Completo

```
┌─────────────────────────────────────────────────────────────┐
│                        META-CAPA                            │
│  Pantalla de Inicio → Árbol de Progresión → Seleccionar Run │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                     INICIO DE RUN                           │
│  Animación de entrada (2s) → Carga de sala seed → Sala 1   │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                    LOOP DE SALA (×4)                        │
│                                                             │
│  Entrar sala → Leer sala (3-8s) → Ejecutar acción →        │
│  Generar eco → Resolver puzzle → Trigger de salida →       │
│  Pantalla de eco (2s) → Siguiente sala                     │
│                                                             │
│  En cada sala: 0-2 upgrades de run disponibles (ver S7)    │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                    SALA DE BOSS (×1)                        │
│  Animación de entrada boss (3s) → Fase 1 (2 ecos) →       │
│  Fase 2 (3 ecos) → Fase 3 (bullet-time + todos ecos) →    │
│  Derrota boss → Animación de victoria (4s)                 │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                   PANTALLA DE FIN DE RUN                   │
│  Récord personal actualizado → Phase Crystals ganados →    │
│  Skins desbloqueados (si aplica) → Estadísticas de run     │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                   META-PROGRESIÓN                           │
│  Gastar Phase Crystals → Árbol de progresión →             │
│  Desbloquear slot de eco / skin / modificador              │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
                    Nueva Run →
```

---

### 3.2 Duración de Cada Etapa

| Etapa | Duración Objetivo | Duración Máxima | Notas |
|---|---|---|---|
| Pantalla de inicio + selección | 15s | 60s | Jugador veterano va directo |
| Animación de entrada de run | 2s | 2s | Fija, no saltable en Run 1; saltable en Run 2+ |
| Sala estándar (Runs 1-10) | 45–90s | 120s | Incluye tiempo de lectura |
| Sala estándar (Runs 11-30) | 30–60s | 90s | Jugador más eficiente |
| Sala estándar (Runs 31+) | 20–45s | 60s | Maestría visible |
| Pantalla de eco (entre salas) | 2s | 2s | Fija; muestra el eco generado moviéndose |
| Boss (Runs 1-5) | 90–150s | 240s | 3 fases |
| Pantalla de fin de run | 10s | 30s | Jugador puede revisar estadísticas |
| Meta-progresión (si gasta) | 20–120s | — | Solo si hay cristales para gastar |
| **Run completa (Runs 1-10)** | **5:00–7:30** | **10:00** | |
| **Run completa (Runs 30+)** | **4:00–6:00** | **8:00** | Runs más cortas con maestría |

---

### 3.3 Puntos de Decisión del Jugador

En cada run, el jugador toma exactamente estas decisiones, en este orden:

**Antes de la run:**
1. ¿Qué modificador de run activo? (disponible desde Run 5) — Un modificador cambia el ruleset de la run completa

**En cada sala (×4):**
2. ¿Qué ruta tomo yo? (define el eco que se generará)
3. ¿Activo bullet-time para leer antes de actuar, o confío en mi intuición?
4. ¿Acepto el upgrade de run que apareció, o prefiero pasar? (si hay upgrade disponible)

**En el boss:**
5. ¿Qué orden uso mis ecos para las fases?

**Al final:**
6. ¿En qué gasto los Phase Crystals?

**Total de decisiones por run:** 5–12 (dependiendo de cuántos upgrades aparecen)

Esta limitación es intencional. PHASE no es un juego de muchas decisiones — es un juego donde cada decisión se siente con peso porque son pocas.

---

## SECCIÓN 4 — LOOP SECUNDARIO

### 4.1 Árbol de Meta-Progresión Permanente

La meta-progresión tiene una sola moneda (Phase Crystals) y un árbol con cuatro ramas. Las ramas no se desbloquean secuencialmente — el jugador puede gastar en cualquier rama desde el inicio, con la restricción de que algunos nodos requieren nodos previos en la misma rama.

#### RAMA A — CAPACIDAD DE ECOS

Esta rama es la más impactante mecánicamente. Cada nodo es una expansión fundamental del espacio de posibilidades.

| Nodo | Nombre | Efecto | Costo (PC) | Requisito previo | Desbloqueado en |
|---|---|---|---|---|---|
| A1 | Eco Base | 2 slots de eco activos | — | — | Inicio (gratis) |
| A2 | Tercer Espejo | 3 slots de eco activos | 150 | A1 | ~Run 8-12 |
| A3 | Resonancia Cuádruple | 4 slots de eco activos | 300 | A2 | ~Run 18-22 |
| A4 | Quinteto Temporal | 5 slots de eco activos (máximo) | 500 | A3 | ~Run 28-35 |
| A5 | Persistencia de Eco | Los ecos no se resetean al fallar en sala (persisten entre intentos de sala) | 200 | A2 | ~Run 20+ |
| A6 | Memoria de Ruta | Muestra el trail del eco durante 1s adicional en bullet-time | 100 | A1 | ~Run 6+ |

**Nota sobre A2 — Tercer Espejo:** Este desbloqueo merece una pantalla especial de celebración. Ver Sección 6.3.

#### RAMA B — MODIFICADORES DE RUN

Los modificadores de run se seleccionan antes de iniciar una run y cambian las reglas de esa run específica. Solo puede estar activo un modificador por run.

| Nodo | Modificador | Efecto | Costo (PC) | Tipo |
|---|---|---|---|---|
| B1 | Run Limpia | Sin modificador (default) | — | Siempre disponible |
| B2 | Modo Espejo | La sala se refleja horizontalmente | 80 | Dificultad |
| B3 | Eco Acelerado | Los ecos corren al 1.3x de velocidad | 80 | Dificultad |
| B4 | Niebla de Sala | Visibilidad de sala reducida a radio de 3 tiles alrededor del jugador | 120 | Dificultad |
| B5 | Doble Bullet | Bullet-time baja la velocidad del mundo a 0.1x (jugador también a 0.05x) | 100 | Especialización |
| B6 | Sin Bullet | Bullet-time desactivado; bonificación de PC al completar sala: +50% | 150 | Desafío extremo |
| B7 | Eco Fantasma | Los ecos son invisibles excepto en bullet-time | 120 | Maestría |
| B8 | Sala Única | La run tiene solo 2 salas + boss (más corta, recompensa normal) | 60 | Casual |

#### RAMA C — COSMÉTICOS DE ECO

Los cosméticos son puramente visuales y no afectan gameplay. La skin del eco cambia cómo se ve la entidad que repite las acciones del jugador.

| Nodo | Skin | Descripción visual | Costo (PC) | Categoría |
|---|---|---|---|---|
| C1 | Eco Base | Silueta del jugador en azul semitransparente | — | Default |
| C2 | Neón Pulso | Líneas de neón que pulsan con el ritmo de la ruta | 80 | Energético |
| C3 | Sombra Distorsionada | Silueta negra con distorsión de calor en bordes | 80 | Oscuro |
| C4 | Partículas de Cristal | El eco se fragmenta en partículas de cristal en movimiento | 120 | Premium |
| C5 | Espejo Puro | El eco es un reflejo especular metálico del jugador | 100 | Elegante |
| C6 | Fantasma Retro | Pixel art 8-bit semitransparente | 80 | Nostálgico |
| C7 | Plasma Temporal | Gradiente animado de azul a violeta con ondas | 150 | Premium |
| C8 | Espectro de Luz | El eco emite luz propia que ilumina el entorno | 180 | Premium |
| C9 | Vacío | El eco es una silueta negra absoluta, sin transparencia | 60 | Minimalista |
| C10 | Arco Iris Cuántico | Cada loop del eco cambia de color | 200 | Raro |

#### RAMA D — CALIDAD DE VIDA

| Nodo | Nombre | Efecto | Costo (PC) | Notas |
|---|---|---|---|---|
| D1 | Historial de Runs | Muestra estadísticas detalladas de las últimas 10 runs | 50 | UI |
| D2 | Modo Sin Anuncios | Elimina todos los anuncios recompensados (ver S9) | 500 | Monetización ética |
| D3 | Salto de Tutorial | Permite saltar el tutorial en cuentas nuevas | 30 | QoL |
| D4 | Animaciones Rápidas | Reduce animaciones cinemáticas a 0.5x velocidad | 40 | QoL |
| D5 | Selector de Semilla | Permite ingresar una semilla manual para reproducir una run | 150 | Avanzado |

---

### 4.2 Sistema de Colección de Skins de Ecos

Las skins se gestionan en una pantalla dedicada accesible desde el menú principal: **"MIS ECOS"**.

#### Mecánica de Colección

- El jugador tiene hasta 5 slots de eco (con meta-progresión A1–A4)
- Cada slot puede tener una skin diferente asignada
- Las skins se pueden cambiar en cualquier momento fuera de una run
- Durante la run, el Eco 1 usa la skin del Slot 1, el Eco 2 usa la skin del Slot 2, etc.

#### Desbloqueo por Logros

Además de comprar con PC, algunas skins se desbloquean por logros específicos:

| Skin Especial | Condición de Desbloqueo | Valor si se comprara |
|---|---|---|
| Eco Dorado | Completar 50 runs | No disponible para compra |
| Eco de Errores | Tener exactamente 3 Trigger Points frustrados en una run ganada | No disponible para compra |
| Eco Clásico (SNES) | Completar una run en Modo Sin Bullet | No disponible para compra |
| Eco Primordial | Completar el juego en todas las zonas (ver Zona 5) | No disponible para compra |

---

### 4.3 Sistema de Récords Personales

PHASE no tiene leaderboards globales (decisión de diseño: competencia social no es el pilar emocional del juego). El sistema de récords es personal y sirve como motivador interno.

#### Métricas registradas por run

| Métrica | Descripción | Visible en |
|---|---|---|
| Tiempo de sala | Segundos desde entrada hasta salida exitosa | Pantalla de fin de run |
| Tiempo de run | Segundos desde inicio hasta victoria | Pantalla de fin de run + historial |
| Ecos creados | Número total de ecos generados en la run | Historial |
| Bullet-times usados | Número de veces que se activó bullet-time | Historial |
| Tiempo en bullet-time | Segundos totales en estado bullet-time | Historial |
| Trigger Points logrados | Triggers ejecutados exitosamente vs frustrados | Historial |
| Run perfecta | Run sin ningún Eco Frustrado en ninguna sala | Badge especial |

#### Pantalla de récord

En la pantalla de fin de run, si el jugador bate su récord personal de tiempo, se muestra una animación de "fantasma del récord anterior" — el eco más veloz de todas sus runs previas aparece corriendo su mejor tiempo mientras el jugador ve sus estadísticas. Esto refuerza el lenguaje visual del juego: el pasado siempre está presente.

---

## SECCIÓN 5 — TUTORIAL

### Principio Rector: Nunca Texto, Siempre Diseño de Nivel

El tutorial de PHASE no tiene texto de instrucciones. Cada mecánica se introduce a través de la geometría de la sala, el comportamiento observable del mundo, y el diseño de luz y sombra que guía la atención visual. Un jugador que no habla el idioma del dispositivo debe poder completar el tutorial.

**Inspiración metodológica:** La introducción de mecánicas en Journey (thatgamecompany), donde el jugador descubre sus poderes por observación y consecuencia, sin un solo tutorial de texto.

---

### 5.1 RUN 1 — Tutorial Completo (Solo Ecos, Sin Bullet-Time)

El objetivo del Run 1 es enseñar una sola cosa: "Mis acciones pasadas se repiten y pueden ayudarme."

Bullet-time no existe en Run 1. El input está deshabilitado en el sistema. El jugador no puede activarlo aunque lo intente.

#### SALA TUTORIAL 0 — "El Espejo"

**Objetivo de diseño:** Enseñar que el jugador se mueve, y que algo repite ese movimiento.

**Geometría:**
- Sala rectangular, 10×5 tiles
- Una única palanca en el centro de la sala
- Una puerta de salida cerrada en el extremo derecho, vinculada a la palanca
- La palanca solo se activa al pasar sobre ella (no hay botón de "usar")
- Iluminación: luz brillante sobre la palanca, puerta en penumbra

**Flujo segundo a segundo:**
- 0:00 — Jugador aparece en el extremo izquierdo. La sala está quieta.
- 0:00–0:05 — El jugador explora. Si toca la pantalla y arrastra, el personaje se mueve.
- 0:06–0:20 — El jugador pasa sobre la palanca. Sonido de activación (click satisfactorio). La puerta se abre con una animación.
- 0:20–0:30 — El jugador llega a la puerta. Animación de transición de sala (0.5s).
- 0:30 — PANTALLA DE ECO: La sala se muestra vacía. Luego, la silueta azul del jugador aparece en el extremo izquierdo y repite la ruta. Pasa sobre la palanca. La puerta se abre de nuevo. El eco llega a la puerta. Fade out. Duración: 2s.

**Qué aprende el jugador:** "Hubo una animación de algo repitiendo lo que hice."

**Nota:** El jugador aún no sabe que eso es un "eco" ni que importa. Solo ve que pasó algo.

---

#### SALA TUTORIAL 1 — "La Puerta Cerrada"

**Objetivo de diseño:** Enseñar que el eco puede hacer cosas mientras el jugador hace otras cosas simultáneamente.

**Geometría:**
- Sala en forma de L, 12×8 tiles
- Dos palancas: Palanca A (arriba izquierda), Palanca B (abajo derecha)
- La puerta de salida requiere que AMBAS palancas estén activadas al mismo tiempo
- La distancia entre A y B hace imposible activar ambas con un solo jugador sin ayuda
- El eco generado en la Sala 0 (que activó la Palanca A del tutorial anterior) comienza a moverse por la sala al llegar el jugador

**Comportamiento del eco al entrar a la sala:**
- El Eco 1 (generado en Sala 0) está en la sala. Se mueve hacia Palanca A.
- El eco no tiene contexto de esta sala — ejecuta su path grabado de Sala 0, que pasaba por la zona donde ahora está Palanca A.
- Cuando el eco pasa sobre Palanca A, la activa (Trigger Point).
- La puerta de salida necesita que Palanca B también esté activada.

**Flujo esperado del jugador:**
- 0:00 — Jugador entra. Ve el eco moviéndose en la sala. Primera vez que lo ve en tiempo real durante gameplay activo.
- 0:00–0:10 — Jugador explora. Probablemente toca Palanca B. La puerta no se abre (Palanca A está inactiva).
- 0:10–0:20 — El eco llega a su Trigger Point y activa Palanca A. La puerta parpadea — casi se abre. El jugador ve la palanca A activarse "sola".
- 0:20–0:30 — El jugador conecta: "el eco activó eso". El jugador activa Palanca B al mismo tiempo que el eco vuelve al loop y activa Palanca A de nuevo.
- 0:30–0:45 — Puerta abierta. Jugador sale.
- 0:45 — PANTALLA DE ECO: El nuevo eco de esta sala se muestra. Este eco irá a Palanca B.

**Si el jugador no lo entiende:** El diseño de sala tiene una ventana temporal amplia [VS: 8s de superposición entre activación de A y regreso del loop]. Si el jugador no activa B durante esa ventana, el eco vuelve a empezar y la ventana se repite. No hay penalización. El jugador puede intentarlo infinitas veces.

**Qué aprende el jugador:** "El eco hace cosas por mí en tiempo real. Tengo que coordinar."

---

#### SALA TUTORIAL 2 — "El Momento No Planeado"

**Objetivo de diseño:** El corazón del juego. El jugador resuelve la sala, y al terminar se da cuenta de que su eco resolvió algo que él nunca planeó. Cumple la Regla R1 (primera sala con el "aha" retroactivo).

**Geometría:**
- Sala de 14×10 tiles, más compleja
- Plataformas a distintas alturas con palancas de peso (requieren que algo esté parado sobre ellas para mantenerse activadas)
- La palanca de peso A está en una plataforma alta. Si el jugador sube para activarla, la palanca B (abajo) se desactiva por un contrapeso.
- La solución "natural" del jugador: subir, activar A, bajar, activar B — pero B se desactiva cuando el jugador sube de nuevo para llegar a la salida.

**La trampa inteligente:**
- La salida está en la plataforma alta, detrás de la palanca A.
- El jugador inevitablemente sube, activa A (por peso), baja a activar B, y sube de nuevo para llegar a la salida.
- Cuando el jugador sube de nuevo, B se desactiva. La puerta se cierra... pero el Eco 1 (de la Sala 0) llega en ese momento a su Trigger Point, que está justo sobre Palanca B. Lo activa. La puerta se abre.
- El jugador sale. La sala resuelve sola algo que él no planeó.

**Calibración temporal crítica:** El diseñador de nivel debe sincronizar el loop del Eco 1 para que su arrival a Palanca B ocurra en el rango de tiempo en que el jugador típicamente sube la segunda vez. Esto requiere: medir tiempo promedio de los pasos anteriores del tutorial [VS: medir en playtest con 20 jugadores, ajustar duración de loop de Eco 1 de 12s–18s según datos].

**Pantalla de eco al final de Sala 2:** Muestra los dos ecos moviéndose simultáneamente en la sala. El jugador ve por primera vez la "danza" de dos ecos coordinados.

---

#### SALA TUTORIAL 3 — "Consolidación"

**Objetivo de diseño:** Confirmar el aprendizaje. El jugador debe usar sus dos ecos intencionalmente para resolver una sala ligeramente más compleja.

**Geometría:**
- 3 palancas de peso en posiciones separadas
- La puerta requiere las 3 activas simultáneamente
- El jugador solo puede estar en una posición a la vez
- Los 2 ecos (de Salas 0 y 1) ya tienen paths que pasan por dos de las palancas
- El jugador debe activar la tercera

**Qué aprende el jugador:** "Puedo contar con mis ecos para hacer cosas mientras yo hago otra."

**Al terminar Run 1:** El jugador no tiene bullet-time. No sabe que existe. Ha completado una run de 4 salas usando solo ecos. Pantalla de fin de run muestra crystals ganados y una animación: los 3 ecos corren en loop de sus salas. Fade a negro. Texto en pantalla: una sola palabra — "PHASE". Sin instrucciones para la siguiente run.

---

### 5.2 Introducción de Bullet-Time en Run 2

**El gancho:** Al iniciar la Run 2, la pantalla de selección muestra un icono nuevo junto al personaje del jugador — un símbolo de reloj de arena con el texto "NUEVO PODER" en un badge pequeño. No hay explicación. El jugador entra a la run.

**Sala 0 de Run 2 — Diseño específico:**
- La sala es idéntica en geometría a la Sala Tutorial 0, pero con una variación: hay un obstáculo que se mueve en la ruta del jugador (una plataforma oscilante a 1x velocidad).
- El obstáculo hace que activar la palanca en tiempo normal sea frustrante (el jugador llega en mal timing repetidamente).
- El diseño de sala tiene una pista visual: el obstáculo tiene una ranura iluminada que indica cuándo está en posición correcta de paso.

**El descubrimiento:**
- Si el jugador levanta el dedo (por accidente al ajustar el agarre, o deliberadamente al dudar), bullet-time se activa.
- El obstáculo se ralentiza a 0.3x. El jugador puede pasar.
- Cuando el jugador pone el dedo de vuelta, la velocidad normal regresa.

**Refuerzo visual en el momento del descubrimiento:**
- Al primer activación de bullet-time: la pantalla hace un flash azul suave (0.3s) + un sonido de "tiempo distorsionado" (diseño de sonido: como el reverb de una nota grave prolongada).
- El badge "NUEVO PODER" desaparece. No hay más texto.

**Por qué funciona:** El jugador descubre bullet-time en un contexto donde ya tiene un problema (el obstáculo) y la solución es el resultado natural de una acción táctil de "pausa" (soltar el dedo). La mecánica se enseña a través de la ergonomía del dispositivo.

---

## SECCIÓN 6 — PROGRESIÓN

### 6.1 Curva de Dificultad Run 1 a Run 50

La dificultad en PHASE no escala linealmente. Tiene etapas con mesetas intencionales donde el jugador consolida habilidades antes del siguiente salto.

| Etapa | Runs | Habilidad del Jugador | Complejidad de Sala | Slots de Eco |
|---|---|---|---|---|
| Aprendizaje | 1–5 | Entiende ecos básicos, descubre bullet-time | 1–2 ecos, puzzles lineales | 2 |
| Adaptación | 6–12 | Usa bullet-time intencionalmente | 2 ecos, primera ramificación de solución | 2 |
| Transición | 13–20 | Planea rutas de eco antes de moverse | 2–3 ecos, puzzles con timing | 2–3 |
| Integración | 21–30 | Lee la sala completa en bullet-time primero | 3 ecos, Eco Frustrado como mecánica | 3–4 |
| Maestría | 31–40 | Ejecuta soluciones multicapa sin bullet-time frecuente | 4 ecos, interacciones cross-eco | 4 |
| Virtuosismo | 41–50 | Anticipa soluciones antes de entrar a la sala | 4–5 ecos, ecos que interactúan entre sí | 5 |

**Nota de curva:** Las runs 13–15 son la "segunda pared de aprendizaje". El jugador ya domina los ecos básicos, pero ahora las salas empiezan a requerir que sus ecos interactúen entre sí (Eco 1 abre una puerta para que Eco 2 pueda pasar). Este es el salto conceptual más grande del juego, y el diseño de salas debe manejarlo con una meseta de 2–3 salas de introducción gradual.

---

### 6.2 Escalado de Complejidad de Salas

#### Variables de dificultad por sala

| Variable | Rango | Notas |
|---|---|---|
| Número de ecos en sala | 0–5 | Escalado por meta-progresión de slots |
| Número de trigger points por eco | 1–4 | Más triggers = eco más complejo |
| Ventana de sincronización [VS] | 2s–8s | Cuánto tiempo tiene el jugador para sincronizar |
| Objetos dinámicos en sala | 0–6 | Plataformas móviles, obstáculos, etc. |
| Dependencias entre ecos | 0–3 | Ecos que necesitan que otro eco haya actuado primero |
| Soluciones alternativas | 1–3 | Cuántas formas válidas tiene la sala |

#### Regla de Escalado por Zona

PHASE tiene 5 zonas visuales, cada una con un tema ambiental. Las zonas no son solo cosméticas — definen el perfil de dificultad de sus salas.

| Zona | Runs típicas | Tema visual | Perfil de dificultad |
|---|---|---|---|
| Zona 1 — Umbral | 1–10 | Sala de espejos, reflejos | Introducción, ecos básicos |
| Zona 2 — Fracturas | 11–20 | Vidrio roto, geometría irregular | Timing, ecos coordinados |
| Zona 3 — Abismo | 21–30 | Espacio-tiempo distorsionado, negro y púrpura | Eco Frustrado como mecánica, dependencias |
| Zona 4 — Resonancia | 31–40 | Cristal sonoro, frecuencias visuales | Cross-eco, 4 slots activos |
| Zona 5 — Convergencia | 41–50 | Todo lo anterior superpuesto, palimpsesto visual | Maestría, 5 slots, anti-patrones del jugador |

---

### 6.3 Desbloqueo de Slots Adicionales y el Tercer Slot Memorable

#### Momentos de desbloqueo

Los slots 3, 4, y 5 son los tres momentos más importantes de la meta-progresión. Deben sentirse como eventos, no como actualización de número.

#### EL TERCER SLOT — "Tercer Espejo"

Este es el desbloqueo más importante del juego. El primer momento en que el jugador ve tres versiones de sí mismo moviéndose simultáneamente.

**Secuencia de desbloqueo (diseño específico):**

1. El jugador compra el nodo A2 (Tercer Espejo) en el árbol de progresión.
2. En lugar de ir directo a la pantalla de inicio de run, se reproduce una secuencia cinemática (8s, saltable después de 3s):
   - La pantalla muestra la sala vacía de Tutorial 1 (el primer puzzle de coordinación).
   - El Eco 1 entra y se mueve. El Eco 2 entra desde la dirección opuesta.
   - Un tercer eco aparece desde el techo — cae lentamente, aterriza, se une a los otros dos.
   - Los tres ecos se alinean y miran al frente (hacia el jugador).
   - Flash de luz. Fade a negro. El número "3" aparece en el centro de la pantalla, en el mismo azul de los ecos. Desaparece.
3. La siguiente run comienza con 3 slots activos.

**Por qué es memorable:** La sala del tutorial es familiar. Agregar un tercer eco en ese espacio conocido hace el contraste inmediato. No es una nueva sala — es el espacio conocido transformado.

---

## SECCIÓN 7 — SISTEMA DE RUNS

### 7.1 Estructura de una Run Completa

Una run consiste de:
- 4 salas estándar (seleccionadas proceduralmente)
- 1 sala de boss (determinada por zona activa)
- 0–2 oportunidades de upgrade de run (aparecen entre salas, no en todas las transiciones)

#### Pool de Salas Base

Cada sala tiene metadatos que el algoritmo de ensamblaje usa para construir runs balanceadas.

| Metadato | Tipo | Valores posibles | Propósito |
|---|---|---|---|
| `zone_id` | Enum | Z1–Z5 | Restricción temática |
| `difficulty_tier` | Int | 1–10 | Curva de run |
| `eco_count_required` | Int | 1–5 | Filtrado por slots del jugador |
| `primary_mechanic` | Enum | SYNC, TIMING, DEPENDENCY, FRUSTRATION, SOLO | Diversificación |
| `estimated_duration_s` | Int | 20–120 | Control de duración de run |
| `has_alt_solution` | Bool | true/false | Para algoritmo de fairness |
| `intro_run_min` | Int | 1–30 | Primera run en que puede aparecer |
| `weight_base` | Float | 0.5–2.0 | Peso en el pool aleatorio |

#### Categorías de Sala por Mecánica Principal

**SYNC — Sincronización de timing:** El jugador debe estar en posición específica cuando el eco ejecuta su trigger.

**TIMING — Ventanas temporales:** La sala tiene elementos dinámicos (plataformas oscilantes, puertas cíclicas) que crean ventanas de acción.

**DEPENDENCY — Cadenas de eco:** Un eco debe haber ejecutado su acción antes de que otro eco pueda ejecutar la suya.

**FRUSTRATION — Eco Frustrado intencional:** La solución requiere que el jugador entienda cuándo un eco va a fallar y use eso a su favor. Solo disponible en Zona 3+.

**SOLO — Sala de un eco:** Salas simples de un solo eco. Sirven como respiro. Obligatorias en posición 1 de cada run.

---

### 7.2 Algoritmo de Ensamblaje Procedural

El algoritmo garantiza que cada run sea diferente pero internamente balanceada. No es puro azar — es azar con restricciones fuertes.

```
FUNCIÓN ensamblar_run(run_number, player_slots, player_zone):

  PASO 1 — Filtrar pool disponible:
    salas_candidatas = todas las salas donde:
      sala.zone_id == player_zone O zona_anterior
      sala.eco_count_required <= player_slots
      sala.intro_run_min <= run_number
      sala.difficulty_tier en rango [max(1, run_tier-2), run_tier+1]
    
    run_tier = MIN(10, FLOOR(run_number / 5) + 1)

  PASO 2 — Forzar restricciones de diversidad:
    sala_1 = elegir de candidatas donde primary_mechanic == SOLO
    sala_2 = elegir de candidatas donde primary_mechanic != SOLO
              Y primary_mechanic != sala_1.primary_mechanic
    sala_3 = elegir de candidatas donde:
              primary_mechanic NOT IN [sala_1, sala_2].primary_mechanics
              Y difficulty_tier > sala_2.difficulty_tier (escala)
    sala_4 = elegir de candidatas donde:
              difficulty_tier >= sala_3.difficulty_tier
              Y has_alt_solution == true (fairness: última sala siempre tiene alt)

  PASO 3 — Verificar duración estimada:
    duración_total = SUMA(sala.estimated_duration_s) para salas 1-4
    SI duración_total > 420s (7 min):
      reemplazar sala_3 con sala de menor difficulty_tier hasta cumplir constraint
    
  PASO 4 — Forzar Regla R1:
    SI sala_1 NO tiene propiedad eco_resuelve_sin_plan:
      buscar sala candidata de dificultad baja con esa propiedad
      intercambiar sala_1

  PASO 5 — Retornar run_config:
    { salas: [sala_1, sala_2, sala_3, sala_4], boss: boss_de_zona, seed: hash_config }
```

**Nota sobre la semilla:** La semilla se genera del hash de los metadatos finales de la run, no del input aleatorio. Esto permite que el nodo D5 (Selector de Semilla) reproduzca runs exactas sin almacenar el estado completo.

---

### 7.3 Lista Completa de Upgrades de Run

Los upgrades de run son modificadores temporales que duran una run. Aparecen como una selección de 2 opciones entre salas (no siempre — probabilidad del 60% [VS] de aparecer en cada transición).

El jugador elige 1 de los 2 presentados o pasa (no elegir no da penalización).

| ID | Nombre | Efecto mecánico | Duración | Sinergias | Notas |
|---|---|---|---|---|---|
| R01 | Eco Veloz | Los ecos de esta run corren al 1.2x | Run completa | R04 | Hace puzzles de timing más difíciles |
| R02 | Eco Lento | Los ecos de esta run corren al 0.8x | Run completa | R06 | Amplía ventanas de sincronización |
| R03 | Bullet Extendido | Transición al regresar a velocidad normal es 0.4s (más suave) | Run completa | — | QoL, no poder |
| R04 | Doble Loop | Los ecos completan su loop dos veces más rápido (sin cambiar velocidad de movimiento — el loop es más corto) | Run completa | R01 | Cambio de frecuencia, no velocidad |
| R05 | Trigger Anticipado | Los Trigger Points se ejecutan 0.3s antes de que el eco llegue físicamente | Run completa | — | Corrige puzzles de timing ajustado |
| R06 | Persistencia Ampliada | Los trails de eco duran 2x más en bullet-time | Run completa | R02 | Mejor lectura visual |
| R07 | Sala Bonus | Se añade una 5ta sala de dificultad baja, recompensa +50% PC | Esta sala | — | Más crystals, más tiempo |
| R08 | Reinicio de Sala | Si el jugador falla una sala (se sale por el trigger equivocado), puede reiniciarla una vez | Único uso | — | Safety net |
| R09 | Eco Duplicado | El primer eco generado se duplica — hay 2 ecos con la misma ruta desde el principio | Run completa | — | Poderoso; solo aparece en runs difíciles |
| R10 | Revelación | Al entrar a cada sala, los trigger points de los ecos futuros se muestran como marcadores por 2s | Run completa | — | Spoiler de solución; para jugadores bloqueados |
| R11 | Mundo Lento | Todo el mundo (físicas ambientales) corre al 0.7x | Run completa | R06 | Facilita timing de sala sin afectar ecos |
| R12 | PC Bonus | +100 Phase Crystals al completar la run (independiente del resultado) | — | — | Recompensa económica pura |

---

## SECCIÓN 8 — SISTEMA DE BOSS

### 8.1 Principio de Diseño de Boss

Los bosses en PHASE son puzzles físicos con múltiples fases que explotan la mecánica de ecos de formas que las salas estándar no pueden. No son enemigos con HP — son estructuras de sala que evolucionan.

**Estructura universal de boss:**
- Fase 1: El jugador usa los ecos que ya tiene (de las 4 salas previas) en la arena del boss
- Fase 2: El boss introduce un elemento nuevo que requiere más ecos o bullet-time
- Fase 3: Síntesis — todos los ecos activos + bullet-time + el elemento nuevo deben coordinarse simultáneamente

**Condición de victoria universal:** El jugador activa el Trigger Final del boss mientras todos los elementos requeridos están activos simultáneamente.

**Condición de fallo:** No existe fallo de boss en el sentido tradicional. Si el jugador no puede resolver la sala, puede salir (abandona la run) o permanecer indefinidamente. No hay timer de boss. No hay HP del jugador. El boss espera.

**Por qué no hay fallo:** La frustración de "morir" en un juego de puzzles interrumpe el estado de flujo cognitivo. El jugador que abandona una run decide hacerlo; no es expulsado.

---

### 8.2 BOSS 1 — "El Espejo Fragmentado" (Zona 1)

**Contexto narrativo visual:** Una sala circular rodeada de espejos. El boss no es una criatura — es el propio reflejo del jugador, fragmentado en múltiples espejos que se mueven hacia el centro.

**Elemento central de sala:** 5 paneles de espejo en posiciones fijas, cada uno con un mecanismo de palanca frente a él. Cuando una palanca está activa, el panel de espejo se vuelve transparente. Cuando todos los paneles son transparentes, el centro de la sala se ilumina y el Trigger Final aparece.

**Layout de sala:**
```
     [E1]
  [E5]   [E2]
    [CENTER]
  [E4]   [E3]
     —
  (entrada)
```

#### FASE 1 — "Primeros Reflejos"

**Ecos disponibles:** Los 2 ecos que el jugador generó en las salas 1 y 2 de esta run.

**Comportamiento del boss:** Los paneles de espejo oscilan lentamente (periodo: 8s [VS]) entre posición A (frente a su palanca) y posición B (lejos). Las palancas solo se pueden activar cuando el panel está en posición A.

**Objetivo:** Usar los 2 ecos para activar los paneles E1 y E2 mientras el jugador activa E3.

**Por qué funciona:** Los paths de los ecos existentes pasan naturalmente cerca de E1 y E2 porque el algoritmo de sala de run los coloca así. El jugador debe observar cuándo sus ecos cruzan las palancas en el timing correcto del oscilador.

**Resolución:** Los 3 primeros paneles se vuelven transparentes. La sala se ilumina parcialmente. Aparecen paneles E4 y E5 (antes inactivos). Transición de 2s. Fase 2 comienza.

#### FASE 2 — "Multiplicación"

**Ecos disponibles:** Ecos 1 y 2 (previos) + el eco generado en la Sala 3 de esta run.

**Elemento nuevo:** Los paneles E4 y E5 tienen un mecanismo de contrapeso: si el jugador activa E4, E2 se desactiva. El jugador no puede activar ambos directamente.

**Objetivo:** Usar los 3 ecos para mantener E1, E2, y E4 activos mientras el jugador activa E3 y E5 en la ventana temporal correcta.

**La solución implícita:** El Eco 3 (de Sala 3) debe tener un path que pase por E4 en el momento preciso en que el contrapeso reseteó E2 para que el Eco 2 pueda reactivarlo. Esto requiere bullet-time para leer el timing.

**Resolución:** 5 paneles activos. Centro de sala completamente iluminado. Trigger Final aparece en el centro. Transición de 1s. Fase 3 comienza.

#### FASE 3 — "La Convergencia"

**Ecos disponibles:** Todos los ecos activos del jugador (2 a 5, dependiendo de meta-progresión).

**Elemento nuevo:** El Trigger Final del boss solo puede activarse mientras el jugador está parado en el centro exacto de la sala Y todos los paneles están activos. Pero el centro de la sala es pequeño (1 tile). El jugador debe llegar al centro, pararse, y esperar que todos los paneles sean activados por sus ecos simultáneamente.

**El movimiento inverso:** El jugador normalmente hace cosas y los ecos repiten. En Fase 3, el jugador llega a su posición final y espera que sus ecos hagan el trabajo por él. Es la realización del tagline: "Tu pasado ya sabe la respuesta."

**Condición de victoria:** Jugador en tile central + todos los paneles activos (duración mínima: 1s continuo [VS]).

**Animación de victoria:** Los espejos explotan en una lluvia de partículas de cristal. Los ecos corren hacia el jugador y pasan a través de él. La sala se abre. Fundido a negro. Pantalla de fin de run.

---

### 8.3 Escalado de Bosses por Zona

| Zona | Boss | Elemento Central | Complejidad Fase 3 | Ecos necesarios mínimo |
|---|---|---|---|---|
| Z1 | El Espejo Fragmentado | Paneles de espejo oscilantes | 2 ecos simultáneos activos | 2 |
| Z2 | La Fractura | Plataformas que colapsan secuencialmente; el jugador debe reconstruir el camino con ecos | Cadena de dependencias: Eco A activa para que Eco B pueda pasar | 3 |
| Z3 | El Abismo | La sala se "desintegra" — partes del suelo desaparecen con timers; los ecos deben activar palancas en el orden correcto para reconstruirlas antes de que el jugador pise | Timing crítico: ventana de 2s [VS] para cada activación | 3 |
| Z4 | La Resonancia | Frecuencias visuales que bloquean partes de la sala; los ecos de distintos slots emiten frecuencias diferentes; combinar 3 ecos en posiciones específicas desactiva el bloqueo | Sincronización espacial: 3 ecos en tiles específicos simultáneamente | 4 |
| Z5 | La Convergencia Final | El boss es la sala tutorial completa, reseteada, con todos los mecanismos activos simultáneamente, a velocidad 1.5x | Toda la progresión del jugador se pone a prueba en una sola sala | 5 |

**Nota sobre Zona 5:** El boss final es intencionalmente el tutorial recontextualizado. El jugador que llegó a la Run 40+ conoce esa sala íntimamente. Verla a 1.5x con 5 ecos activos es una prueba de maestría, no de aprendizaje nuevo. La dificultad está en la ejecución, no en la comprensión.

---

## SECCIÓN 9 — ECONOMÍA Y MONETIZACIÓN

### 9.1 Phase Crystals — Moneda Única

Phase Crystals (PC) son la única moneda del juego. No hay moneda premium separada. No hay conversión de dinero real a PC.

**Principio:** El jugador que juega consistentemente puede alcanzar toda la progresión de gameplay sin pagar nada. El dinero real compra comodidad (sin anuncios), cosméticos, y tiempo ahorrado.

#### Cómo se Ganan Phase Crystals

| Fuente | Cantidad | Condición | Notas |
|---|---|---|---|
| Completar run (Zona 1) | 30 PC | Base | Independiente del tiempo |
| Completar run (Zona 2) | 45 PC | Base | |
| Completar run (Zona 3) | 60 PC | Base | |
| Completar run (Zona 4) | 80 PC | Base | |
| Completar run (Zona 5) | 100 PC | Base | |
| Run perfecta (sin Eco Frustrado) | +20 PC | Bonus | Cualquier zona |
| Récord personal de tiempo | +15 PC | Bonus | Primera vez que bates tu récord |
| Primera run del día | +25 PC | Daily bonus | Resetea a medianoche local |
| Ver anuncio recompensado | +10 PC | Opcional | Máximo 3 por día |
| Completar desafío semanal | 150 PC | Semanal | Un desafío de diseño especial por semana |
| Milestone de runs (runs 10, 25, 50) | 100 / 200 / 400 PC | Una vez | Celebración de hito |

#### Proyección de Ingresos de PC

Un jugador promedio de 2 runs diarias en Zona 1-2 gana aproximadamente:
- 2 runs × 37.5 PC promedio = 75 PC/día
- Daily bonus: +25 PC
- Anuncios (promedio 1.5 por día): +15 PC
- **Total estimado: ~115 PC/día**

A este ritmo, el tercer slot (150 PC) se consigue en aproximadamente 1.3 días de juego consistente. El cuinto slot (acumulado ~950 PC en el árbol) se consigue en ~8 días. Esto es intencional: la meta-progresión de capacidad (slots de eco) no debe ser una barrera larga.

---

### 9.2 Monetización Ética

PHASE tiene tres vías de monetización. Ninguna de ellas es pay-to-win.

#### VÍA 1 — Modo Sin Anuncios (pago único)

**Precio:** $3.99 USD (equivalente a 500 PC si se compraran por anuncios)

**Efecto:** Elimina todos los anuncios recompensados del juego. El jugador sigue ganando PC por gameplay.

**Nota:** Los anuncios recompensados en PHASE son siempre opcionales. Nunca hay anuncio intersticial ni anuncio de "espera X segundos para continuar". El jugador que no paga ve la opción "Ver anuncio: +10 PC" en la pantalla de fin de run. Siempre puede ignorarla.

#### VÍA 2 — Skins de Ecos (micropagos opcionales)

Los skins C4, C7, C8, y C10 (marcados como "Premium" en el árbol) también están disponibles para compra directa:

| Skin | Precio IAP | Equivalente en PC gratis |
|---|---|---|
| Partículas de Cristal (C4) | $0.99 | 120 PC (~1 día de juego) |
| Plasma Temporal (C7) | $1.49 | 150 PC (~1.3 días) |
| Espectro de Luz (C8) | $1.99 | 180 PC (~1.5 días) |
| Arco Iris Cuántico (C10) | $2.49 | 200 PC (~1.7 días) |

**Principio:** El precio IAP siempre debe ser menor o igual al tiempo que tardaría un jugador promedio en ganarlo. La compra es por comodidad, no por exclusividad.

#### VÍA 3 — Season Pass "Frecuencia" (opcional, trimestral)

**Precio:** $4.99 USD / trimestre

**Contenido:**
- 5 skins de eco exclusivas del trimestre (cosméticas, no recompra)
- 1 modificador de run exclusivo por trimestre (regresa al pool permanente al siguiente trimestre)
- Desafíos semanales con recompensas dobles
- Badge cosmético de perfil del trimestre

**Lo que NO incluye el Season Pass:** No da PC extra. No da slots de eco. No da poder de gameplay. Una persona que no compra el Season Pass puede hacer exactamente las mismas cosas mecánicamente.

---

### 9.3 Proyección LTV

| Perfil de Jugador | Gasto estimado a 6 meses | Lógica |
|---|---|---|
| Gratis puro | $0 | Ve anuncios, gana PC, no gasta |
| Sin Anuncios | $3.99 | Pago único, no repite |
| Coleccionista casual | $8–15 | Sin anuncios + 2-3 skins |
| Season Pass único | $4.99 | Un trimestre |
| Jugador comprometido | $18–30 | Sin anuncios + 1-2 season passes |
| Entusiasta | $40–60 | Sin anuncios + 4 season passes + skins selectas |

**LTV promedio estimado (todas las cohortes):** $4.50 [VS — validar con datos de primera cohorte de 1000 usuarios]

**Nota de rentabilidad:** Para un estudio pequeño, PHASE no apunta a ser un top-grossing. Apunta a ser un juego con retención alta (sesiones de 5-8 min, 2-3 por día) y monetización de bajo conflicto que sostenga el proyecto sin sacrificar la experiencia de juego.

---

### 9.4 Qué NUNCA Habrá en PHASE

Estas son restricciones de producción, no aspiraciones. Si una feature de monetización entra en conflicto con esta lista, se rechaza sin discusión.

- Anuncios intersticiales (que aparecen sin que el jugador los solicite)
- Anuncios que interrumpen una run activa
- Energía o vidas limitadas que bloqueen el acceso al juego
- Slots de eco que solo se consigan con dinero real (todos están en el árbol de PC)
- Mecánicas pay-to-win de ningún tipo (no hay modo ranked, no hay ventaja estadística de pago)
- Loot boxes o gacha (todos los cosméticos tienen precio conocido)
- Urgencia artificial (ofertas de "¡Solo hoy!")
- Notificaciones push agresivas

---

## SECCIÓN 10 — BALANCE Y DIFICULTAD

### 10.1 Dificultad Adaptativa Sin que el Jugador lo Note

PHASE no tiene un selector de dificultad explícito. La dificultad se ajusta a través del algoritmo de ensamblaje de run (Sección 7.2) usando métricas pasivas del jugador.

#### Sistema de Calibración Pasiva

El sistema registra silenciosamente:

| Métrica | Lectura | Acción si está bajo umbral |
|---|---|---|
| Tasa de completitud de run | % runs completadas vs intentadas | Si < 40% en últimas 5 runs: bajar `run_tier` en 1 |
| Tiempo promedio de sala | Segundos por sala | Si promedio > 90s: priorizar salas `has_alt_solution: true` |
| Uso de bullet-time | % de tiempo en bullet-time | Si < 5% en últimas 3 runs: introducir sala con obstáculo dinámico (induce uso de BT) |
| Ecos frustrados por run | Promedio de frustrations/run | Si > 3: reducir `primary_mechanic: DEPENDENCY` en el pool temporalmente |
| Runs consecutivas abandonadas | Número | Si >= 3 seguidas: forzar sala de Tier bajo en posición 1 de próxima run |

El jugador nunca ve estas métricas. No hay barra de "dificultad actual". No hay mensaje de "el juego ajustó la dificultad para ti".

**Nota de diseño:** La adaptación es un safety net, no un elevator. Si el jugador está teniendo éxito, el sistema no sube la dificultad artificialmente. Solo baja cuando el jugador tiene problemas. La progresión natural de la curva de dificultad (Sección 6) maneja el incremento.

---

### 10.2 Tabla de Balance de Upgrades de Run

Los upgrades de run tienen un valor de balance medido en dos dimensiones: **impacto en dificultad** (cuánto facilita resolver las salas) y **cambio en complejidad** (cuánto cambia el espacio de posibilidades del jugador).

| Upgrade | Impacto Dificultad | Cambio Complejidad | Probabilidad Aparición Base [VS] | Notas |
|---|---|---|---|---|
| R01 Eco Veloz | -1 (más difícil) | Bajo | 15% | No presentar antes de Run 5 |
| R02 Eco Lento | +2 (más fácil) | Bajo | 10% | |
| R03 Bullet Extendido | +1 | Ninguno | 20% | Siempre seguro de presentar |
| R04 Doble Loop | -1 a +1 (depende) | Medio | 12% | Solo presentar si jugador domina timing |
| R05 Trigger Anticipado | +2 | Bajo | 15% | Buen safety net |
| R06 Persistencia Ampliada | +1 | Ninguno | 20% | Siempre seguro |
| R07 Sala Bonus | 0 | Ninguno | 10% | Preferir cuando PC bonus es relevante |
| R08 Reinicio de Sala | +3 | Ninguno | 8% | Solo presentar si tasa de completitud < 60% |
| R09 Eco Duplicado | +3 | Alto | 5% | Raro; potente |
| R10 Revelación | +4 | Ninguno | 7% | Solo presentar si hay señales de bloqueo |
| R11 Mundo Lento | +2 | Bajo | 12% | |
| R12 PC Bonus | 0 | Ninguno | 12% | Neutro; siempre aceptable |

**Regla de presentación de upgrades:** El sistema nunca presenta dos upgrades del mismo tipo de impacto en la misma transición. Si se sortea un upgrade de impacto +3 o más, el upgrade alternativo siempre es uno de impacto 0 o negativo (para dar opción al jugador que quiere el reto).

---

### 10.3 Reglas de Fairness en Diseño de Sala

Estas son reglas de diseño que aplican a cada sala del juego, sin excepción. Son el contrato implícito con el jugador.

**REGLA F1 — La solución es siempre visible en bullet-time**
Toda sala tiene al menos una solución que el jugador puede anticipar completamente si observa el estado de la sala en bullet-time durante 5 segundos. No hay información oculta que requiera memoria o suerte.

**REGLA F2 — El comportamiento del eco es consistente**
Un eco que en la Sala 1 activa palancas al pasar sobre ellas, también activa palancas al pasar sobre ellas en la Sala 4. No hay excepciones de comportamiento sin introducción explícita.

**REGLA F3 — Ninguna sala de Zonas 1-2 tiene solución única**
Toda sala de dificultad Tier 1-5 tiene al menos dos rutas de solución válidas. El jugador no debe sentir que "solo había una forma correcta" en las primeras zonas.

**REGLA F4 — Los ecos no traicionan**
Si el eco va a ejecutar una acción en un momento determinado, esa acción es predecible con observación. No hay aleatorización de timing de ecos. Un eco que ha completado un loop siempre lo completa en el mismo tiempo.

**REGLA F5 — La sala se puede resolver siempre**
Ninguna sala tiene un estado de bloqueo permanente. Si el jugador hace algo "incorrecto" (activa una palanca equivocada), debe existir una forma de deshacer ese estado o la sala debe ser solucionable de todas formas. Salas que requieren reinicio explícito (reset de sala) no pueden aparecer antes de Zona 3.

---

### 10.4 Cómo Evitar Frustración Cuando el Eco No Hace Lo Esperado

Este es el mayor riesgo de experiencia de usuario del juego. El jugador que no entiende por qué su eco no hizo lo que esperaba abandona la run.

#### Protocolo de Comunicación de Fallo de Eco

El sistema tiene tres niveles de comunicación cuando un eco se frustra o no hace lo esperado:

**Nivel 1 — Feedback inmediato (siempre activo):**
- La animación de frustración de eco (0.5s de vibración + destello rojo) es clara y específica
- El eco continúa su loop — el jugador ve que no "murió", solo "no pudo"
- El objeto que el eco intentó interactuar parpadea en rojo por 0.3s (confirma qué objeto era el problema)

**Nivel 2 — Asistencia pasiva en bullet-time:**
- En bullet-time, los Trigger Points futuros del eco se muestran como marcadores semitransparentes
- Los Trigger Points en estado FRUSTRADO (el objeto está en el estado incorrecto) se muestran en rojo
- El jugador puede ver visualmente "mi eco va a intentar activar eso, y ese objeto está en el estado equivocado"

**Nivel 3 — Pista implícita de diseño de nivel (si los niveles 1 y 2 no resuelven el problema en 45s [VS]):**
- El objeto que el eco necesita para dejar de frustrarse comienza a pulsar suavemente (luz amarilla, ciclo de 2s)
- Esto no dice "activa esto". Dice "este objeto es relevante".
- La pista desaparece si el jugador interactúa con el objeto pulsante (ya sabe que es relevante).

**Nunca:**
- No hay flecha que diga "ve aquí"
- No hay texto de instrucción
- No hay modo automático de solución
- No hay penalización por tardar

**Filosofía de frustración:** Un poco de confusión es parte del diseño. La confusión que termina en comprensión es satisfacción. La confusión que no termina nunca es frustración. El sistema interviene en el punto en que la confusión se está convirtiendo en abandono, no en el punto en que comienza.

---

*Fin de la Primera Mitad del GDD — Secciones 1-10*

*Siguiente documento: GDD-PHASE-parte2.md*  
*Cubre: Diseño de Niveles, Arte y Dirección Visual, Audio, Sistemas Técnicos, Localización, Pipeline de QA, Roadmap de Producción*
