# PHASE — Referencia de Paletas de Color
> Cheat sheet para artistas y desarrolladores. Para contexto completo ver arte-direction.md §4.

---

## PALETA UI (Definida en Fase 5 — NO modificar)
```
Background     #04060F   Fondo pantalla
Surface 1      #08101A   Cards, paneles
Surface 2      #0C1828   Tracks, elementos secundarios
Border         #1A2E45   Bordes sutiles
Accent         #3AFFD4   Cyan principal — PHASE brand, jugador
Text Primary   #E2E8F6   Texto principal
Text Secondary #8090A8   Texto secundario
Text Dim       #3A5070   Texto muy tenue
Text Faint     #2E3E58   Texto casi invisible
Danger         #FF4060   Error, daño, peligro
```

---

## PALETA MUNDO (Game World — definida en Fase 6)

### Fondos
```
#010308   Void Deep    — Capa más lejana (casi negro)
#060B14   Void Far     — Segunda capa atmospheric
#0A1222   Void Mid     — Tercera capa (aquí van micro-estrellas)
#8090A8   Star Pixel   — Color de píxeles-estrella (1×1, dispersos)
```

### Terreno
```
#0E1A2E   Stone Base   — Tile base
#080F1E   Stone Shade  — Sombra interna del tile
#111E34   Stone Mid    — Variante media
#1C2E48   Stone Rim    — Borde iluminado (borde superior)
#223548   Stone Top    — Highlight de superficie (1px superior)
#0A1818   Moss Accent  — Variante rara húmeda
```

### Personaje Jugador
```
#E8EEF8   Player Light — Zona frontal / iluminada
#B0BDD4   Player Mid   — Zona media
#6878A0   Player Dark  — Zonas en sombra
#000000   Outline      — Contorno 1px
```

### Ecos — 5 colores definitivos (NO usar en otra cosa)
```
#3AFFD4   Eco 1 — CYAN      (slot 1, el primero)
#A855F7   Eco 2 — VIOLET    (slot 2)
#F97316   Eco 3 — EMBER     (slot 3)
#22C55E   Eco 4 — VERDANT   (slot 4)
#EC4899   Eco 5 — MAGENTA   (slot 5)
```

### Peligros
```
#FF4060   Spike Base   — Pinchos y bordes letales
#FF406025 Spike Glow   — Ambient (additive blend)
#FFA030   Laser Core   — Láseres de disparo
#FFA03020 Laser Glow   — Ambient del láser
#1E2E48   Crusher      — Plataformas aplastadoras
#2A3E5A   Crusher Edge — Borde de crusher
```

### Interactivos y Coleccionables
```
#3AFFD4   Crystal Core   — Cristal (= accent UI)
#3AFFD445 Crystal Glow   — Halo ambiental
#FFFFFF90 Portal Ring    — Portal de salida
#3AFFD420 Portal Inside  — Interior del portal
#1A2E45   Plate Idle     — Plataforma presión inactiva
#3AFFD4   Plate Active   — Plataforma presión activada
#2A1A40   Gate Locked    — Puerta bloqueada
#3A2060   Gate Open      — Puerta abierta
```

---

## REGLAS DE USO RÁPIDO

| Color | ¿Puede aparecer en...? |
|-------|------------------------|
| #3AFFD4 (Accent/Eco1) | UI accent ✓, Eco 1 ✓, Cristales ✓, Plate activo ✓ — **Jamás en enemigos** |
| #FF4060 (Danger) | Pinchos ✓, Daño ✓ — **Jamás en UI positiva** |
| Colores de Eco (#A855F7, etc.) | Solo en ecos — **Jamás en terrain, UI o jugador** |
| #E8EEF8 (Player Light) | Solo en sprite jugador — el eco es este color + shader |

---

## COMPATIBILIDAD CON HERRAMIENTAS

### Unity
- Todos los hex importar como sRGB
- Bloom threshold: 0.8 → solo objetos con valor > 200 hacen glow en post-processing
- Sorting layers siguen el orden del arte-direction.md §3.3

### Aseprite / Libresprite (pixel art)
- Importar paleta como archivo `.pal` o `.gpl`
- Activar: View → Color Mode → Indexed con la paleta cargada
- Esto garantiza que no escalen fuera de paleta accidentalmente

### Pencil (Prototipo UI)
- Paleta ya aplicada en PHASE-UI.ep (ver Fase 5)
