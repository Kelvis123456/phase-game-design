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
| 9 — Vertical Slice | Plan + **prototipo real en C#** | ✅ |
| 10 — Desarrollo completo | — | Pendiente |
| 11 — QA | — | Pendiente |

## Prototipo técnico real

`fase9-vertical-slice/` contiene una implementación temprana y funcional de los sistemas centrales, no solo documentación:

- `Scripts/Core/` — ServiceLocator, bootstrap
- `Scripts/Echo/` — grabación y reproducción de ecos (InputRecorder, EchoManager, EchoPlayer)
- `Scripts/Time/` — TimeManager con time scales por capa (bullet-time real vs. ecos inmunes a él)
- `Scripts/Player/` — controller y stats
- `Scripts/Run/` — timer de loop, controlador de sala
- `Shaders/EchoShader.shader` — shader de tinte para representar ecos

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
fase9-vertical-slice/    Plan de vertical slice + prototipo C# real
```

## Stack técnico

Unity 2022 LTS + URP · C# · FMOD Studio · Aseprite · Git/GitHub Actions + GameCI

---

*Proyecto de diseño original, sin afiliación a franquicias existentes.*
