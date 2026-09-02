# FASE 11 — QA Y LANZAMIENTO
## Proyecto: PHASE

**Fecha:** 2026-09-02
**Entrada:** Build completo de Fase 10 — 3 zonas, 50 salas, 3 bosses, meta-progresión, monetización, builds de release para Android e iOS.
**Salida:** PHASE publicado en Google Play Store y App Store.

---

## Estructura de la fase

La Fase 11 tiene tres etapas secuenciales. No se pasa a la siguiente sin completar la anterior.

```
Etapa A — QA Interno (2.5 semanas)
  └─ El equipo testea el juego sistemáticamente con casos de prueba documentados,
     con énfasis específico en el sistema de ecos y bullet-time (el riesgo de UX más alto del juego)

Etapa B — Beta Abierta (2 semanas)
  └─ Usuarios externos testean en dispositivos reales — Google Play Internal Testing + TestFlight

Etapa C — Lanzamiento (1 semana)
  └─ Store listings, submit, live monitoring el día del lanzamiento
```

---

## Etapa A — QA Interno

### A.1 — Dispositivos de prueba obligatorios

El GDD (Fase 8 §17.5) fija Snapdragon 685 como dispositivo de referencia. Todo bug crítico debe reproducirse en al menos uno de los dispositivos piso antes de bloquearse:

| Dispositivo | OS | Chipset | Rol |
|-------------|-----|---------|-----|
| Pixel 7 | Android 13 | Tensor G2 | Dispositivo objetivo Android |
| Moto G Power (o equiv. Snapdragon 685) | Android 12 | Snapdragon 685 | **Dispositivo de referencia** — el usado en todos los targets de Fase 8 §17.5 |
| iPhone 14 | iOS 16 | A15 Bionic | Dispositivo objetivo iOS |
| iPhone SE 2022 | iOS 15 | A15 Bionic (RAM reducida) | **Dispositivo piso** iOS — pantalla pequeña, relevante para safe zones (GDD §11.6) |

Si no se dispone de todos los dispositivos físicos, usar BrowserStack Device Testing para el Snapdragon 685 y el iPhone SE.

---

### A.2 — Casos de prueba — Sistema de Ecos (P0)

Los bugs P0 bloquean el lanzamiento. Este es el sistema con más riesgo de UX del juego (GDD §19.3, Riesgo 3) — recibe la mayor densidad de casos de prueba.

```
E-01  El eco reproduce exactamente la ruta grabada
      Pasos: Grabar una ruta con movimiento variado (idle, walk, jump, fall) durante 8s completos
             Comparar posición del eco en cada timestamp contra el PathData original
      Esperado: diferencia < 1px en cualquier timestamp (Fase 8 §17.2.1 — compresión RDP epsilon=2px
                 debe preservar fidelidad perceptual, no exacta al pixel)

E-02  El eco es inmune al Time.timeScale del bullet-time del jugador
      Pasos: Activar bullet-time con un eco a mitad de su loop
             Medir velocidad de reproducción del eco en frames antes/durante/después de BT
      Esperado: el eco se mueve a velocidad idéntica en los 3 estados — CERO cambio perceptible
      Nota: este es el test más crítico de todo el proyecto (equivalente al F-03 de SKIM para su
            ecuación de física) — si esto falla, la mecánica central del juego está rota

E-03  Un Trigger Point solo responde al eco que lo grabó
      Pasos: Generar 2 ecos con rutas distintas, uno pasa por Palanca A, otro no
      Esperado: solo el eco que grabó pasando por Palanca A la activa; el otro simplemente
                atraviesa la zona sin efecto

E-04  Eco Frustrado no rompe el loop
      Pasos: Modificar el estado de un objeto interactuable después de que el jugador lo grabó,
             de forma que el eco llegue al Trigger Point con el objeto en estado incorrecto
      Esperado: animación de frustración (0.5s, vibración + destello rojo), el eco CONTINÚA su loop,
                no se detiene ni se destruye, reintenta en la siguiente vuelta del loop

E-05  ShiftEchos() no genera pop visual ni GC spike
      Pasos: Alcanzar el máximo de slots activos (probar en 2, 3, 4 y 5) y forzar la creación
             de un eco adicional que desplace al más viejo
      Esperado: el eco más viejo se anima con fade de 0.5s antes de reciclarse al pool
                (Fase 10 §1.1); Profiler no muestra spike de GC.Alloc en el frame de la transición

E-06  El pool de 10 EchoPlayer no se agota
      Pasos: Generar y desplazar ecos repetidamente durante una sesión de 20+ minutos
             (varias runs consecutivas sin cerrar la app)
      Esperado: nunca se instancia un EchoPlayer nuevo fuera del pool; sin memory leak asociado

E-07  El nodo A5 "Persistencia de Eco" funciona exactamente como se especifica
      Pasos: Con A5 comprado, fallar una sala (salir por trigger equivocado o abandonar)
             y reintentar la MISMA sala
      Esperado: los ecos generados antes del fallo siguen activos en el reintento;
                sin A5 comprado, los ecos se resetean al reintentar
```

---

### A.3 — Casos de prueba — Bullet-Time (P0)

```
B-01  La transición de activación es de 1 frame; la de desactivación es un ease-out de 0.15s
      Pasos: Medir con Frame Debugger el número de frames entre "dedo se detiene ≥0.3s" y
             "Time.timeScale del jugador llega a 0.1"
      Esperado: activación funcionalmente inmediata; desactivación con la curva de easing especificada,
                sin "teleportación" visual (Fase 8 §17.3)

B-02  Activación involuntaria por micromovimiento táctil
      Pasos: Sostener el dedo con temblor simulado de 2-3px en 10 dispositivos distintos
             (incluir 3 de gama baja, per GDD §19.2)
      Esperado: tasa de activación fallida < 15% (umbral del GDD §19.2); si se supera,
                el modo alternativo "tap doble para activar BT" debe estar listo como fallback

B-03  El pitch shift de audio es perceptible y sincronizado
      Pasos: Activar BT con auriculares, medir el momento del pitch shift (-2 semitonos)
             contra el momento del cambio visual (chromatic aberration + desaturación)
      Esperado: ambos ocurren dentro de la misma ventana de 0.15s — sin desincronización audio/visual

B-04  Los stems ECO_N NO se filtran durante bullet-time
      Pasos: Con 2+ ecos activos, activar BT y escuchar específicamente los motivos melódicos de eco
      Esperado: los stems ECO_N mantienen pitch y filtro normales; solo BASE/RHYTHM/TENSION
                reciben el low-pass de 4kHz (Fase 4 §12.2.2) — esta separación es central a la mecánica

B-05  El slider de velocidad de ecos en bullet-time (accesibilidad) funciona
      Pasos: Ajustar el slider de Options → Gameplay de 1.0× a 0.5×
      Esperado: los ecos reproducen más lento en BT según el valor exacto del slider,
                sin afectar su velocidad fuera de BT
```

---

### A.4 — Casos de prueba — Progresión y Save (P0)

```
S-01  Los datos no se pierden al cerrar la app
      Pasos: Jugar 5 runs completas → cerrar la app (no home, cerrar proceso) → reabrir
      Esperado: PhaseCrystalBalance, nodos del árbol, y run_history son idénticos

S-02  El checkpoint rolling recupera runs interrumpidas por crash
      Pasos: Forzar un crash a mitad de run (kill del proceso) después de al menos 1 checkpoint (30s)
      Esperado: al relanzar, se ofrece "Continuar run" o "Empezar nueva"; la run recuperada
                NO otorga Phase Crystals si se abandona después de recuperarla

S-03  Un nodo con requisito previo no es comprable sin ese requisito
      Pasos: Desde save limpio, intentar comprar A3 (Resonancia Cuádruple) sin tener A2
      Esperado: la UI muestra A3 como bloqueado, no como disponible; SpendCrystals rechaza la compra

S-04  El balance de Phase Crystals no puede ser negativo
      Pasos: Tener 50 PC → intentar comprar un nodo de 150 PC
      Esperado: SpendCrystals devuelve false, balance sigue en 50

S-05  La calibración pasiva de dificultad nunca se muestra al jugador
      Pasos: Forzar 5 runs abandonadas consecutivas (dispara el ajuste de GDD §10.1)
      Esperado: ninguna UI, texto, o notificación revela que el sistema ajustó run_tier —
                solo se observa indirectamente en qué salas aparecen después

S-06  "Eliminar mi cuenta" (GDPR) borra todo en la nube en ≤72h
      Pasos: Ir a Settings → About → Eliminar mi cuenta → confirmar
      Esperado: confirmación inmediata en cliente; verificar en backend que los datos
                de nube se eliminan dentro de la ventana comprometida

S-07  "Exportar mis datos" genera un JSON completo y válido
      Pasos: Settings → About → Exportar mis datos
      Esperado: el JSON descargable contiene la estructura completa de Fase 4 §15.2,
                sin campos vacíos inesperados
```

---

### A.5 — Casos de prueba — Salas y Fairness (P1)

Los bugs P1 deben resolverse antes del lanzamiento pero no bloquean la beta.

```
R-01  Ninguna sala del pool tiene solución de "fuerza bruta" sin ecos
      Pasos: QA tester intenta completar cada una de las 50 salas SIN crear ningún eco
      Esperado: 0 de 50 salas son resolubles sin crear al menos 1 eco (GDD §18.1, criterio explícito)

R-02  El algoritmo de ensamblaje respeta el límite de 420s por run
      Pasos: Generar 100 runs con semillas aleatorias, sumar estimated_duration_s de las 5 salas
      Esperado: 0 runs superan 420s (7 min) en duración estimada total

R-03  Zonas 1-2 tienen al menos 2 rutas de solución válidas (Regla F3)
      Pasos: QA tester resuelve cada sala Tier 1-5 de Zona 1 y 2 de dos formas distintas
      Esperado: ambas rutas completan la sala exitosamente

R-04  El modo daltónico no rompe la legibilidad de ninguna sala
      Pasos: Activar cada uno de los 3 modos daltónicos y jugar 5 salas representativas por zona
      Esperado: los 5 ecos siguen siendo distinguibles por forma de ícono en todos los modos

R-05  El anti-repetición de salas funciona en ventana de 100 runs
      Pasos: Jugar 100 runs consecutivas registrando sala_id de cada sala servida
      Esperado: ningún sala_id se repite dentro de esa ventana de 100
```

---

### A.6 — Casos de prueba — Bosses (P0)

```
J-01  Ningún boss tiene condición de fallo
      Pasos: En cada uno de los 3 bosses, dejar la pelea a medio resolver y salir manualmente
             de la run (no completar ninguna fase)
      Esperado: el jugador puede abandonar sin quedar atrapado en un estado inconsistente;
                sin crash, sin softlock

J-02  Las transiciones de fase no pierden ecos activos
      Pasos: En cada boss, verificar que los ecos generados en fases anteriores de la run
             siguen disponibles y funcionales al entrar a la Fase 2 y Fase 3 del boss
      Esperado: 0 ecos perdidos en transición de fase

J-03  El Boss Z1 (paneles oscilantes) resuelve con el timing documentado
      Pasos: Medir el período real de oscilación de los 5 paneles en build final
      Esperado: coincide con el valor [VS] validado en producción (Fase 8 §8.2, periodo base 8s)

J-04  El Boss Z3 (suelo que se desintegra) no mata injustamente
      Pasos: Verificar que la ventana de 2s [VS] de cada activación es suficiente para un jugador
             de habilidad media, medida en 10 intentos de QA
      Esperado: tasa de "muerte injusta" (el jugador ejecuta correctamente pero el timing
                era imposible de leer) reportada en 0 de 10 intentos
```

---

### A.7 — Casos de prueba — Performance (P0)

```
P-01  60fps estable en gameplay normal, dispositivo de referencia
      Pasos: Jugar 10 salas variadas en Snapdragon 685
      Esperado: 0 frames por debajo de 55fps

P-02  ≥55fps en el peor escenario (boss + 5 ecos + partículas máximas)
      Pasos: Llegar a Fase 3 de cualquier boss con los 5 slots de eco desbloqueados y activos
      Esperado: ≥55fps sostenido, medido con Unity Profiler en dispositivo de referencia
      Nota: este es el escenario que el GDD (§19.3, Riesgo 4) identificó como el más incierto —
            si falla aquí, la mitigación documentada es bajar el techo de ecos activos a 4

P-03  Batería <7% en 10 minutos de juego continuo
      Pasos: Medir con Android Battery Historian en Snapdragon 685, sesión de 10 min de gameplay activo
      Esperado: consumo <7%

P-04  RAM <350MB en pico (sala de boss), <180MB en menús
      Pasos: Perfilar RAM durante la Fase 3 de cada boss y durante navegación de meta-progresión
      Esperado: ambos picos dentro del budget

P-05  APK/AAB <100MB, tiempo de carga inicial <4s, carga de sala <0.8s
      Pasos: Generar build de release, medir tamaño en Play Console; medir tiempo desde
             tap en ícono hasta Pantalla de Inicio; medir tiempo de transición entre salas
      Esperado: los 3 valores dentro de los targets de Fase 8 §17.5

P-06  Sin memory leak en sesión de 30 minutos continuos
      Pasos: Jugar 30 min sin cerrar la app, monitoreando RAM con Profiler
      Esperado: RAM estable; candidato principal si falla: pool de ecos o VFX no reciclados
```

---

### A.8 — Casos de prueba — Monetización (P0)

```
M-01  Los 3 tipos de IAP completan en entorno sandbox
      Pasos: Cuenta de prueba en Google Play / Apple Sandbox — comprar Sin Anuncios,
             cada una de las 4 skins premium, y el Season Pass
      Esperado: cada compra aplica su efecto correctamente (ads_removed=true, skin desbloqueada,
                season pass activo con sus 5 skins + modificador exclusivo)

M-02  Los IAP fallidos no otorgan nada
      Pasos: Cancelar el diálogo de pago en cada uno de los 3 tipos de compra
      Esperado: ningún estado cambia, OnPurchaseFailed se emite correctamente

M-03  Ningún IAP otorga ventaja de gameplay
      Pasos: Auditar código y diseño — comparar el set de acciones posibles para un jugador
             con todos los IAP comprados vs. uno sin ninguno
      Esperado: idéntico set de acciones mecánicas posibles; la única diferencia es cosmética
                y la ausencia de anuncios opcionales (esto es un principio de diseño no negociable,
                GDD §9.4 — cualquier violación es P0 automático sin importar su origen)

M-04  El anuncio recompensado (+10 PC) respeta el límite de 3/día
      Pasos: Ver el anuncio 4 veces en el mismo día (hora local)
      Esperado: la opción desaparece o se deshabilita después de la 3ra vez, reaparece al día siguiente

M-05  El Season Pass no se renueva automáticamente sin consentimiento explícito
      Pasos: Verificar el flujo de renovación trimestral en sandbox
      Esperado: el usuario recibe notificación clara antes de cualquier cargo recurrente,
                con opción de cancelar visible y funcional
```

---

### A.9 — Criterio de salida de QA Interno

```
□ 0 bugs P0 abiertos (con énfasis: E-01 a E-07 y B-01 a B-05 son innegociables — son el núcleo del juego)
□ < 5 bugs P1 abiertos (y todos con workaround conocido)
□ Performance: todos los casos P-01 a P-06 pasan en Snapdragon 685
□ Monetización: todos los casos M-01 a M-05 pasan en sandbox, M-03 auditado explícitamente
□ Fairness: R-01 a R-05 pasan en las 50 salas del pool completo
□ Save: ningún caso de corrupción de datos encontrado en 100+ ciclos de save/kill/restore
```

---

## Etapa B — Beta Abierta

### B.1 — Distribución

**Android — Google Play Internal Testing:**
- Hasta 100 testers en Internal Testing, distribución por email
- Feedback por formulario (link en descripción del build)

**iOS — TestFlight:**
- Hasta 10,000 testers externos, distribución por link público
- Feedback por TestFlight built-in + formulario

### B.2 — Qué pedir a los beta testers

El formulario está diseñado para validar específicamente las métricas de tutorial y comprensión del GDD (§18.3) — no son preguntas genéricas, apuntan a los riesgos de diseño ya identificados (§19.3).

```
1. ¿En qué dispositivo jugaste? (campo libre)

2. ¿El juego corrió sin problemas en tu teléfono?
   □ Sí, sin problemas
   □ Sí, pero con algunos drops de FPS
   □ No, el juego se congeló o cerró solo

3. ¿En algún momento entendiste, sin que nadie te lo explicara, que la figura que te
   seguía era una repetición de tus propias acciones?
   □ Sí, lo entendí solo y rápido
   □ Lo entendí después de un rato
   □ No lo entendí del todo

4. ¿Descubriste que quitar el dedo de la pantalla ralentizaba el tiempo?
   □ Sí, por accidente
   □ Sí, lo intenté deliberadamente
   □ No lo descubrí / tuve que buscarlo

5. ¿Hubo algún momento en que tu "eco" resolvió algo que tú no habías planeado,
   y te sorprendió de forma positiva? (campo libre — describe qué pasó)

6. ¿Alguna vez el juego se sintió injusto — como si tu eco te "traicionara"? (campo libre)

7. ¿Cuántas runs jugaste aproximadamente? ___

8. ¿Qué cambiarías o añadirías? (campo libre — opcional)

9. Si esto estuviera disponible hoy en la tienda, ¿lo descargarías? Sí / Tal vez / No
```

**Nota de diseño del formulario:** las preguntas 3, 4, 5 y 6 no son genéricas — corresponden directamente a los criterios de "el tutorial funciona" del GDD §18.3 (comprensión de bullet-time >70%, comprensión de ecos >60%, momento aha, ausencia de sensación de traición). Los resultados de esta beta son la primera validación real de esos números con usuarios que nunca vieron el juego.

### B.3 — Bugs críticos de la beta

Un bug encontrado por ≥3 testers en dispositivos distintos entra automáticamente como P0 aunque no haya estado en los test cases de QA Interno.

Si ≥20% de los testers responde "No lo entendí del todo" en la pregunta 3, esto NO se trata como bug — se trata como señal de diseño que requiere revisar el tutorial (GDD §11.5.2, las 7 micro-salas) antes de continuar a Etapa C, aunque técnicamente no bloquee un submit.

### B.4 — Criterio de salida de Beta

```
□ ≥ 50 respuestas al formulario
□ ≥ 80% reportan "sin problemas" de performance
□ ≥ 70% entendieron el sistema de ecos sin ayuda (pregunta 3)
□ ≥ 70% descubrieron bullet-time (pregunta 4) — umbral idéntico al del GDD §18.3
□ ≥ 60% reportan al menos un momento "aha" (pregunta 5)
□ < 15% reportan sensación de injusticia/traición del eco (pregunta 6)
□ 0 bugs P0 nuevos sin resolver
□ Los comentarios frecuentes de preguntas 8 están documentados como roadmap v1.1
```

---

## Etapa C — Lanzamiento

### C.1 — Store Listings

**Google Play Store:**

```
Título: PHASE — Puzzle Roguelite
Subtítulo (30 chars): Tu pasado resuelve el presente

Descripción corta (80 chars):
Crea ecos de tu pasado. Coordina tu presente. Resuelve lo imposible.

Descripción larga:
PHASE es un puzzle-roguelite donde cada acción que haces se convierte en un eco de
ti mismo — repitiendo lo que hiciste, en bucle, mientras tú sigues actuando en el presente.

Toca la pantalla para moverte. Suelta el dedo para activar bullet-time y leer la sala
con claridad — pero tus ecos no esperan. Siguen moviéndose a velocidad normal.

El momento central de PHASE es retroactivo: resuelves una sala con acciones que
tomaste sin saber que las ibas a necesitar.

⏱ BULLET-TIME PARA LEER, NO PARA REACCIONAR — sin reflejos, con estrategia
👥 HASTA 5 ECOS SIMULTÁNEOS — una orquesta de versiones de ti mismo
🧩 50 SALAS ÚNICAS en 3 zonas, generadas proceduralmente en cada run
👹 3 BOSSES DE 3 FASES — puzzles físicos que evolucionan, no enemigos con HP
🎨 SIN PAY-TO-WIN — todo el gameplay se desbloquea jugando; el dinero compra cosméticos

Runs de 5-8 minutos. Sin vidas, sin energía, sin esperas.
Tu pasado ya sabe la respuesta.

Categoría: Puzzle
Clasificación de contenido: Everyone
```

**Screenshots requeridos (7 total, 1080×1920):**
```
1. Gameplay — jugador coordinando con 3 ecos activos, colores distintos visibles
2. Bullet-time activo — chromatic aberration + desaturación de entorno, arco de lectura claro
3. Momento "aha" — un eco resolviendo un trigger mientras el jugador está en otra parte de la sala
4. Boss Z1 "El Espejo Fragmentado" — Fase 3, todos los paneles activos
5. Árbol de meta-progresión — nodos de las 4 ramas visibles
6. Pantalla "MIS ECOS" — colección de skins, 5 slots
7. Pantalla de fin de run — estadísticas, Phase Crystals ganados
```

**App Store (iOS) — campo adicional:**
```
Keywords (100 chars):
puzzle,roguelite,echo,timeloop,bullettime,strategy,timeloop,physics,mobile,indie,timeloop puzzle
```

### C.2 — Política de privacidad

Necesaria para Google Play y App Store (obligatoria siempre). Contenido mínimo alineado con Fase 4 §15.4:

```
PHASE recopila un UUID de jugador no vinculado a datos personales, estadísticas de runs
para balanceo de contenido, y reportes de crash anonimizados.
No compartimos datos con terceros. No hay perfil publicitario.
Los IAP son procesados por Google Play / Apple App Store según sus términos.
El usuario puede exportar o eliminar todos sus datos desde Settings → About.
Contacto: [email del estudio]
```

Publicar en una URL permanente antes del submit.

### C.3 — Checklist de submit

**Android:**
```
□ AAB firmado con keystore de producción, guardado en lugar seguro + backup offline
□ Bundle version code incrementado
□ Screenshots subidos (7 de 1080×1920)
□ Descripción en español e inglés
□ Política de privacidad publicada y URL añadida en Play Console
□ Clasificación de contenido completada (cuestionario IARC)
□ Precio: Free (con IAP)
□ Build promovido de Internal Testing a Production
```

**iOS:**
```
□ Archive generado en Xcode con Distribution Certificate
□ Subido a App Store Connect
□ Screenshots en 6.7" (obligatorio) y 5.5" (obligatorio)
□ Descripción en inglés (idioma principal) + español
□ Keywords completos
□ Política de privacidad URL añadida
□ IDFA: marcar "Does not use IDFA" (sin publicidad de terceros en PHASE)
□ Age Rating: 4+
□ In-App Purchases creados en App Store Connect con mismos IDs que el código
  (Sin Anuncios, 4 skins premium, Season Pass trimestral)
□ Submit for Review
```

### C.4 — Tiempos de revisión esperados

```
Google Play:   3-7 días laborables (primera subida)
App Store:     1-3 días (varía)
```

Planificar el submit para martes o miércoles para evitar que la revisión caiga en fin de semana.

---

## C.5 — Día del Lanzamiento — Live Monitoring

### Dashboard de monitoreo

```
GameAnalytics / analytics propio:
  - Sessions per day, session length, DAU
  - Eventos de tutorial (TUTORIAL_COMPLETED, BT_FIRST_USE, TUTORIAL_ROOM_ABANDONED) — Fase 4 §18.3
  - Eventos de progresión (RUN_COMPLETED, slots desbloqueados)
  - Eventos de negocio (IAP completados, Season Pass activaciones)

Google Play Console:
  - ANR rate: target <0.47%
  - Crash rate: target crash-free sessions >99.2%
  - Rating promedio: objetivo ≥4.2 en primera semana

App Store Connect:
  - Crashes (Xcode Organizer)
  - Ratings
```

### Umbrales de acción inmediata

Si en las primeras 6 horas:
- Crash-free sessions <97% → detener rollout en Play Console
- 3+ reviews de 1 estrella mencionando el mismo bug de eco/bullet-time → hotfix prioritario máximo
  (los bugs de la mecánica central se tratan con la misma urgencia que un bug de pago)
- ANR rate >2% → probable deadlock en el hilo principal, revisar primero el sistema de ecos
  (es el sistema con más lógica corriendo en Update())

### Hotfix pipeline

```
Bug crítico en producción → plazo de hotfix:
  - Crash que afecta >10% de sesiones: 24 horas
  - Bug en el sistema de ecos que rompe el loop central: 24 horas (misma prioridad que crash —
    es la mecánica que vende el juego)
  - Bug de IAP (no se otorga lo comprado): 12 horas (prioridad máxima)
  - Bug de fairness (una sala del pool resulta irresoluble en producción): 48 horas,
    con mitigación inmediata de sacar esa sala del pool remoto si el sistema lo permite

Proceso:
  1. Fix en rama hotfix/v1.0.1
  2. Test en Snapdragon 685 (dispositivo de referencia)
  3. Build de release
  4. Google Play: rollout 20% → 100% en 2h si no hay más crashes
  5. App Store: submit expedited review para bugs críticos
```

---

## Roadmap Post-Lanzamiento

Extraído directamente del roadmap del GDD (Fase 4 §16.4) — no es aspiracional, es el plan de producción ya documentado.

### v1.1 — Primera actualización (3 meses post-lanzamiento)
```
□ Pool ampliado a 65 salas (+15 nuevas, distribuidas en Zonas 1-3)
□ Nuevo tipo de eco: EcoMirror (ruta espejada en X, primer tipo alternativo — Fase 4 §16.3)
□ Boss Remix del Boss Z1
□ 2 Story Vignettes de lore (3-5 salas narrativas opcionales cada una)
□ Corrección de bugs reportados en reviews de lanzamiento
□ Localización FR/DE/PT-BR (deuda diferida del GDD §19.4)
```

### v1.2 — Segunda actualización (6 meses post-lanzamiento — Zona 4)
```
□ Zona 4 "Resonancia" completa: 20 salas nuevas (Fase 4 §16.5 — mínimo 15-20 por zona nueva),
  1 boss nuevo (Z4 "La Resonancia", ya especificado en Fase 8 §8.3), nueva música, nuevo tileset
□ Nuevo tipo de eco: EcoRetrasado (inicia N segundos después de ser creado)
□ Árbol de meta-progresión expandido con nodos relacionados a Zona 4
□ QoL: estadísticas detalladas de runs en pantalla de resultados
```

### v2.0 — Si PHASE tiene éxito comercial (1 año post-lanzamiento)
```
□ Zona 5 "Convergencia" — el boss final recontextualiza la sala de tutorial (Fase 8 §8.3, nota
  específica: "toda la progresión del jugador se pone a prueba en una sola sala")
□ Modo Espejo: salas de zonas 1-4 en versión espejada con modificadores
□ Tipo de eco: EcoInverso (el más complejo — requiere diseño de salas específico)
□ Coleccionables cosméticos adicionales, leaderboards de temporada
```

---

## Autocrítica — Fase 11

### Fortalezas del plan

**1. Los casos de prueba priorizan la mecánica central, no la superficie**
E-01 a E-07 y B-01 a B-05 (ecos y bullet-time) tienen más profundidad de test que cualquier otra categoría, incluyendo monetización. Esto refleja correctamente dónde está el riesgo real del proyecto: PHASE no falla por un bug de UI, falla si el eco se siente como decoración en vez de personaje (GDD §19.3, Riesgo 3).

**2. El formulario de beta mide exactamente lo que el GDD dijo que había que medir**
En lugar de un formulario genérico de satisfacción, las preguntas 3-6 son una traducción directa de los criterios de "el tutorial funciona" de GDD §18.3. Esto convierte la beta en una validación real de hipótesis de diseño, no solo en una cacería de bugs.

**3. Los hotfixes tratan bugs de ecos con la misma urgencia que bugs de pago**
La mayoría de los pipelines de hotfix priorizan crashes y dinero. Este plan reconoce explícitamente que un bug que rompe el sistema de ecos es igual de urgente que un bug de IAP, porque el eco ES el producto — no es un sistema secundario.

### Debilidades del plan

**1. No hay plan de ASO (App Store Optimization) validado**
Las keywords elegidas ("timeloop", "bullettime") son razonables pero repiten "timeloop" dos veces por error de composición y no están respaldadas por investigación real de volumen de búsqueda. Es trabajo de 1-2 horas con una herramienta de ASO que este plan no incluye — debería hacerse antes del submit, no después de ver descargas bajas.

**2. La calibración pasiva de dificultad no tiene test case explícito de "efecto no deseado"**
El caso S-05 verifica que el sistema no se muestra al jugador, pero no hay un caso que verifique que la calibración no crea un bucle donde el jugador que abandona runs recibe salas cada vez más fáciles, se aburre, y abandona más — un efecto de retroalimentación negativa que el GDD no contempló explícitamente. Esto debería añadirse como caso de QA antes de v1.1, usando datos reales de D7/D30.

**3. El roadmap depende de un backend de nube que Fase 10 dejó sin resolver hasta el final**
Si la decisión de M5.5 (Fase 10) resulta en una arquitectura que no escala bien para 65+ salas y Season Pass recurrente, el roadmap de v1.1/v1.2 hereda ese riesgo sin que este documento lo mencione. Un spike de validación de backend con carga simulada de v1.2 (Zona 4, más contenido, más usuarios) debería ejecutarse en el primer mes post-lanzamiento, no asumirse como resuelto.

---

## Estado final del proyecto al lanzamiento

Con la Fase 11 completa, PHASE está publicado en Google Play Store y App Store.

Las 11 fases del estudio han producido:

| Fase | Entregable |
|------|-----------|
| 1 — Investigación | Análisis de mercado — tamaño, oportunidades, géneros a evitar |
| 2 — Ideas | 35 conceptos generados, 5 finalistas |
| 3 — Validación | Decisiones de diseño no negociables, reglas de nivel |
| 4 — GDD | Especificación completa (~2300 líneas) — mecánica, economía, progresión, arte, audio, arquitectura |
| 5 — UX/UI | Prototipo de 8 pantallas en Pencil |
| 6 — Arte | Art bible, paletas, shader de eco |
| 7 — Tecnología | Unity 2022 LTS, FMOD, stack completo |
| 8 — Arquitectura | Patrones, sistemas, 16 secciones de diseño técnico |
| 9 — Vertical Slice | Prototipo real en C#, playtesting, criterios Go/No-Go |
| 10 — Desarrollo | Juego completo — 5 slots de eco, 3 zonas, 50 salas, 3 bosses, meta-progresión, monetización |
| 11 — QA y Lanzamiento | **PHASE en producción** |

**Tu pasado ya sabe la respuesta.**
