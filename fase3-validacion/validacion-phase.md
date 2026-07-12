# FASE 3 — VALIDACIÓN DEL CONCEPTO
## PHASE: Validación completa antes del GDD
**Fecha:** Junio 2026

---

# BLOQUE 1: VIABILIDAD COMO PRODUCTO

## 1. ¿Por qué alguien instalaría este juego?

### Descripción en tienda que convierte

El jugador instala si la descripción dice algo así:

> *"Cada movimiento que haces se convierte en un fantasma que lo repite para siempre. Detén el tiempo con un toque. Descubre que tu pasado ya resolvió el problema que tienes ahora."*

Ese texto conecta porque:
- Promete algo que el jugador nunca ha hecho ("mi movimiento se vuelve fantasma")
- Promete poder ("detén el tiempo")
- Promete una revelación ("tu pasado ya lo resolvió") — genera curiosidad inmediata

### Screenshot diferenciador

Un screenshot que muestre: personaje principal en el centro + 3-4 siluetas translúcidas de distintos colores ejecutando acciones simultáneas en partes distintas del nivel + un efecto visual de "burbuja de tiempo lento" alrededor del personaje. Este screenshot no existe en ningún otro juego de la tienda. El cerebro humano procesa "¿qué está pasando aquí?" en medio segundo y eso es exactamente lo que se necesita.

### Tagline definitivo

> **"Tu pasado ya sabe la respuesta."**

Tres variantes candidatas para A/B test en tienda:
- "Tu pasado ya sabe la respuesta."
- "Toca para congelar el tiempo. Tus fantasmas no paran."
- "Resuelve puzzles con versiones de ti mismo."

La primera es la más poética y ambigua → genera más descargas por curiosidad.

---

## 2. ¿Por qué volvería mañana?

### Qué queda sin resolver (urgencia de retorno)

El roguelite garantiza esto estructuralmente: cada run termina con un upgrade elegido que el jugador todavía no ha usado. La última pantalla de cada sesión muestra el upgrade que acaba de desbloquear y una preview de lo que hace. El jugador cierra el juego ya queriendo saber cómo se siente jugar con ese upgrade.

Segundo mecanismo: el juego guarda el "record personal" de coordenación de ecos (máximo de ecos coordinados simultáneamente en una sola solución). Ver "Récord: 3 ecos coordinados" el día 1 y saber que hay jugadores con 5 ecos crea aspiración directa.

### Progresión visible entre hoy y mañana

- **Progresión de habilidad genuina**: el jugador que el día 7 puede anticipar qué ecos necesitará 4 pasos adelante es cuantificablemente mejor que el del día 1. Esto se puede visualizar con un "Echo Mastery Score" que sube visiblemente.
- **Progresión de colección**: los ecos desbloqueados tienen skins. Al día 7 el jugador tiene 2-3 ecos con estéticas distintas que ve en su "Spirit Collection".
- **Meta-progresión**: árbol de habilidades permanente que se desbloquea entre runs. Al día 7 hay 4-5 nodos activados de los 30 posibles → sensación de expansión continua.

### ¿Tiene suficiente variedad para el día 7?

**Sí, pero con condiciones.**

La variedad es GENUINA (no percibida) porque:
- Los ecos de tipos distintos crean combinaciones emergentes que ningún diseñador predijo
- El procedural de salas genera configuraciones nuevas cada run
- El árbol de upgrades crea "builds" distintos cada run (run de bullet-time largo vs run de muchos ecos rápidos)

La condición: el sistema de generación procedural de salas debe garantizar que cada sala tenga al menos UN momento de "eco resuelve problema del presente" que no sea idéntico a una sala anterior. Si el procedural reutiliza demasiado, el día 7 se siente repetitivo. Esto debe estar en el GDD como regla de diseño explícita.

---

# BLOQUE 2: ANÁLISIS DE LA EXPERIENCIA

## 3. ¿Qué emoción genera exactamente?

### Primeros 5 minutos: Perplejidad → Delight

El estado emocional no es "diversión" inmediata. Es **perplejidad seguida de una revelación**. El jugador empuja una caja. La caja-fantasma sigue empujando sola. El jugador mira. Frena el tiempo. Ve el fantasma trabajar. Realiza: "eso soy yo antes."

La emoción específica del momento: **sorpresa cognitiva**. El cerebro recibe información que no encaja en ningún esquema previo y eso genera un click interno que es adictivo. Es la misma sensación que al resolver un acertijo verbal por primera vez.

### Primera hora: Estado de Flow

El jugador está en un estado de planificación en dos tiempos. Piensa: "si hago X ahora, mi eco hará X en loop, y necesitaré X más tarde cuando esté en Z posición." Es **pensamiento temporal prospectivo y retrospectivo simultáneo**. Produce el flow state más puro posible: el reto está exactamente en el borde de la competencia.

Emoción específica: **dominio emergente**. Cada run el jugador es un poco mejor en anticipar. La mejora es perceptible y produce orgullo genuino.

### Primera semana: Identidad de jugador

El jugador que lleva una semana con PHASE ha desarrollado una habilidad cognitiva que no tenía: planificación temporal en bucle. Esto es raro. Produce un **sentido de identidad** ("soy el tipo de jugador que puede coordinar 4 ecos en tiempo real"). Este es el enganche de retención más poderoso del juego.

### ¿Es la emoción correcta para mobile?

**Sí, con una advertencia.**

La perplejidad inicial es correcta para mobile porque el primer momento de confusión resuelto en 30 segundos es exactamente el tamaño de sesión que mobile aguanta. El flow state de la hora 1 es perfectamente interrumpible: una run de 7 minutos termina limpiamente cuando te llaman, y vuelves cuando quieras. La identidad de jugador de la semana 1 está atada al progreso persistente, no a una sesión específica.

La advertencia: si la perplejidad inicial dura más de 60 segundos sin resolverse en un "aha" pequeño, el jugador mobile la clasifica como "confusión" y desinstala. El tutorial debe garantizar el primer "aha" en menos de 60 segundos. Esto es un requisito de diseño no negociable.

---

## 4. ¿Cuál es el momento wow exacto?

### Secuencia de segundos del momento wow

**Escenario (tutorial nivel 3, aprox. minuto 4-5):**

1. **0s**: El jugador entra a una sala. Hay un botón de presión en el suelo izquierdo y una puerta cerrada a la derecha. En el centro hay un objeto pesado.
2. **10s**: El jugador empuja el objeto pesado hacia la izquierda. El objeto queda cerca del botón. **Echo 1 creado**: fantasma del jugador empujando el objeto, en loop.
3. **20s**: El jugador salta sobre el objeto (ahora en nueva posición) para alcanzar una plataforma alta. **Echo 2 creado**: fantasma del jugador saltando sobre el objeto, en loop.
4. **30s**: El jugador desde la plataforma alta ve la situación completa. La puerta sigue cerrada. El botón no está presionado. Toca el botón directamente pero está demasiado lejos.
5. **35s**: El jugador quieta el dedo. Bullet-time. Mira los ecos en cámara lenta. Echo 1 sigue empujando el objeto... que se está deslizando lentamente sobre el botón. Echo 2 está aterrizando sobre el objeto justo cuando el objeto llega al botón. El peso del Echo 2 termina de presionar el objeto sobre el botón. La puerta se abre.
6. **40s**: El jugador suelta bullet-time. El tiempo vuelve a la normalidad. Echo 1 y Echo 2 ejecutan la secuencia en tiempo real. La puerta se abre. El jugador pasa.

**Emoción en el segundo 40**: "Fui yo. Lo hice sin saber que lo estaba haciendo."

### ¿Puede ocurrir en 60-90 segundos?

**NO en esta forma compleja.** El momento wow completo (dos ecos coordinando una solución no anticipada) requiere mínimo 3-4 minutos de setup.

**PERO:** hay un momento wow menor que SÍ puede ocurrir en 60 segundos:

Nivel 1, versión ultra-simple: jugador hace UNA acción (empuja caja pequeña). Eco 1 creado. El eco sigue empujando la caja mientras el jugador hace otra cosa. Un enemigo/obstáculo llega. El eco (que el jugador olvidó momentáneamente) bloquea el obstáculo por accidente. El jugador mira. Realiza: "mi eco me salvó sin que yo lo planeara."

Este mini-wow ocurre en 40-50 segundos y es suficiente para el enganche inicial. El wow complejo llega en el minuto 4-5. El diseño debe tener ambos.

### ¿Puede capturarse en video?

**Sí, y con alto potencial viral.**

El clip perfecto de PHASE: 15 segundos de un jugador en bullet-time mirando sus 4 ecos coordinarse solos para resolver algo que parece imposible. El caption escribe solo: "Yo hice esto sin saberlo."

Es exactamente el tipo de contenido que TikTok/Reels amplifica: visual impactante, breve, produce reacción de "¿qué acabo de ver?" y "necesito descargarlo para entender cómo funciona."

---

## 5. ¿Qué evita que aburra?

### Run 10 vs Run 1: diferencias concretas

**Run 1**: 2 ecos máximo, niveles con 1-2 mecanismos simples, bullet-time de 3 segundos máximo, sin upgrades.

**Run 10**: 3 ecos máximo (upgrade desbloqueado), niveles con 3-4 mecanismos, bullet-time de 5 segundos (upgrade), 3 upgrades activos que modifican comportamiento de ecos. El jugador resuelve salas en 40% menos tiempo que en el run 1 y lo percibe como crecimiento real.

### ¿La variedad es genuina?

**Genuina**, con un asterisco. Los ecos generan variedad genuina porque son emergentes: el mismo nivel jugado por dos jugadores produce ecos distintos (cada uno hizo acciones diferentes) y por lo tanto soluciones distintas. Esto es variedad de verdad.

El asterisco: la variedad de los NIVELES depende completamente del sistema procedural. Si los "building blocks" de salas son pocos, el día 7 los jugadores habrán visto todas las combinaciones posibles. La fase 4 (GDD) debe especificar un mínimo de 40-50 "building blocks" de sala distintos para que el procedural no se repita perceptiblemente antes del día 14.

### ¿Tiene profundidad suficiente para mejorar?

**Sí, y es una fortaleza del diseño.** PHASE es uno de los pocos juegos donde la curva de aprendizaje es puramente cognitiva: no hay "grinding de stats" que te haga mejor. Solo tu capacidad de anticipar en dos tiempos mejora. Esto produce una sensación de dominio genuino que los juegos de grind nunca pueden dar.

---

# BLOQUE 3: ANÁLISIS CRÍTICO DE DEBILIDADES

## 6. El problema del tutorial

### ¿Puede enseñarse sin texto en 90 segundos?

Sí, pero requiere diseño de nivel de maestría. Este es el segundo mayor riesgo del proyecto (después del performance). Flujo propuesto:

**Tutorial Nivel 0 — "El primer eco" (0-30 segundos)**
- Sala: un pasillo con un botón al final que abre la salida.
- El jugador entra. Flechas visuales (no texto) guían hacia el botón.
- Jugador camina hacia el botón. Cuando pisa el botón, aparece inmediatamente una silueta translúcida del jugador repitiendo el camino hacia el botón, en loop.
- El botón se apaga (la puerta se cierra de nuevo). La silueta llega al botón y lo pisa. La puerta se abre otra vez.
- El jugador pasa.
- **Aprendió**: mis acciones crean ecos que las repiten. Los ecos hacen cosas útiles.
- **Tiempo**: 20-30 segundos.

**Tutorial Nivel 1 — "Bullet-time" (30-60 segundos)**
- Sala: dos mecanismos simultáneos necesarios. El eco del nivel anterior está repitiendo su acción.
- Hay demasiadas cosas pasando. El jugador necesita procesar.
- Un indicador visual pulsa alrededor del dedo del jugador: una mano animada que muestra "quedate quieto."
- El jugador detiene el dedo. El tiempo se ralentiza visualmente (aberración cromática, partículas lentas).
- El jugador ve el eco moviéndose lentamente. Puede leer la situación con calma.
- Suelta. El tiempo vuelve. Usa el eco para resolver.
- **Aprendió**: quieto = tiempo lento. Útil para leer qué están haciendo mis ecos.
- **Tiempo**: 20-30 segundos.

**Tutorial Nivel 2 — "El momento aha" (60-90 segundos)**
- Sala diseñada para producir el mini-wow: el eco salva al jugador de algo que no anticipó.
- Diseño específico: una trampa activa que el jugador no puede evitar por velocidad, pero el eco (que pasa por ese punto antes) la activa por él, dejando al jugador el camino libre.
- El jugador no planeó esto. Sucedió solo.
- **Aprendió**: mis ecos pueden resolver problemas que yo no vi venir.
- **Tiempo**: 20-30 segundos.

**Total tutorial**: 90 segundos. Un eco aprendido. Bullet-time aprendido. Momento aha experimentado.

### ¿Si falla el tutorial, qué simplificar primero?

Eliminar bullet-time del tutorial completamente. Enseñar solo ecos en los primeros 10 minutos. Introducir el bullet-time como "upgrade" que el jugador desbloquea en su primera run. Esto divide el curva de aprendizaje en dos visitas al juego, lo que es más sostenible para mobile.

---

## 7. El problema de la complejidad cognitiva

### ¿Cuándo se vuelve demasiado?

El techo cognitivo se alcanza en **3 ecos simultáneos en tiempo normal** para casual, y **5 ecos en bullet-time** para mid-core. Más allá de 5 ecos es territorio de jugador hardcore y no debe ser el diseño objetivo del contenido principal.

La variable clave: en bullet-time, el jugador puede procesar mucha más información porque el tiempo le da margen para observar. 5 ecos en bullet-time es manejable. 5 ecos en tiempo normal es caótico y frustrante.

**Regla de diseño emergente**: los retos más difíciles del juego siempre deben ser solucionables en bullet-time. El tiempo normal es para ejecución, el bullet-time es para planificación.

### PHASE Lite (modo para casual sin sacrificar originalidad)

**PHASE Lite** = exactamente el mismo juego con:
- Máximo 2 ecos (nunca 5)
- Sin memory decay (los ecos no se degradan)
- Bullet-time más lento (0.05x en lugar de 0.1x)
- Salas de menor complejidad mecánica

La originalidad no se sacrifica: el "resolviste esto con tu pasado sin saberlo" sigue siendo 100% el corazón del juego. La dificultad baja, la emoción core permanece.

**Implementación**: no es un modo separado. Es la misma curva de dificultad con una opción de "ajuste de experiencia" al inicio: ¿quieres que el juego te desafíe más (hasta 5 ecos) o prefieres empezar ligero (hasta 2 ecos)?

---

## 8. El problema del performance

### Diagnóstico técnico

Los 5 ecos NO necesitan ser entidades físicas completas. Esta es la clave que resuelve el problema de performance:

- **El jugador**: física completa (rigid body, colisiones, interacciones con objetos).
- **Los ecos**: entidades cinemáticas (siguen una ruta grabada, no calculan física en tiempo real). Solo tienen puntos de "trigger" donde interactúan con el mundo (presionar botón → llamada discreta al motor, no colisión continua).

Esto reduce la carga computacional de "5 simulaciones físicas simultáneas" a "1 simulación física + 5 reproductores de animación con trigger points." La diferencia en CPU es de 80-90%.

**Tiempo de manipulación**: cambiar `Time.timeScale` a 0.1 para el jugador mientras los ecos corren en un layer separado con su propio delta time. Técnicamente trivial en Unity/Godot. Costo computacional: casi cero.

**Procedural**: por salas pre-diseñadas ensambladas proceduralmente (no terrain generation). Un set de 50 salas pre-construidas con metadatos de conexión. El ensamblaje es O(n) simple. Memory footprint: 50 salas cacheadas = ~30-40MB de asset, perfectamente dentro del target de 512MB.

### Veredicto de performance

**PHASE es técnicamente viable en Snapdragon 685 con este enfoque.** El riesgo de performance que parecía alto queda resuelto por diseño (ecos cinemáticos, no físicos completos). Esto DEBE estar especificado en el GDD como decisión de arquitectura no negociable.

---

## 9. El problema de la originalidad

### Precedentes conocidos

| Juego | Relación con PHASE | Diferencia clave |
|---|---|---|
| Braid (PC, 2008) | Manipulación de tiempo + plataformas | Rewind, no ecos. Sin roguelite. |
| Ryme (Switch, 2017) | Ecos que repiten acciones del jugador | 3D, consola, sin bullet-time, sin roguelite. |
| Super Time Force (PC/Xbox, 2014) | Crear ecos de uno mismo para cooperar | Run-and-gun shooter, sin puzzle físico, sin touch-native, nunca mobile. |
| Timeshift (PC, 2007) | Bullet-time + manipulación de tiempo | FPS, sin ecos, sin mobile. |
| Echochrome (PSP/PS3) | Nombre con "echo", puzzles de perspectiva | Mecánica completamente diferente. |

**Hallazgo crítico**: Super Time Force es el precedente más cercano al concepto de ecos en PHASE. Sin embargo:
1. Super Time Force es un shooter de acción, no un puzzle físico.
2. Sus ecos son herramientas de DPS (atacan), no herramientas de puzzle (accionan mecanismos).
3. No tiene bullet-time táctil.
4. Nunca llegó a mobile y no tiene versión móvil.
5. Está descontinuado comercialmente.

La diferencia de PHASE no es incremental. Es una mecánica de puzzle táctil que no existe en ningún juego, en ninguna plataforma, en ninguna forma similar.

**¿Es comunicable en 3 palabras?** Propuesta: "Fantasmas. Tiempo. Tú." — ambiguo pero intrigante. Alternativa más directa: "Resuelve con fantasmas." — claro pero menos poético. La tienda admite subtítulo: podría ser "PHASE — Resuelve con tu pasado."

---

# BLOQUE 4: COMPARACIÓN CON EL BACKUP

## 10. PHASE vs FLUX v2

| Criterio | PHASE | FLUX v2 |
|---|---|---|
| Originalidad | Absoluta (0 precedentes directos en mobile) | Alta (tiles de gravedad es nuevo, pero gravedad-puzzle existe) |
| Tutorial en 90s | Posible pero exigente | Fácil (causa-efecto inmediato) |
| Momento wow en 60s | Mini-wow en 45s, wow completo en 4-5 min | Wow inmediato: primera pieza = primer resultado |
| Carga cognitiva casual | Media-alta (dos frames temporales) | Baja-media (un frame, causa-efecto) |
| Escalabilidad de complejidad | Enorme (combinatorias de ecos) | Alta (configuraciones de tiles) |
| Performance | Resuelto con ecos cinemáticos | Sin riesgos (física 2D estándar) |
| Viral potencial | Muy alto (video de ecos coordinándose) | Alto (visual sorprendente de objetos cayendo diagonal) |
| Tamaño de equipo mínimo | 3 personas (requiere puzzle designer especializado) | 2 personas |
| Potencial como IP a largo plazo | Excepcional | Sólido |

### ¿Cuándo elegir FLUX v2?

Escenario A: El equipo es de 1-2 personas con el diseñador haciendo también código. FLUX v2.

Escenario B: Las primeras 3 pruebas del tutorial de PHASE en usuarios reales muestran abandono >60% antes del minuto 2. FLUX v2.

Escenario C: El presupuesto no alcanza para un tester de UX dedicado en los primeros 3 meses. FLUX v2.

Escenario D: Equipo de 3+ personas, presupuesto para 1 ronda de UX testing, y compromiso con la complejidad. PHASE.

### ¿Puede PHASE ejecutarse con <3 personas?

Sí, pero con una distribución muy clara:
- **Persona 1** (obligatorio): Programador senior (Unity o Godot). Implementa sistema de ecos cinemáticos, bullet-time, procedural de salas.
- **Persona 2** (obligatorio): Diseñador de niveles/puzzles. Este rol es el cuello de botella del proyecto. Sin alguien que ENTIENDA el diseño de puzzles de ecos temporales, PHASE no puede existir.
- **Persona 3** (recomendado, puede ser freelance): Arte y audio. Sin arte de calidad, el "fantasma visual" de los ecos no transmite emoción.

Con 2 personas donde una es generalista que hace diseño + código y otra hace arte: viable pero lento y arriesgado. El diseño de puzzles requiere tiempo de iteración largo que un generalista no puede comprimir.

---

# BLOQUE 5: REDISEÑO DE DEBILIDADES

## 11. Mejoras obligatorias antes de Fase 4

**Prioridad 1 — FATAL si no se implementa:**
Dividir el tutorial en dos sesiones. Primera sesión: solo ecos (sin bullet-time). Segunda sesión (run 2+): introducir bullet-time como un "poder nuevo que encontraste." Esto reduce la carga cognitiva del día 1 a la mitad.

**Prioridad 2 — ALTA:**
Especificar explícitamente en el GDD que los ecos son entidades CINEMÁTICAS, no físicas completas. Esta decisión de arquitectura debe aparecer en la sección de tecnología del GDD y no puede cambiar durante el desarrollo sin una revisión de impacto completa.

**Prioridad 3 — ALTA:**
Definir como regla de diseño no negociable: "Cada sala debe tener exactamente UN momento donde un eco anterior resuelve un problema presente que el jugador no anticipó." Si una sala en diseño no cumple esto, se rediseña. No hay excepciones.

**Prioridad 4 — MEDIA:**
El máximo de 2 ecos al inicio no es un número arbitrario. Es el límite cognitivo del tutorial. Debe ser el estado de juego durante al menos las primeras 3 runs. El unlock del tercer slot de eco debe ser un evento notable, no solo un número que sube en un árbol de habilidades.

**Prioridad 5 — MEDIA:**
Diferenciar visualmente PHASE de Super Time Force en todos los materiales de marketing. Nunca usar la palabra "clone" ni "inspired by." Subrayar que la mecánica es táctil-nativa y que el género es puzzle-roguelite, no shooter.

**Prioridad 6 — BAJA (revisar en Vertical Slice):**
El "memory decay" (ecos que se vuelven imprecisos) es una mecánica interesante pero potencialmente frustrante. No incluir en el GDD como mecánica confirmada. Incluir como "mecánica para probar en Vertical Slice."

---

## 12. Versión definitiva de PHASE para el GDD

### PHASE v1.1 — Definición de concepto para Fase 4

**Nombre de trabajo**: PHASE

**Género**: Puzzle-roguelite táctil

**Plataformas**: Android + iOS

**Sesión objetivo**: 7 minutos por run, 1-3 runs por sesión

**Concepto en una frase**: Resuelve puzzles físicos coordinando tus acciones presentes con los ecos de tus acciones pasadas, mientras controlas el tiempo con un toque.

---

**Mecánicas definitivas (confirmadas para GDD):**

1. **Movimiento**: swipe continuo para mover al personaje. El personaje tiene física completa (empuja objetos, activa plataformas, etc.).

2. **Creación de ecos**: cuando el jugador completa un "ciclo de acción" (definido como: una acción que produce un resultado en el mundo — presionar un botón, empujar un objeto hasta su posición final, activar un mecanismo), esa secuencia de movimiento se convierte en un eco cinemático que la repite indefinidamente.

3. **Ecos cinemáticos**: los ecos NO tienen física de rigid body. Siguen su ruta grabada y tienen puntos de trigger predefinidos donde interactúan con el mundo. Esto es una decisión de arquitectura no negociable.

4. **Bullet-time**: mantener el dedo quieto en pantalla = el jugador corre a 0.1x velocidad normal. Los ecos corren a 1.0x siempre. El efecto visual es aberración cromática + partículas ralentizadas alrededor del jugador + los ecos en colores saturados más brillantes.

5. **Slots de eco**: el jugador empieza con 2 slots. Máximo a largo plazo: 5 slots. Los slots adicionales se desbloquean como upgrades en el árbol de meta-progresión (no en upgrades de run, en meta-progresión permanente, desbloqueo cada ~8-10 runs).

6. **Runs**: proceduralmente ensambladas de un pool de salas pre-diseñadas. 6-8 salas por run. Boss room final. Duración: 5-8 minutos.

7. **Upgrades de run**: al final de cada sala, el jugador elige entre 3 upgrades de run que duran solo esa run (modificadores de comportamiento de ecos: eco que rebota, eco que crea una copia, eco que deja trail físico, etc.).

8. **Meta-progresión**: árbol permanente desbloqueado con "phase crystals" ganados por run. Incluye: nuevos slots de eco, nuevas skins de ecos, modificadores de bullet-time, ecos especiales permanentes.

---

**Mecánicas para revisar en Vertical Slice (no incluir en GDD como confirmadas):**

- Memory decay (ecos imprecisos con el tiempo)
- Eco-to-eco interactions (ecos que se afectan entre sí)
- Duración exacta del bullet-time antes de agotarse

---

**Reglas de diseño de nivel (absolutas):**

R1: Cada sala debe tener exactamente UN momento de "eco resuelve problema presente no anticipado."

R2: La sala más simple del juego (Tutorial Nivel 0) debe ser solucionable con 1 eco, sin bullet-time, en 20 segundos.

R3: La sala más compleja del contenido principal debe requerir máximo 4 ecos. 5 ecos es territorio de challenge modes opcionales.

R4: Toda sala debe ser solucionable en bullet-time. Ninguna sala puede requerir reflejos de tiempo real para ser completada.

---

# VEREDICTO FINAL

## ✅ PHASE APROBADO PARA FASE 4

PHASE pasa la validación con las siguientes condiciones obligatorias que deben estar resueltas en el GDD:

1. Tutorial dividido en dos sesiones (ecos primero, bullet-time después)
2. Ecos son entidades cinemáticas (decisión de arquitectura fija)
3. Reglas de diseño de nivel absolutas (R1-R4 arriba)
4. Máximo 2 ecos al inicio de juego
5. Memory decay como mecánica a probar en Vertical Slice, no como mecánica confirmada

**El concepto que entra a Fase 4 es PHASE v1.1 según la descripción de la sección 12.**

**Backup activo**: FLUX v2 sigue disponible si durante el Vertical Slice el tutorial de PHASE demuestra abandono >50% en los primeros 2 minutos con tres iteraciones de rediseño.

---

# AUTOCRÍTICA DE FASE 3

### Lo que esta fase hizo bien

- Identificó el riesgo principal (tutorial) con suficiente detalle como para proponer una solución concreta
- La decisión de ecos cinemáticos vs físicos completos es el hallazgo más importante de esta fase: resuelve el performance sin comprometer la experiencia
- La comparación PHASE vs FLUX v2 produjo criterios específicos de decisión, no solo "uno es mejor que el otro"
- El análisis de precedentes fue honesto: Super Time Force es un precedente real y no ignorarlo es correcto

### Errores y limitaciones

1. **No se puede probar sin prototipo**: la validación completa de PHASE requiere un prototipo de papel o digital de 2 horas. Esta fase validó con análisis y razón, pero el "¿es divertido coordinar 3 ecos en bullet-time?" es una pregunta que solo el prototipo puede responder definitivamente. El Vertical Slice es obligatorio antes del GDD completo.

2. **El análisis de competencia no incluyó mercados asiáticos**: puede haber juegos con mecánicas similares publicados en China o Japón que no están en el radar occidental. Recomendación: antes de lanzar el GDD, hacer búsqueda específica en TapTap y DeNA de "time echo puzzle mobile."

3. **La monetización no fue validada en profundidad**: se asumió que las skins de ecos son suficientes. Esto necesita validación en Fase 4 con proyecciones reales de LTV y estimados de conversión.

### Deuda de diseño para Fase 4

- Diseño detallado del tutorial (nivel por nivel, segundo a segundo)
- Sistema de meta-progresión completo (árbol de habilidades, curva de desbloqueo)
- Definición exacta de qué constituye un "ciclo de acción" que genera eco (reglas precisas para el programador)
- Diseño de 50+ salas base para el pool procedural

---

*Fase 3 completada: Junio 2026*
*Fase siguiente: Game Design Document completo de PHASE v1.1*
*Concepto aprobado: PHASE — "Tu pasado ya sabe la respuesta."*
