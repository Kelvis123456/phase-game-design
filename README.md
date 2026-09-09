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
| 10 — Desarrollo completo | 5 milestones de producción — ecos a 5 slots, 50 salas en 3 zonas, 3 bosses, meta-progresión, monetización | ✅ |
| 11 — QA y Lanzamiento | QA interno, beta, store listings, live monitoring, roadmap post-lanzamiento | ✅ |

## Prototipo técnico real

`fase9-vertical-slice/` es un **proyecto Unity 6000.4 LTS real y jugable** (no solo documentación ni scripts sueltos) que ya cubre buena parte del plan de Fase 10, verificado corriendo en vivo con capturas reales de la build standalone en cada sistema:

- **Movimiento y mundo**: controller/stats del jugador, colisión real (BoxCollider2D explícito, no Tilemap), bullet-time por capas (`TimeManager` — el jugador va a 0.1x, los ecos siempre a 1.0x, `Time.timeScale` nunca se toca), muerte por hazard con reset.
- **Ecos**: grabación/reproducción real (`InputRecorder`, `EchoManager` con pool de 10, `EchoPlayer`), posición relativa a la sala (no absoluta) para que un eco grabado en una sala siga teniendo sentido si se lo lleva a otra.
- **Arte real**: sprites de pixel art hechos a mano (jugador, piso, hazard, palanca, puerta) en vez de rectángulos de color placeholder, más un keyart de fondo para el menú principal.
- **Pool de salas**: 50+ salas reales en Zona 1 (SYNC, TIMING, SOLO) y las primeras de Zona 3 (DEPENDENCY con puertas "latching" en cadena, FRUSTRATION con hazard de timing), más un boss real (Fase 1 de "El Espejo Fragmentado": paneles con oscilador + palanca + confrontación final) y un tutorial de 4 salas 100% sin texto (GDD §5) para la primera run de cada jugador.
- **Meta-progresión**: árbol de 29 nodos reales (las 4 ramas del GDD), 9 de 12 upgrades de run con efecto real conectado, cinemática real del "Tercer Espejo" al comprar el 3er slot de eco.
- **Logros**: arquitectura real (no la lista de 30+ del GDD, que el propio documento difiere a datos de jugadores reales) con 7 logros iniciales verificables, pantalla accesible desde el menú principal.
- **UI real**: menú principal, árbol de progresión, selector de upgrades, pantalla de logros — todo construido en runtime (uGUI), no mockups.
- `Assets/Editor/VSSceneBuilder.cs` — construye TODO el proyecto desde cero por código (escena, salas, wiring de cada sistema), reproducible con `-executeMethod VSSceneBuilder.BuildAll`.

**Lo que falta de forma honesta** (no reclamado como hecho): Fases 2-3 del boss, upgrades de run R05/R06/R10 (necesitan sistemas nuevos: lookahead de trigger, trails de eco, UI de revelación), Zona 2 y el resto de Zona 3, monetización (tienda, IAP, anuncios), audio, localización, accesibilidad conectada a gameplay, builds móviles, y QA/lanzamiento real — ver `fase10-desarrollo/` y `fase11-qa-lanzamiento/` para el plan completo de esa parte.

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
