# PHASE

**"Tu pasado ya sabe la respuesta."**

PHASE es el diseño completo de un roguelite móvil original construido alrededor de una mecánica que no existe en ningún otro juego móvil: el jugador deja **ecos** de sus propias acciones pasadas, que se repiten en bucle mientras él actúa en el presente. Tocar la pantalla y quedarse quieto ralentiza el tiempo (bullet-time) para leer la escena — pero los ecos siguen moviéndose a velocidad normal. El "aha" central del juego es retroactivo: resuelves una sala con acciones que tomaste sin saber que las ibas a necesitar.

Runs cortas (5-8 min), proceduralmente generadas, con progresión de meta entre corridas.

## Estado del proyecto

Este repositorio es un **documento de pre-producción completo**, no un juego terminado. Cubre desde la investigación de mercado hasta la arquitectura técnica lista para implementar, siguiendo una metodología de estudio de desarrollo de juegos con fases secuenciales:

| Fase | Contenido | Estado |
|---|---|---|
| 1 — Investigación de mercado | Tamaño de mercado, oportunidades, géneros a evitar | ✅ |
| 2 — Ideación | 35 conceptos generados, 5 finalistas | ✅ |
| 3 — Validación de concepto | Decisiones de diseño no negociables, reglas de nivel | ✅ |
| 4 — Game Design Document | Especificación completa (~2300 líneas) | ✅ |
| 5 — Prototipo UX/UI | 8 pantallas diseñadas | ✅ |
| 6 — Dirección de arte | Art bible, paletas, shader de eco | ✅ |
| 7 — Selección de tecnología | Unity 2022 LTS, FMOD, stack completo | ✅ |
| 8 — Arquitectura técnica | Patrones, sistemas, 16 secciones de diseño | ✅ |
| 9 — Vertical Slice | Plan + **proyecto Unity real, jugable y verificado** | ✅ |
| 10 — Desarrollo completo | 5 milestones de producción — ecos a 5 slots, 50 salas en 3 zonas, 3 bosses, meta-progresión, monetización | M1-M4 ✅, M5 parcial (sin anuncios + skins Premium + Season Pass + accesibilidad + primer APK de Android ✅, nube/GDPR/build de release pendiente) |
| 11 — QA y Lanzamiento | QA interno, beta, store listings, live monitoring, roadmap post-lanzamiento | Documento de plan ✅, ejecución real pendiente |

## Prototipo técnico real

`fase9-vertical-slice/` es un **proyecto Unity 6000.4 LTS real y jugable** (no solo documentación ni scripts sueltos) que ya cubre buena parte del plan de Fase 10, verificado corriendo en vivo con capturas reales de la build standalone en cada sistema:

- **Movimiento y mundo**: controller/stats del jugador, colisión real (BoxCollider2D explícito, no Tilemap), bullet-time por capas (`TimeManager` — el jugador va a 0.1x, los ecos siempre a 1.0x, `Time.timeScale` nunca se toca), muerte por hazard con reset.
- **Ecos**: grabación/reproducción real (`InputRecorder`, `EchoManager` con hasta 5 slots reales, `EchoPlayer`), posición relativa a la sala (no absoluta) para que un eco grabado en una sala siga teniendo sentido si se lo lleva a otra, más un `TrailRenderer` visible en bullet-time (R06).
- **Arte real**: sprites de pixel art hechos a mano (jugador, piso, hazard, palanca, puerta) en vez de rectángulos de color placeholder, más un keyart de fondo para el menú principal.
- **Pool de salas**: las 3 zonas completas del plan de Fase 10 — Zona 1 "Umbral" (SYNC, TIMING, SOLO), Zona 2 "Fracturas" (17 salas, DEPENDENCY como mecánica central) y Zona 3 "Abismo" (15 salas, incluye FRUSTRATION), con `RunManager.CurrentZoneId()` decidiendo la zona activa por runs completadas (GDD §6.2) — más un tutorial de 4 salas 100% sin texto (GDD §5) para la primera run de cada jugador.
- **3 bosses reales, cada uno con sus 3 fases completas** (GDD §8.2/§8.3): "El Espejo Fragmentado" (Z1, paneles con oscilador + contrapeso E4→E2 + convergencia final), "La Fractura" (Z2, `CollapsingPlatform` sostenida por palanca) y "El Abismo" (Z3, `DisintegratingFloor` con pulso de 2s en vez de sostén continuo, para que el timing se sienta distinto). Los 3 comparten el patrón universal sin fail-state del GDD §8.1.
- **Meta-progresión**: árbol de 29 nodos reales (las 4 ramas del GDD), los 12 upgrades de run (R01-R12) con efecto real conectado, cinemática real del "Tercer Espejo" al comprar el 3er slot de eco.
- **Audio real**: música por zona + SFX (`AudioManager`), todo con licencia libre (créditos en `Assets/Audio/CREDITS.md`).
- **Logros**: arquitectura real (no la lista de 30+ del GDD, que el propio documento difiere a datos de jugadores reales) con 7 logros iniciales verificables, pantalla accesible desde el menú principal.
- **Monetización real (parcial)**: modo sin anuncios ($3.99) y las 4 skins Premium (C4/C7/C8/C10, GDD §9.2 vías 1 y 2) comprables vía `MonetizationSystem` sobre un `IPurchaseProvider` stub — conectar Unity IAP real necesita una cuenta de consola de tienda que no existe en este entorno. `SkinCatalog` traduce las 10 skins de la Rama C a tinte/opacidad/luz/ciclo de color, equipables por slot desde "Mis Ecos" (`EchoShopUI`).
- **Accesibilidad real (completa para escritorio)**: modo daltónico (GDD §14.2) — `ColorblindPalette` remapea los colores identificadores de eco — más velocidad de ecos y tiempo de carga de bullet-time (GDD §14.3), todo editable en vivo desde "Opciones" (mouse o teclado, selector ↑↓/←→). Tamaño de texto y soporte de una sola mano (GDD §14.4/§14.5) son específicos de UI móvil/táctil y no aplican a este build de escritorio.
- **Season Pass real (parcial)**: `SeasonPassSystem` conecta el estado activo/vencido y la compra ($4.99/trimestre, GDD §9.2 vía 3) con badge visible en "Mis Ecos". Las 5 skins exclusivas del trimestre, el modificador de Rama B exclusivo y los desafíos semanales quedan fuera — dependen de la Rama B teniendo efecto de gameplay real (todavía no lo tiene, deuda previa) y de un sistema de rotación de contenido en vivo que no existe.
- **Build de Android real (no firmado)**: `VSSceneBuilder.BuildAndroidApk()` compila y empaqueta un APK real (58MB, Mono + armeabi-v7a) desde este proyecto — verificado desempaquetando el APK. No es IL2CPP+ARM64 (obligatorio en Google Play real) ni tiene firma de producción, y no se probó en un dispositivo o emulador real.
- **UI real**: menú principal, árbol de progresión, selector de upgrades, pantalla de logros, tienda "Mis Ecos" — todo construido en runtime (uGUI), no mockups.
- `Assets/Editor/VSSceneBuilder.cs` — construye TODO el proyecto desde cero por código (escena, salas, wiring de cada sistema), reproducible con `-executeMethod VSSceneBuilder.BuildAll`.

**Lo que falta de forma honesta** (no reclamado como hecho): efecto de gameplay real para la Rama B de modificadores de run (deuda previa a monetización/Season Pass), backend de nube real, GDPR, el build de Android de release (IL2CPP+ARM64, firma de producción, probado en dispositivo real), build de iOS (necesita Xcode/Mac, no disponible en este entorno), y QA/lanzamiento real — todo requiere cuentas de plataforma o testers humanos que no existen en este entorno; ver `fase10-desarrollo/` y `fase11-qa-lanzamiento/` para el plan completo. La mayoría de la verificación es de compilación + reconstrucción de escena/build en batch mode; algunos sistemas (monetización, modo daltónico) también se probaron jugando la build real (lanzándola como ventana y capturando pantalla, sin mouse — solo teclado, ver gotcha en memoria del proyecto), pero no hay un pase manual completo de cada mecánica.

Abrir con Unity Hub (versión 6000.4.10f1) apuntando a `fase9-vertical-slice/`, o regenerar la escena y el build desde cero vía Unity en batchmode.

## Estructura del repositorio

```
fase1-investigacion/    Informe de mercado
fase2-ideas/             35 conceptos evaluados
fase3-validacion/        Validación del concepto ganador
fase4-gdd/               Game Design Document completo
fase5-uxui/              Prototipo de UI (Pencil) + style guide
fase6-arte/              Dirección de arte y paletas
fase7-tecnologia/        Stack técnico seleccionado
fase8-arquitectura/      Arquitectura de sistemas
fase9-vertical-slice/    Proyecto Unity real y jugable (Assets/Packages/ProjectSettings) + docs de plan
fase10-desarrollo/       Plan de producción completa: 5 milestones, checklist de arte, deuda técnica
fase11-qa-lanzamiento/   QA interno, beta abierta, store listings, live monitoring, roadmap post-lanzamiento
```

## Stack técnico

Unity 2022 LTS + URP · C# · FMOD Studio · Aseprite · Git/GitHub Actions + GameCI

---

*Proyecto de diseño original, sin afiliación a franquicias existentes.*
