# FASE 10 — DESARROLLO COMPLETO
## Proyecto: PHASE

**Fecha:** 2026-09-02
**Entrada:** Vertical Slice validado — el eco genera el momento "aha", el bullet-time se lee como poder y no como bug, todos los criterios GO de la Fase 9 se cumplieron.
**Salida:** PHASE completo, listo para entrar a QA.

---

## Qué se construye en esta fase

El Vertical Slice tenía 1 sala hecha a mano, 1 slot de eco, 1 hazard estático y ningún sistema de meta-juego. El juego completo tiene 5 slots de eco, un pool de 50 salas repartidas en 3 zonas, 3 bosses de 3 fases, un árbol de meta-progresión de 24 nodos, economía de Phase Crystals, sistema de runs procedurales, y monetización ética (sin anuncios, skins, season pass).

Esta fase se organiza en **5 milestones** secuenciales. El orden protege el núcleo: no se construye contenido de sala (M2) hasta que el sistema de ecos soporte 5 slots reales y persista datos (M1); no se activa monetización (M5) hasta que el juego tenga suficiente contenido para justificarla.

---

## Milestone 1 — Núcleo de Producción (2.5 semanas)

**Objetivo:** Los sistemas que todo lo demás necesita. El VS probó 1 eco; M1 escala esos mismos sistemas a los 5 slots reales del GDD, con persistencia real.

### 1.1 — EchoManager a 5 slots con pool completo

El VS usó 1 `EchoPlayer` instanciado directo. El juego completo necesita el pool descrito en Fase 8 §17.2.4 (10 objetos pre-instanciados: 5 activos + 5 en reserva para transición sin GC spike).

```csharp
// Extensión de EchoManager.cs (Fase 8 §5.2) para producción:
EchoManagerImpl:
  - _maxEchos parte en 2 (A1, gratis) y escala hasta 5 vía ProgressionSystem.IsSlotUnlocked(n)
  - ShiftEchos() ahora debe animar la muerte del eco más viejo (fade 0.5s) antes de reciclarlo al pool,
    no destruirlo instantáneamente — el VS lo hacía sin transición porque no importaba con 1 slot
  - EchoColors (Fase 8) ya está definida — no cambia, es la tabla canónica de 5 colores
  - Nodo A5 "Persistencia de Eco" (GDD §4.1): cuando está comprado, ShiftEchos() no se dispara al fallar
    una sala — los ecos persisten entre reintentos de la MISMA sala únicamente
```

### 1.2 — RunManager (FSM) — no existía en el VS

El VS reseteaba la sala directamente al morir. El juego completo necesita el estado de run completo.

```csharp
public enum RunState { Idle, RoomTransition, RoomActive, UpgradeChoice, BossFight, RunComplete, RunFailed }

RunManagerImpl (FSM):
  - Idle → RoomTransition: al confirmar semilla en Selección de Run (Fase 7.2 algoritmo)
  - RoomTransition → RoomActive: tras la animación de Pantalla de Eco (2s, fija)
  - RoomActive → UpgradeChoice: entre salas, con 60% [VS] de probabilidad (GDD §7.3)
  - RoomActive → BossFight: al completar la 4ta sala estándar
  - BossFight → RunComplete: al activar el Trigger Final del boss (Fase 8 §8, todas las fases)
  - Cualquier estado → RunFailed: el jugador sale manualmente de la run (no hay fallo por HP, GDD §8.1)
  - RunComplete/RunFailed → Idle: tras Pantalla de Fin de Run
```

**Checkpoint rolling (deuda de la Fase 4 §15.3):** el estado de run activo se escribe a disco cada 30s. Si el juego crashea a mitad de run, al relanzar se ofrece "Continuar run" o "Empezar nueva" — la run interrumpida no otorga Phase Crystals.

### 1.3 — SaveSystem real (JSON dual local + nube)

El VS no tenía save. Implementar la estructura exacta de Fase 4 §15.2 (`meta_progression`, `achievements`, `accessibility_prefs`, `run_history`).

```csharp
SaveSystemImpl:
  - Load() al arrancar Persistent.unity
  - Save() en OnApplicationPause + cada checkpoint de RunManager (§1.2) + al cerrar meta-progresión
  - Backend de nube: decisión diferida en GDD §19.4 sigue pendiente — para M1 se implementa
    SOLO el save local; el hook de sincronización de nube se deja como interfaz (ICloudSync)
    con un stub que no hace nada, para no bloquear M1 con una decisión de backend no tomada
  - Conflict resolution (Fase 4 §15.3) se implementa cuando ICloudSync tenga un backend real
```

**Nota de deuda:** GDD §19.4 dejó "backend de nube: Supabase o Firebase" sin decidir. Este milestone NO resuelve esa deuda — solo evita que bloquee el resto de producción. Decisión de backend requerida antes de M5 (monetización de skins necesita sync entre dispositivos).

### 1.4 — ProgressionSystem (Phase Crystals + árbol)

```csharp
ProgressionSystemImpl:
  - PhaseCrystalBalance: int, persistido
  - EarnCrystals(amount, source): aplica las tablas de GDD §9.1 (completar run por zona, run perfecta,
    récord personal, daily bonus, anuncio recompensado, desafío semanal, milestones de runs)
  - SpendCrystals(amount, nodeId): false si balance insuficiente
  - IsNodeUnlocked(nodeId) / IsSlotUnlocked(n): recorre el árbol de 24 nodos (Ramas A-D, GDD §4.1)
  - Validación de dependencias: un nodo con requisito previo (ej. A3 requiere A2) no es comprable
    si el requisito no está desbloqueado — UI debe reflejar esto como "bloqueado", no ocultar el nodo
```

### 1.5 — Test de regresión del VS

Antes de avanzar a M2, correr todos los criterios de Go del VS (Fase 9 §7) de nuevo sobre el M1 recién construido:
- El bullet-time se sigue sintiendo como poder, no como lag
- Los ecos con 5 slots activos no rompen el framerate (55fps mínimo, dispositivo de referencia Snapdragon 685)
- El eco frustrado (GDD §2.4) sigue comunicándose sin texto

**Si algo regresiona aquí, se arregla antes de M2. Sin excepciones.**

---

## Milestone 2 — Pool de Salas y Zonas 1–3 (3.5 semanas)

**Objetivo:** 50 salas reales que superan el checklist de calidad de GDD §18.1, repartidas en las 3 zonas de lanzamiento (GDD §16.4 fija el alcance de lanzamiento en 3 zonas, no las 5 de la curva de dificultad completa — ver Autocrítica).

### 2.1 — Herramienta de metadatos de sala

Antes de diseñar salas a mano, construir el ScriptableObject que el algoritmo de ensamblaje (Fase 8 §17.4.1) necesita:

```csharp
[CreateAssetMenu(menuName = "PHASE/Room")]
public class RoomData : ScriptableObject
{
    public string roomId;              // "ZONA1_SALA_012"
    public int zoneId;                 // 1-3 en lanzamiento
    public int difficultyTier;         // 1-10 (GDD §7.1)
    public PrimaryMechanic mechanic;   // SYNC, TIMING, DEPENDENCY, FRUSTRATION, SOLO
    public int ecoCountRequired;       // 1-5
    public float estimatedDurationS;   // 20-120
    public bool hasAltSolution;
    public int introRunMin;
    public float weightBase;
    public DoorPosition entrada, salida;
}
```

Esta herramienta se construye ANTES que la primera sala de contenido — sin ella, el algoritmo de la Fase 8 §17.2 no tiene qué ensamblar y cada sala nueva se probaría de forma aislada, no dentro del contexto real de una run.

### 2.2 — Salas Zona 1 — Umbral (18 salas)

Tema visual: sala de espejos, reflejos (GDD §6.2). Perfil de dificultad: introducción, ecos básicos, 1-2 slots.

- 4 salas `SOLO` (posición 1 obligatoria de cada run, GDD §7.1)
- 6 salas `SYNC` (sincronización de timing básica)
- 5 salas `TIMING` (ventanas temporales, plataformas oscilantes)
- 3 salas `DEPENDENCY` (2 ecos coordinados, introducidas hacia el final de la zona)

Cada sala pasa el checklist completo de GDD §18.1 antes de integrarse al pool: resoluble en ≤2 intentos por un QA tester sin contexto previo, resoluble sin bullet-time, ningún estado de bloqueo irrecuperable, crear al menos 1 eco es obligatorio para resolver.

### 2.3 — Salas Zona 2 — Fracturas (17 salas)

Tema visual: vidrio roto, geometría irregular. Perfil: timing, ecos coordinados, 2-3 slots.

- Introduce el primer uso real de `DEPENDENCY` como mecánica central (Eco 1 abre para que Eco 2 pase, GDD §6.1 "segunda pared de aprendizaje")
- Meseta de 2-3 salas de introducción gradual antes de escalar dificultad — este es el salto conceptual más grande del juego según el GDD; no comprimirlo

### 2.4 — Salas Zona 3 — Abismo (15 salas, incluye la introducción de Eco Frustrado como mecánica)

Tema visual: espacio-tiempo distorsionado, negro y púrpura. Perfil: Eco Frustrado intencional, dependencias, 3-4 slots.

- Regla de fairness F1-F5 (GDD §10.3) aplica sin excepción, pero la Zona 3 es la primera donde Eco Frustrado puede ser LA solución (GDD §2.4, "Regla de Fairness: ninguna sala de Runs 1-5 puede tener Eco Frustrado como solución deliberada")
- Primera zona donde una sala puede requerir reset explícito de sala (F5 lo prohíbe antes de Zona 3)

### 2.5 — Validación de fairness cruzada

Con las 50 salas construidas, correr el algoritmo de ensamblaje (Fase 8 §17.4.2) 500 veces con semillas aleatorias y verificar:
- Ninguna run generada supera los 420s (7 min) de duración estimada total (regla de la Fase 4 §7.2 paso 3)
- La sala 1 de cada run generada cumple la Regla R1 (GDD §7.2 paso 4: eco resuelve sin plan)
- La sala 4 (última antes del boss) siempre tiene `has_alt_solution: true`

---

## Milestone 3 — Meta-Progresión Completa (2.5 semanas)

**Objetivo:** El árbol de 24 nodos, la economía de Phase Crystals, y los 12 upgrades de run, todos jugables y balanceados.

### 3.1 — Árbol de Meta-Progresión — UI

Implementar el árbol de nodos conectados descrito en GDD §11.5.5: nodos visibles aunque bloqueados (outline gris), disponibles (outline de color por rama), comprados (relleno). Las 4 ramas (GDD §4.1):

| Rama | Nodos | Qué desbloquea |
|---|---|---|
| A — Capacidad de Ecos | 6 | Slots 3-5, persistencia de eco, memoria de ruta |
| B — Modificadores de Run | 8 | Run Limpia (default) + 7 modificadores de dificultad/especialización |
| C — Cosméticos de Eco | 10 | Skins visuales, sin efecto en gameplay |
| D — Calidad de Vida | 5 | Historial, sin anuncios, skip tutorial, animaciones rápidas, selector de semilla |

**Nota de deuda (GDD §19.4):** "24 es estimación; se revisará tras probar curva de progresión" — mantener el árbol en un ScriptableObject editable, no hardcodear la cuenta de nodos en ningún lado del código.

### 3.2 — El Tercer Slot — Secuencia cinemática memorable

Implementar la secuencia específica del GDD §6.3: al comprar A2 (Tercer Espejo), NO ir directo a selección de run. Reproducir la cinemática de 8s (saltable tras 3s) en la sala de Tutorial 1 reutilizada — Eco 1 y Eco 2 entran desde direcciones opuestas, un tercer eco cae del techo, los tres se alinean mirando al jugador, flash + "3" en pantalla.

**Por qué se prioriza en M3 y no se deja para "pulido":** el GDD es explícito en que este es "el desbloqueo más importante del juego" — tratarlo como polish tardío arriesga que se recorte por presión de tiempo cuando es, según el propio documento de diseño, el momento emocional central de la meta-progresión.

### 3.3 — Upgrades de Run (R01–R12)

Implementar los 12 modificadores de la GDD §7.3 con su tabla de probabilidad de aparición (GDD §10.2). La regla de presentación (nunca dos upgrades del mismo signo de impacto en la misma transición) se implementa en el selector de 2 opciones:

```csharp
UpgradeSelector.Present():
  - upgrade_a = weighted_random(pool, exclude=[])
  - upgrade_b = weighted_random(pool, exclude=[upgrade_a],
                  constraint: sign(upgrade_b.impact) != sign(upgrade_a.impact) if abs(upgrade_a.impact) >= 3)
```

### 3.4 — Calibración pasiva de dificultad (GDD §10.1)

Implementar el sistema de calibración silenciosa: tasa de completitud, tiempo promedio de sala, % de uso de bullet-time, ecos frustrados por run, runs abandonadas consecutivas. Ninguna de estas métricas se muestra al jugador — solo ajustan `run_tier` y el pool de salas candidatas del algoritmo de ensamblaje.

### 3.5 — Validación de balance con playtesting interno

Con el árbol completo, correr 10 sesiones internas de "jugador simulado" (o testers reales si hay disponibilidad) desde Run 1 hasta Run 15 y confirmar que el ritmo de adquisición de Crystals coincide con la proyección del GDD §9.1 (~115 PC/día promedio, tercer slot en ~1.3 días).

---

## Milestone 4 — Los 3 Bosses de Lanzamiento (2 semanas)

**Objetivo:** Los bosses de Zona 1, 2 y 3, cada uno con sus 3 fases funcionando según la especificación de Fase 8 §8.

### 4.1 — Boss Z1 "El Espejo Fragmentado"

Ya especificado en detalle en Fase 8 §8.2 (layout de 5 paneles, 3 fases, contrapesos). Implementación directa desde ese documento — es el boss de referencia que valida el patrón universal de boss (Fase 8 §8.1) antes de construir los otros dos.

### 4.2 — Boss Z2 "La Fractura"

Plataformas que colapsan secuencialmente. El jugador reconstruye el camino con ecos.

```
Fase 1: 2 ecos disponibles — Eco A debe activar una palanca para que una plataforma
        se mantenga estable el tiempo suficiente para que el jugador cruce
Fase 2: 3 ecos — cadena de dependencias: Eco A activa para que Eco B pueda pasar,
        y el jugador debe llegar detrás de ambos
Fase 3: síntesis — el jugador reconstruye la ruta completa mientras los 3 ecos
        mantienen las plataformas estables simultáneamente
```

Complejidad Fase 3 (GDD §8.3): cadena de dependencias, mínimo 3 ecos.

### 4.3 — Boss Z3 "El Abismo"

La sala se desintegra: partes del suelo desaparecen con timers. Los ecos deben activar palancas en el orden correcto para reconstruirlas antes de que el jugador pise.

```
Ventana de timing crítico: 2s [VS] por activación (heredado del GDD, validar en QA interno M4)
Fase 3: todos los ecos activos coordinando reconstrucción de suelo bajo presión de tiempo real
```

Complejidad Fase 3: timing crítico, mínimo 3 ecos.

**Nota:** Z4 "La Resonancia" y Z5 "La Convergencia Final" están especificados en el GDD (§8.3) pero NO entran en este milestone — pertenecen a Zona 4 y 5, fuera del alcance de lanzamiento v1.0 (ver Autocrítica, y GDD §16.4 roadmap).

### 4.4 — Validación de boss sin fallo

Confirmar el principio de GDD §8.1: no existe condición de fallo de boss. QA interno debe intentar activamente "romper" cada boss dejándolo a medio resolver y verificar que el jugador puede simplemente salir de la run sin quedar atrapado en un estado inconsistente.

---

## Milestone 5 — Monetización y Plataforma (2.5 semanas)

**Objetivo:** El juego puede publicarse. Monetización ética funcional, builds limpios para Android e iOS.

### 5.1 — Modo Sin Anuncios (IAP único, $3.99)

```csharp
// EconomySystem — vía 1 de monetización (GDD §9.2)
PurchaseAdRemoval():
  - Unity IAP (Google Play Billing 6.x / Apple StoreKit 2, misma API abstraída)
  - OnPurchaseCompleted: SaveData.adsRemoved = true
  - Los anuncios recompensados (+10 PC, máx. 3/día) desaparecen de la pantalla de fin de run
  - El jugador sigue ganando PC por gameplay exactamente igual — el IAP no toca el balance
```

### 5.2 — Skins Premium (C4, C7, C8, C10)

Las 4 skins marcadas "Premium" en el árbol (GDD §9.2 vía 2) obtienen precio de compra directa además de su costo en Crystals — nunca exclusivas de dinero real, el árbol de PC siempre es la vía gratuita.

```
Partículas de Cristal (C4): $0.99 / 120 PC
Plasma Temporal (C7):       $1.49 / 150 PC
Espectro de Luz (C8):       $1.99 / 180 PC
Arco Iris Cuántico (C10):   $2.49 / 200 PC
```

Regla de implementación: el precio IAP nunca puede fijarse por encima del tiempo estimado de farmeo (GDD §9.2 principio explícito) — cualquier ajuste de precio post-lanzamiento debe revalidar contra la proyección de PC/día vigente en ese momento.

### 5.3 — Season Pass "Frecuencia" ($4.99/trimestre)

```csharp
SeasonPassSystem:
  - 5 skins de eco exclusivas del trimestre (cosméticas)
  - 1 modificador de run exclusivo del trimestre (Rama B temporal, regresa al pool permanente después)
  - Desafíos semanales con recompensa doble mientras el pase está activo
  - NO otorga PC extra, NO otorga slots, NO otorga ventaja de gameplay — validar explícitamente
    en QA que un usuario sin Season Pass puede completar el 100% del contenido mecánico
```

### 5.4 — Pantalla de Tienda / Colección de Skins

Implementar "MIS ECOS" (GDD §4.2): 5 slots de eco, cada uno con skin asignable, cambiable fuera de una run. Las skins de logro (Eco Dorado, Eco de Errores, Eco Clásico SNES, Eco Primordial — GDD §4.2) no aparecen en la tienda; se desbloquean por evento de logro y se marcan visualmente como "no disponible para compra".

### 5.5 — Backend de nube (resuelve la deuda de M1.3)

Ahora que hay monetización con estado (skins compradas, Season Pass activo), la sincronización de nube deja de ser opcional. Decisión de backend requerida aquí, no antes — implementar `ICloudSync` real (Supabase o Firebase, la deuda del GDD §19.4 se resuelve en este punto de producción, no antes).

### 5.6 — GDPR y accesibilidad de lanzamiento

- Pantalla de consentimiento de datos (Fase 4 §15.4) antes de cualquier sync de nube
- Exportación de datos / borrado de cuenta funcionales desde Settings → About
- Modo daltónico (3 variantes, GDD §14.2) probado con testers daltónicos reales, no solo con simuladores de shader
- Auditoría de tap targets ≥44pt con Accessibility Scanner (Android) / Accessibility Inspector (iOS)

### 5.7 — Builds de release

**Android:** AAB firmado con keystore de producción (no el de debug), IL2CPP + ARM64, target Google Play. Budget de assets validado contra la tabla de Fase 8 §17.5 (100MB total).

**iOS:** Archive en Xcode con Distribution Certificate, subido vía App Store Connect, TestFlight para beta externa antes de submit.

---

## Checklist de Arte — Todo lo que produce en esta fase

El arte se especificó en Fase 6. En Fase 10 se implementa. Checklist basado en Fase 8 §13:

### Spritesheets
```
□ Jugador — 16×16px base, sheet completo: Idle(4f), Run(6f), Salto(3f), Caída(2f), Aterrizaje(3f),
  Creación de eco(5f), Daño(4f), Muerte(7f), Bullet-time idle(4f), Bullet-time run(6f), Interactuar(3f)
□ Ecos — reutilizan el spritesheet del jugador con shader de tinte por color de slot (ahorro de memoria
  confirmado en GDD §13.3: 5× sprites reducido a 1× con variación por shader)
□ Tileset Zona 1 — Umbral: piso, pared, borde, prop decorativo, elemento interactivo (espejos)
□ Tileset Zona 2 — Fracturas: mismo set adaptado a vidrio roto / geometría irregular
□ Tileset Zona 3 — Abismo: mismo set adaptado a espacio-tiempo distorsionado, negro/púrpura
```

### Shaders URP
```
□ Echo Shader (ya existe desde Fase 9, `EchoShader.shader`) — extender para soportar 5 colores de slot
□ Chromatic Aberration post-process (GDD §13.4) — separación R/G/B en bordes, máscara radial
□ Desaturación de entorno en bullet-time (multiplier 0.55, transición 0.12s ease-in-out)
□ Shimmer temporal de eco (periodo 4s, GDD §13.6)
```

### VFX
```
□ Burst de creación de eco — 12-16 partículas, color del eco, 0.6s (GDD §13.5)
□ Continuo de bullet-time — partículas violeta `#C8B8FF`, 2/seg, float lento
□ Burst de trigger resuelto — blanco + color del eco, 20 partículas
□ Burst de eco expirado sin resolver — rojo + color del eco, implosión
□ Burst global de boss (aparece / muere) — según tabla de Fase 8 §13.5
```

### UI Assets
```
□ Logo PHASE animado (DOTween o equivalente, fade + scale)
□ Eco Strip — 5 slots circulares con estado vacío/activo/en-trigger (GDD §11.2.3)
□ Bullet-Time Ring — indicador circular de carga (GDD §11.2.4)
□ Iconos de forma única por eco (círculo/triángulo/cuadrado/rombo/estrella — accesibilidad daltónica)
□ Árbol de meta-progresión — nodos conectados, 3 estados visuales por rama
```

### Audio (FMOD)
```
□ Stems BASE + RHYTHM + TENSION por zona (3 zonas × set completo)
□ Stems ECO_N — 5 motivos melódicos, uno por slot, reutilizables entre zonas
□ Stem BOSS × 3 (uno por boss de lanzamiento)
□ SFX críticos completos (GDD §12.3): creación de eco, BT on/off, trigger resuelto/expirado,
  daño, muerte, boss aparece/derrotado, sala resuelta
□ Pitch shifter DSP de bullet-time verificado en dispositivo real (no solo en Editor)
```

---

## Gestión de Deuda Técnica

Deuda identificada en fases anteriores que se paga en este milestone (fuente: GDD §19.4 + hallazgos del VS):

| Deuda | Origen | Cuándo pagar |
|-------|--------|-------------|
| Velocidad default de ecos en bullet-time | GDD §19.2 — diferida a playtesting del VS | Resuelta en VS; confirmar en M1 con 5 ecos activos (el VS solo probó 1) |
| Costo/cooldown de bullet-time | GDD §19.2 — requiere datos de comportamiento | Resuelta en VS (Fase 9 §6.1); revalidar en M3 con calibración pasiva activa |
| Backend de nube (Supabase/Firebase) | GDD §19.4 — decisión de producción diferida | M5.5 — no antes, para no bloquear M1-M4 con una decisión no crítica aún |
| Número de nodos del árbol (24 = estimación) | GDD §19.4 | M3 — mantener editable, no hardcodear |
| Sistema de logros completo (30+) | GDD §19.4 — depende de comportamiento real | Fin de M3, con datos del playtesting interno de §3.5 |
| Localización FR/DE/PT-BR | GDD §19.4 — no crítica para VS ni v1.0 | Post-lanzamiento v1.1 |
| Soporte de controllers Bluetooth | GDD §19.4 — edge case | Post-lanzamiento, sin fecha fija |
| Animación de logo PHASE | GDD §19.4 | M5.7, antes de builds de release |

---

## Métricas de Éxito — Fase 10

### Performance (no regresionar desde el VS, targets de Fase 8 §17.5)
| Métrica | Target |
|---------|--------|
| Framerate en gameplay normal | 60fps estable, 0 frames <55fps |
| Framerate en boss + 5 ecos | ≥55fps (peor escenario) |
| Batería / 10 min | <7% en Snapdragon 685 |
| RAM en gameplay (pico, sala de boss) | <350MB |
| APK/AAB size | <100MB |
| Tiempo de carga inicial | <4s |
| Tiempo de carga de sala | <0.8s |

### Completitud de features
```
□ Los 5 slots de eco funcionan con pool de 10 objetos, transición de muerte animada
□ El árbol de 24 nodos no tiene loops imposibles ni dead ends
□ Las 50 salas de las 3 zonas de lanzamiento superan el checklist de GDD §18.1
□ Los 3 bosses de lanzamiento funcionan con sus 3 fases y sin condición de fallo
□ El save/load persiste correctamente en 100 ciclos de save/kill/restore
□ El modo daltónico (3 variantes) es correcto en las 50 salas
□ Los 12 upgrades de run respetan la regla de presentación (nunca 2 del mismo signo de impacto)
□ La calibración pasiva de dificultad ajusta run_tier sin exponer ninguna métrica al jugador
□ Monetización (sin anuncios, skins, season pass) no otorga ninguna ventaja de gameplay — auditado
```

### Validación subjetiva

La pregunta central de PHASE antes de entrar a QA (adaptada del criterio de la Fase 9, ahora a escala de juego completo):

**"¿Hay alguna sala, boss, o sistema de meta-progresión que se sienta como relleno, injusto, o que rompa la regla de que el pasado ya sabe la respuesta?"**

Si la respuesta tiene más de 3 ítems, no se avanza a QA.

---

## Autocrítica — Fase 10

### Fortalezas del plan

**1. El orden de milestones protege la decisión más frágil del juego**
El backend de nube (M5.5) se deja deliberadamente para el final, cuando la monetización ya lo necesita — implementarlo antes habría forzado una decisión de arquitectura (Supabase vs. Firebase) sin la presión real que la obliga a ser correcta. Es la misma lógica que "no automatizar antes de tener certeza" aplicada a infraestructura.

**2. El Tercer Slot se trata como el momento emocional que el GDD dice que es**
Muchos planes de producción tratarían la cinemática de A2 como "polish, si queda tiempo". Ponerla explícitamente en M3 (no en un milestone de "pulido final" que no existe en este plan) refleja que el propio GDD lo llama "el desbloqueo más importante del juego" — el plan de producción no puede contradecir esa jerarquía sin razón.

**3. El alcance de zonas está anclado al roadmap real, no a la curva de dificultad aspiracional**
El GDD tiene una tabla de dificultad (§6.1) que sugiere 5 zonas y runs hasta la 50, pero el roadmap (§16.4) es explícito en que el lanzamiento cubre solo 3 zonas y 50 salas. Este plan sigue el roadmap, no la tabla aspiracional — evita el error de construir contenido de Zona 4-5 que el propio documento de diseño programó para 6 y 12 meses después del lanzamiento.

### Debilidades del plan

**1. Las estimaciones de tiempo son optimistas para un dev solo**
5 milestones × promedio 2.6 semanas = 13 semanas. M2 (pool de 50 salas que pasan un checklist de 7+ criterios cada una) es probablemente el milestone más subestimado — diseñar, iterar y validar 50 salas manuales con fairness cruzada es trabajo de diseño de nivel intensivo, no solo implementación.

**2. La deuda de "backend de nube" sigue siendo vaga hasta M5**
Diferir la decisión Supabase-vs-Firebase protege M1-M4 de bloquearse, pero también significa que si M5 revela que la opción elegida no soporta bien el modelo de sync de skins + Season Pass, el retrabajo llega muy tarde en el ciclo. Sería más seguro hacer un spike técnico de 2-3 días evaluando ambas opciones en paralelo a M3, sin comprometerse a implementar hasta M5.

**3. No hay milestone dedicado a validar el riesgo de rendimiento con 5 ecos**
El GDD (§19.3, Riesgo 4) pide explícitamente una escena de estrés con 5 ecos + partículas máximas + boss activo en la PRIMERA semana de desarrollo, para poder bajar el techo a 4 ecos si Snapdragon 685 no llega a 55fps. Este plan de Fase 10 no incluye ese spike temprano — lo más cercano es el test de regresión de M1.5, que ya ocurre tarde (fin de M1, no día 1). Si el riesgo se materializa después de M2-M3 (con 50 salas ya diseñadas asumiendo 5 slots), el costo de reducir a 4 ecos sería mucho mayor.

### Decisión de avance

El juego completo entra a **Fase 11 — QA y Lanzamiento** cuando:
1. Los 5 milestones están completos
2. Las métricas de performance de Fase 8 §17.5 se cumplen en dispositivo de referencia real (Snapdragon 685)
3. La validación subjetiva tiene < 3 ítems de "algo se siente injusto o relleno"
4. Los builds de Android y iOS compilan sin errores en modo Release
5. Ninguna deuda de la tabla de Gestión de Deuda Técnica queda sin resolver, salvo las explícitamente marcadas para post-lanzamiento
