# PHASE — Fase 7: Selección de Tecnología
> Documento de decisión del stack completo. Cada herramienta está justificada con criterios específicos para PHASE y su plataforma objetivo (iOS + Android).

---

## 1. Criterios de Evaluación

Para cada categoría se evaluaron alternativas en estas dimensiones:

| Criterio | Peso | Descripción |
|----------|------|-------------|
| Soporte móvil iOS + Android | 30% | Export nativo, rendimiento, optimización de batería |
| Fit con la mecánica de PHASE | 25% | Bullet-time, ecos cinemáticos, pixel art 480×270 |
| Ecosistema y comunidad | 20% | Assets, plugins, documentación, soporte a largo plazo |
| Curva de aprendizaje | 15% | Velocidad de ramp-up para un equipo pequeño / solo dev |
| Costo de licencias | 10% | Free tier viable para MVP e ingresos < $200K/año |

---

## 2. Motor de Juego

### Decisión: **Unity 2022 LTS (6000.0.x)**

Pre-decidida en GDD Fase 4 con justificación técnica validada.

### Matriz de alternativas

| Motor | Móvil | Pixel Art | Physics 2D | Audio nativo | Post-FX | Veredicto |
|-------|-------|-----------|------------|--------------|---------|-----------|
| **Unity 2022 LTS** | ✅ Excelente | ✅ PixelPerfectCamera | ✅ Box2D nativo | ✅ FMOD integrado | ✅ URP Bloom/Vignette | **ELEGIDO** |
| Godot 4.x | ✅ Bueno | ✅ Nativo | ✅ Bueno | ⚠️ Limitado | ⚠️ Básico | Descartado |
| Unreal Engine 5 | ⚠️ Pesado | ❌ Workflow complicado | ⚠️ Paper2D limitado | ✅ Excelente | ✅ Excelente | Descartado |
| Defold | ✅ Bueno | ✅ Bueno | ⚠️ Limitado | ⚠️ Sin FMOD | ❌ Básico | Descartado |
| Cocos2D-x | ✅ Bueno | ✅ Nativo | ✅ Box2D | ❌ Manual | ❌ Sin post-FX | Descartado |

### Por qué Unity 2022 LTS específicamente (no 2023/Unity 6)

- **LTS = Long Term Support**: parches de seguridad garantizados hasta 2025, sin features experimentales que rompan builds
- **Runtime Fee cancelado** en Unity 6 (pero el cambio de policy de 2023 confirma que 2022 LTS es el punto de estabilidad confiable)
- **PixelPerfectCamera 2D** ya madura en esta versión — sin bugs de sub-pixel que aparecieron en Unity 6 early
- **FMOD Unity Plugin** compatible y probado contra 2022 LTS

### Configuración de Unity para PHASE

```
Project Settings → Player:
  - Target Architectures: ARM64 (iOS + Android)
  - Scripting Backend: IL2CPP
  - API Compatibility: .NET Standard 2.1
  - Graphics API Android: Vulkan (primary) → OpenGLES3 (fallback)
  - Graphics API iOS: Metal

Project Settings → Quality:
  - VSync: Off (controlado por Application.targetFrameRate = 60)
  - Pixel Light Count: 0 (2D, no needed)

Project Settings → Physics 2D:
  - Gravity: (0, -20) — respuesta más arcade que -9.8
  - Simulation Mode: Update (no Fixed — bullet-time lo controlamos por Time.timeScale)

Render Pipeline: URP 14.x (incluido en Unity 2022 LTS)
```

---

## 3. Render Pipeline

### Decisión: **Universal Render Pipeline (URP) 14.x**

URP es obligatorio por tres razones específicas de PHASE:
1. **Bloom** — los ecos deben hacer glow (threshold 0.8, intensity 1.2)
2. **Vignette** — se intensifica durante bullet-time para feedback visual
3. **Chromatic Aberration** — efecto sutil en transiciones de bullet-time (intensity 0.3 → 0.8)

```
URP Asset Settings para PHASE:
  Rendering Path: Forward
  HDR: ON (requerido para Bloom)
  MSAA: Off (pixel art no necesita anti-aliasing)
  Shadow Distance: 0 (2D, sin sombras 3D)
  Post Processing: ON

Global Volume Profile (PHASE_PostProcess.asset):
  Bloom:
    Threshold: 0.8
    Intensity: 1.2
    Scatter: 0.7
    Tint: #3AFFD4 (cyan tint mínimo)
  Vignette:
    Color: #000000
    Intensity: 0.25 (normal) → 0.55 (bullet-time)
    Smoothness: 0.4
  Chromatic Aberration:
    Intensity: 0.0 (normal) → 0.35 (bullet-time)
  Color Grading (LDR):
    Mode: LDR
    Lift: slight blue shift (-0.02 en R, +0.02 en B) — world frío
    Contrast: 105%
```

---

## 4. Audio

### Decisión: **FMOD Studio 2.x + FMOD for Unity (plugin oficial)**

| Herramienta | Pros | Contras |
|-------------|------|---------|
| **FMOD Studio 2.x** | Parámetros en runtime (bullet-time filter), banco de sonidos optimizado para mobile, royalty-free < $200K revenue | Curva de aprendizaje adicional (DAW separado) |
| Unity Audio (nativo) | Zero config | Sin parámetros dinámicos, sin mezcla compleja, sin pitch/filter automation |
| Wwise | Más potente que FMOD | Más complejo, licencia más cara en escala |

**Por qué FMOD es crítico para PHASE:**
El bullet-time no es solo ralentizar `Time.timeScale`. Los sonidos del mundo deben pitch-shift hacia abajo y filtrarse con low-pass cuando el jugador activa el slow-mo. FMOD permite un parámetro global `BulletTimeAmount` (0.0 → 1.0) que todas las pistas consultan en tiempo real, sin código adicional por sonido.

```
FMOD Banks recomendados:
  Master.bank          — siempre en memoria
  UI.bank              — cargado desde Splash
  Gameplay.bank        — cargado en Run Start, descargado en Run End
  Music.bank           — streaming, no preload completo
  Ambience.bank        — streaming

Parámetros globales:
  BulletTimeAmount     0.0 - 1.0  (controlado por PlayerController)
  EchoCount            0 - 5      (suma de ecos activos)
  RunTension           0.0 - 1.0  (tiempo restante del loop, afecta música)
```

**Licencia FMOD:** Free para proyectos < $200,000 USD de ingresos brutos. A partir de ahí, plan Indie a $500/año — costo manejable.

---

## 5. Herramientas de Arte (Pipeline Pixel Art)

### Decisión: **Aseprite 1.3**

| Herramienta | Precio | Indexed color | Script API | Veredicto |
|-------------|--------|---------------|------------|-----------|
| **Aseprite 1.3** | $19.99 (único) | ✅ | ✅ Lua | **ELEGIDA** |
| LibreSprite | Gratis | ✅ | ⚠️ Limitado | Backup gratuito |
| Piskel | Gratis | ⚠️ | ❌ | No |
| Photoshop | $55/mes | ⚠️ | ⚠️ | No viable |
| Pyxel Edit | $9 | ✅ | ❌ | Aceptable pero Aseprite es mejor |

**Workflow Aseprite → Unity:**
```
1. Spritesheet exportado como PNG con tag por animación (Idle, Walk, Jump...)
2. Unity Sprite Editor: Sprite Mode = Multiple, PPU = 16, Filter = Point, Compression = None
3. Animator Controller por personaje con parámetros: Speed, IsGrounded, IsBulletTime, IsHurt
4. Echo shader toma el spritesheet del jugador (solo blanco #E8EEF8) y aplica _EchoColor en runtime
```

**Paleta de importación (.pal):**
- Cargar la paleta del archivo `fase6-arte/paletas-color.md` como índice en Aseprite
- Activar `View → Sprite Properties → Color Mode: Indexed`
- Esto impide usar colores fuera de paleta accidentalmente

---

## 6. Control de Versiones

### Decisión: **Git + GitHub (repositorio privado)**

```
Estructura de ramas:
  main          — builds estables y testeadas
  develop       — integración diaria
  feature/*     — features individuales (feature/bullet-time-vfx)
  release/*     — preparación de release (release/v1.0.0)
  hotfix/*      — fixes urgentes en producción

.gitignore crítico para Unity:
  /Library/
  /Temp/
  /Obj/
  /Build/
  /Builds/
  /UserSettings/
  *.csproj
  *.sln
  *.user

Git LFS (obligatorio para assets binarios):
  *.png track
  *.wav track
  *.mp3 track
  *.aif track
  *.bank track     (FMOD banks)
  *.psd track
```

**Por qué GitHub sobre GitLab/Bitbucket:** GitHub Actions tiene el mejor soporte para Unity CI/CD con el runner oficial de GameCI.

---

## 7. CI/CD

### Decisión: **GitHub Actions + GameCI**

[GameCI](https://game.ci/) es el proyecto open-source estándar de la industria para Unity en GitHub Actions.

```yaml
# .github/workflows/build.yml (esquema)
name: Build PHASE
on:
  push:
    branches: [develop, main]

jobs:
  build-android:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
        with: { lfs: true }
      - uses: game-ci/unity-builder@v3
        with:
          targetPlatform: Android
          unityVersion: 2022.3.x
          androidKeystoreName: phase.keystore
          buildName: PHASE

  build-ios:
    runs-on: macos-latest   # requerido por Xcode
    steps:
      - uses: actions/checkout@v3
        with: { lfs: true }
      - uses: game-ci/unity-builder@v3
        with:
          targetPlatform: iOS
          unityVersion: 2022.3.x
```

**Costo:** El plan Free de GitHub incluye 2,000 minutos/mes de Actions — suficiente para un build diario en desarrollo.

---

## 8. Analytics

### Decisión: **GameAnalytics (SDK Unity)**

| Servicio | Precio | Mobile-native | Eventos custom | Funnel analysis | Veredicto |
|---------|--------|---------------|----------------|-----------------|-----------|
| **GameAnalytics** | Gratis | ✅ | ✅ | ✅ | **ELEGIDO** |
| Firebase Analytics | Gratis | ✅ | ✅ | ⚠️ Limitado | Backup |
| Amplitude | Gratis (10M eventos/mes) | ✅ | ✅ | ✅ | Overkill para MVP |
| Unity Analytics | Gratis | ✅ | ✅ | ✅ | Ligado a Unity — riesgo de policy change |

**Eventos críticos a trackear en PHASE:**

```csharp
// Tutorial
GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start, "Tutorial", "Step1");
GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete, "Tutorial", "Step3");

// Run
GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start, "Run", $"Run_{runNumber}");
// Al morir o completar:
GameAnalytics.NewProgressionEvent(GAProgressionStatus.Fail, "Run", $"Run_{runNumber}",
    score: roomsCleared);

// Bullet-time (mecánica core — ¿cuánto la usan?)
GameAnalytics.NewDesignEvent("BulletTime:Activated", secondsHeld);

// Ecos (engagement con la mecánica central)
GameAnalytics.NewDesignEvent("Echo:Resolved", echoSlot);  // eco resolvió puzzle
GameAnalytics.NewDesignEvent("Echo:Death", echoSlot);     // eco causó muerte

// Monetización
GameAnalytics.NewBusinessEvent("USD", cents, "Skin", skinId, "Store");
```

---

## 9. Monetización

### Decisión: **Plugin de monetización nativo por plataforma + Unity Ads (rewarded only)**

**Modelo de ingresos PHASE** (definido en GDD, ética monetización):
1. **Gratis con contenido completo** — no paywall en gameplay
2. **Skins cosméticas** — ecos con diferentes apariencias (color + trail)
3. **Anuncios recompensados** — `+1 Cristal` o `+30s Tiempo Límite` por ad voluntario

**SDK Stack:**

```
Compras In-App (IAP):
  Unity IAP 4.x — capa de abstracción sobre Google Play Billing y Apple StoreKit
  Configurado con: Consumable (cristales), Non-consumable (skins permanentes)

Publicidad Recompensada:
  Unity Ads 4.x — integración nativa, el más simple para proyectos Unity
  Alternativa si RPM bajo: ironSource / MAX Mediation (agrega múltiples redes)
  Solo Rewarded Video — NUNCA intersticiales ni banners (destruyen UX en mobile)

Política de ads:
  - Max 1 oferta de ad por run
  - Solo se muestra si el jugador lo solicita activamente
  - No durante gameplay, solo en Run End screen
```

---

## 10. Crash Reporting y Monitoring

### Decisión: **Firebase Crashlytics**

```
Firebase SDK: com.google.firebase.crashlytics
- Gratis, ilimitado
- Crash reports en tiempo real con stack trace de C# + IL2CPP symbols
- Integración con GameAnalytics para correlacionar crashes con sesiones
- Dashboard en Firebase Console
```

**Configuración crítica para IL2CPP:**
```
Build Settings → Upload dSYM/mapping files para iOS
Firebase Console → App Distribution para TestFlight interno
```

---

## 11. Distribución

### Plataformas objetivo

| Plataforma | Store | SDK | Requisitos mínimos |
|------------|-------|-----|--------------------|
| Android | Google Play | Google Play Core (in-app updates) | Android 8.0+ (API 26), ARM64 |
| iOS | App Store | StoreKit 2 (via Unity IAP) | iOS 14.0+, Metal required |

```
Build Target Android:
  Minimum API: 26 (Android 8.0) — cubre 95%+ del mercado activo
  Target API: 34 (Android 14) — requerido por Google Play desde 2024
  Format: AAB (Android App Bundle) — obligatorio desde 2021
  Compression: LZ4HC

Build Target iOS:
  Minimum iOS: 14.0 — cubre 97%+ de dispositivos activos
  iPad: SUPPORTED — layout adaptable
  Required Capabilities: Metal
  Background Modes: audio (FMOD music continuación)
```

---

## 12. Stack Completo — Resumen Visual

```
┌─────────────────────────────────────────────────────────────┐
│                    PHASE TECH STACK                         │
├─────────────────────────────────────────────────────────────┤
│  ENGINE          Unity 2022 LTS + URP 14.x                  │
│  LENGUAJE        C# / .NET Standard 2.1 / IL2CPP            │
│  PHYSICS         Unity 2D Physics (Box2D) — Time.timeScale  │
│  AUDIO           FMOD Studio 2.x + FMOD for Unity           │
│  PIXEL ART       Aseprite 1.3 (paleta indexed, PPU=16)      │
│  UI DESIGN       Pencil Project (prototipos) → Unity UI     │
│  VCS             Git + GitHub (repo privado) + Git LFS      │
│  CI/CD           GitHub Actions + GameCI                     │
│  ANALYTICS       GameAnalytics SDK                          │
│  CRASH           Firebase Crashlytics                        │
│  IAP             Unity IAP 4.x (Google Play + Apple)        │
│  ADS             Unity Ads 4.x (solo Rewarded Video)        │
│  ANDROID         Google Play (AAB, API 26+, ARM64)          │
│  iOS             App Store (iOS 14+, Metal, ARM64)          │
└─────────────────────────────────────────────────────────────┘
```

---

## 13. Riesgos y Mitigaciones

| Riesgo | Probabilidad | Impacto | Mitigación |
|--------|-------------|---------|------------|
| Unity cambia licencia nuevamente | Media | Alto | Arquitectura desacoplada: game logic en C# puro, sin Unity-specfic APIs en core systems. Migración a Godot sería posible. |
| FMOD performance en low-end Android | Baja | Medio | Profiling desde el primer sprint. Fallback: Unity Audio con scripts de pitch manual. |
| Git LFS costos si los assets crecen | Media | Bajo | Plan free GitHub LFS: 1GB storage / 1GB bandwidth al mes. Suficiente para MVP. Migrar a Git Large File Storage propio si necesario. |
| GameAnalytics down / policy change | Baja | Bajo | Firebase Analytics como fallback ya configurado. |
| App Store review rejection | Media | Alto | Seguir HIG de Apple desde el diseño. Evitar mecanismos de loot box (PHASE no los tiene). |

---

## 14. Costos de Licencias

| Herramienta | Costo | Notas |
|-------------|-------|-------|
| Unity 2022 LTS | **Gratis** (ingresos < $100K/año) | Personal plan |
| FMOD Studio | **Gratis** (ingresos < $200K/año) | Indie license al crecer |
| Aseprite | **$19.99** (pago único) | |
| GitHub | **Gratis** (repo privado, 2000 min Actions) | |
| GameAnalytics | **Gratis** | |
| Firebase | **Gratis** (Spark plan, Crashlytics ilimitado) | |
| Unity Ads | **Gratis** (revenue share del ad) | |
| Google Play | **$25** (pago único, cuenta developer) | |
| Apple Developer | **$99/año** | Requerido para App Store |
| **TOTAL MVP** | **~$145 primer año** ($25 Google + $99 Apple + $20 Aseprite) | |

---

## 15. Autocrítica

**¿Qué podría estar mal en estas decisiones?**

- **Unity LTS vs Godot 4**: Godot 4 ha madurado rápidamente. Para un dev solo con tiempo ilimitado, Godot podría ser mejor a largo plazo (open-source, sin riesgo de policy). Se eligió Unity por el ecosistema FMOD maduro y la integración de PixelPerfectCamera, pero es la decisión con más "vendor lock-in" del stack.

- **FMOD para un solo dev**: FMOD Studio es una DAW completa. La curva de aprendizaje es real. Si el dev no tiene background en audio, es un riesgo de timeline. Mitigación: empezar con Unity Audio en el Vertical Slice y migrar a FMOD en Fase 10.

- **GameCI en GitHub Actions**: Requiere tener Unity activada en el runner (licencia floating). La configuración inicial puede tomar 1-2 días y hay problemas conocidos con keystore management. Alternativa simple: Unity Cloud Build ($9/mes) si CI/CD resulta problemático.

- **ARM64-only Android**: Forzar ARM64 desde el inicio excluye dispositivos muy viejos (< Android 8.0). El mercado latinoamericano y asiático tiene mayor penetración de Android low-end. Decisión: mantener ARM64 para optimización, añadir ARM32 si métricas de Play Store muestran pérdida de instalaciones > 5%.
```

---

*Fase 7 completada — Stack tecnológico decidido, documentado y costeado.*
*Próxima fase: Fase 8 — Arquitectura*
