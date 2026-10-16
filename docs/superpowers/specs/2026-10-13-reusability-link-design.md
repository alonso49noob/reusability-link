# Reusability Link — Diseño (v1)

Fecha: 2026-10-13
Juego objetivo: Kerbal Space Program 1.12.5 (build 03190), Windows, instalación con mods (FAR, Kopernicus, Parallax, Scatterer, TUFX, MechJeb2, KSPCommunityFixes, Harmony…).

## 1. Objetivo

Permitir recuperar etapas reutilizables pilotándolas a mano sin perderlas. Hoy, al separar un booster y seguir con la etapa superior, KSP descarga el booster cuando se aleja (~22,5 km en vuelo atmosférico), lo borra si está en la atmósfera, y cambiar a él implica pantalla de carga o directamente es imposible.

**Éxito en v1:** en un lanzamiento típico del usuario (booster que aterriza en el mar / barcaza a distancia moderada, etapa superior que sigue a órbita), el usuario puede:

1. Llevar la etapa superior a órbita viendo en una ventanita el estado del booster.
2. Cambiar al booster **al instante, sin pantalla de carga**, cuando la ventanita le avise.
3. Hacer el encendido de reentrada y aterrizar el booster a mano, viendo la etapa superior en la ventanita.
4. Que ninguna de las dos naves sea borrada por KSP en ningún momento del proceso.

## 2. Lo que dijo el usuario vs. supuestos

**Dicho por el usuario:**

- Pilota el booster siempre a mano, alternando entre naves (no usa pilotos automáticos para esto).
- Casi siempre hay una nave en la atmósfera y otra fuera.
- Aterriza casi siempre en el mar / barcaza lejana (distancias moderadas); muy rara vez vuelta a KSC.
- Ventanita: empezar solo con datos + navball simplificada; cámara en vivo más adelante como opción.
- Empezar con 2 naves seguidas; valorar después 3–4 (estilo Falcon Heavy).
- Seguimiento automático al separar, con opción manual, filtrado a primeras etapas con núcleo de control.
- Enfoque híbrido (física solo cuando hace falta).

**Supuestos (a validar):**

- La distancia típica entre naves cuando el booster necesita física es de pocos cientos de km como máximo.
- FAR calcula aerodinámica para cualquier nave cargada y con física, activa o no.
- Se puede mantener una nave cargada pero "sobre raíles" (empaquetada) en la atmósfera sin que KSP la destruya, o se puede impedir con Harmony.

## 3. Comportamiento

### 3.1 Seguimiento

Una nave "seguida" es una que el mod mantiene cargada y vigilada. Máximo **2** en v1 (la activa + otra). El límite es una constante de configuración para poder probar 3–4 más adelante.

**Seguimiento automático.** Al crearse una nave nueva por separación, se sigue solo si cumple todo:

1. Se separa de la nave activa.
2. Tiene al menos una pieza con `ModuleCommand` (sonda o cabina).
3. Tiene al menos un motor (`ModuleEngines` / `ModuleEnginesFX`).
4. En el momento de la separación, la nave activa está en ascenso: situación `FLYING` o `SUB_ORBITAL`, o bien `ORBITING` con periapsis por debajo del límite de la atmósfera (o del suelo, en cuerpos sin atmósfera).

Si ya hay 2 naves seguidas, la nueva no se sigue automáticamente (aviso en pantalla). El filtro automático se puede desactivar en la configuración.

**Seguimiento manual.** Desde la ventana del mod se puede seguir / dejar de seguir la nave activa o cualquier nave cargada. Sin filtro.

**Fin del seguimiento.** Una nave deja de seguirse si: el usuario la desmarca, se destruye, se recupera, o lleva `LANDED`/`SPLASHED` y casi quieta (velocidad de superficie < 0,5 m/s) durante 10 s. En ese último caso se notifica ("Booster aterrizado — seguimiento terminado") y la nave queda como una nave normal de KSP.

**Persistencia.** La marca "seguida" se guarda por nave en el archivo de partida mediante un `VesselModule` con campo persistente, de modo que sobrevive a guardar/cargar.

### 3.2 Física o raíles (política híbrida)

Para cada nave seguida **inactiva**, el mod decide en cada actualización física entre:

- **Raíles:** cargada pero empaquetada (sin física, propagación orbital exacta). Coste casi nulo, sin problemas de precisión. Cambio instantáneo posible.
- **Física:** cargada y desempaquetada (aerodinámica FAR, motores, colisiones).

La nave activa siempre tiene física (comportamiento normal de KSP).

**Regla de decisión** (valores iniciales, se ajustan tras el prototipo):

| Condición de la nave inactiva | Decisión |
|---|---|
| Presión dinámica `q` actual **o prevista en los próximos 15 s** ≥ 100 Pa | Física |
| Presión estática ≥ 1,0 kPa (umbral al que KSP destruye naves sobre raíles; cubre p. ej. el descenso con paracaídas, donde `q` es bajo) | Física |
| Algún motor de la nave encendido con empuje > 0 | Física |
| `LANDED`/`SPLASHED` y velocidad de superficie ≥ 0,5 m/s (todavía asentándose) | Física |
| Estaba en física y `q` < 50 Pa durante al menos 5 s, sin ninguna de las condiciones anteriores | Raíles |
| En cualquier otro caso | Mantener el estado actual (histéresis) |

`q = ½ · ρ · v²`, con `ρ` la densidad atmosférica en la posición de la nave y `v` la velocidad respecto a la superficie. Se calcula igual estando en raíles, a partir de la órbita. Usar `q` en vez de una altitud fija hace que la física se active antes si la nave entra rápida.

**Límite de distancia de física.** Si la política pide física pero la nave está a más de `MaxPhysicsDistance` de la activa (valor inicial **300 km**, se fija con el prototipo), la nave se queda en raíles y la ventanita muestra un aviso crítico: "Fuera de rango de física — cambia a esta nave". Al cambiar, la nave pasa a ser la activa y recupera la física con normalidad.

**Aplicación.** La decisión se aplica mediante los rangos que KSP ya trae por nave (`Vessel.vesselRanges`, para cada situación: `load`, `unload`, `pack`, `unpack`):

- Para toda nave seguida: `load` = 2.500 km, `unload` = 2.600 km → nunca se descarga.
- Física: `pack` = 2.550 km, `unpack` = 2.400 km (siempre mayores que la distancia, que nunca supera `MaxPhysicsDistance` en este modo).
- Raíles: `pack` = 2.500 m, `unpack` = 2.000 m (`RailsMinDistance`). Por debajo de 2,5 km la nave conserva la física como en KSP normal, para no perder el empuje de la separación ni empaquetar una nave pegada a la activa.

Los mismos valores se escriben en las 7 situaciones de `vesselRanges`, porque KSP evalúa `pack` con `BestSituation` y `unpack` con `situation` (en `OrbitPhysicsManager`), y `load`/`unload` en `Vessel.Update`. Se reaplican en cada actualización física por si KSP los reinicia. Al dejar de seguir una nave se restauran sus rangos originales.

### 3.3 Protección contra borrado

Verificado descompilando KSP 1.12.5: `Vessel.CheckKill()` destruye una nave si está empaquetada, no es la activa, `FlightGlobals.overrideOrbit` es `false`, no está `LANDED`/`SPLASHED`, `mainBody.GetPressure(altitude) > 1.0` kPa y está más lejos de la activa que su `pack`. La misma función destruye también naves que atraviesan el terreno; esa parte **no** se toca.

Con la política de 3.2 una nave seguida solo puede estar sobre raíles a más de 1 kPa en el caso `FUERA DE RANGO`. Para ese caso, `KillGuard` es un parche Harmony sobre `Vessel.CheckKill`: un *prefix* que, solo para naves seguidas, pone `FlightGlobals.overrideOrbit = true` y guarda el valor anterior, y un *finalizer* que lo restaura. Así se salta únicamente la rama de la presión y se conserva la del terreno.

Las naves seguidas nunca se descargan, así que las reglas de KSP para naves descargadas no les afectan mientras se esté en la escena de vuelo.

### 3.4 Cambio de nave

- Tecla configurable (por defecto **`Alt + Y`**: las teclas propias de KSP ignoran los modificadores, así que `Alt + B` también activaba los frenos; al arrancar, el mod avisa si `SwitchKey` coincide con alguna tecla de KSP) y clic en la ventanita → `FlightGlobals.ForceSetActiveVessel` a la otra nave seguida.
- Verificado en el código: `ForceSetActiveVessel` a una nave **cargada** no recarga la escena (solo guarda y recarga si `!v.loaded`), y con `force` se salta la prohibición de cambiar de nave en la atmósfera. Si estaba en raíles, KSP la desempaqueta al hacerse activa.
- Si la otra nave no está cargada por algún motivo (p. ej. tras cambiar de escena), el cambio funciona igual pero con la carga normal de KSP; no es un error.

### 3.5 Ventanita (monitor)

Ventana pequeña, movible, que recuerda su posición, visible solo en vuelo y solo si hay otra nave seguida. Se muestra/oculta con un botón en la barra de aplicaciones estándar (AppLauncher).

Contenido (de la nave seguida **no activa**):

- Nombre de la nave.
- Velocidad de superficie (m/s).
- Altitud sobre el mar y sobre el terreno (m / km).
- Velocidad vertical (m/s, con signo).
- Navball simplificada: horizonte (cabeceo y alabeo), rumbo, marcadores de prograde y retrograde de superficie.
- Estado: `RAÍLES` / `FÍSICA` / `ATERRIZADO` / `FUERA DE RANGO`.
- Distancia a la nave activa.
- Avisos (con color):
  - Informativo: "Entrando en aire denso" (cuando pasa a física).
  - Atención: "Impacto estimado en < 60 s".
  - Crítico: "Impacto estimado en < 20 s", "Fuera de rango de física — cambia a esta nave".

El tiempo a impacto se estima con la altitud sobre el terreno y la velocidad vertical. La altura del terreno se obtiene de la superficie del cuerpo (`CelestialBody.TerrainAltitude`), que no requiere que el terreno esté construido alrededor de la nave.

Implementación con IMGUI de Unity (`GUILayout.Window`), navball dibujada con primitivas simples (líneas y marcadores rotados), sin assets externos.

### 3.6 Configuración

Archivo `GameData/ReusabilityLink/Settings.cfg`:

- `MaxTrackedVessels` (2)
- `AutoTrack` (true), `AutoTrackRequireEngines` (true)
- `PhysicsOnDynamicPressure` (100 Pa), `RailsBelowDynamicPressure` (50 Pa), `RailsHoldSeconds` (5), `LookaheadSeconds` (15)
- `MaxPhysicsDistance` (300000 m), `RailsMinDistance` (2500 m), `RailsMaxStaticPressureKPa` (1.0)
- `LandedSettleSpeed` (0,5 m/s), `LandedSettleSeconds` (10)
- `SwitchKey`
- `ImpactWarnSeconds` (60), `ImpactCriticalSeconds` (20)
- Posición y visibilidad de la ventana: se guardan solas en `GameData/ReusabilityLink/PluginData/Window.cfg` (Module Manager ignora `PluginData`, así que no invalida su caché).

### 3.7 Casos límite

- **Warp sobre raíles:** KSP empaqueta todo. Si una nave seguida necesita física según la política, el mod limita el warp a warp físico y muestra un aviso.
- **Salir de la escena de vuelo** (Centro Espacial, Estación de Seguimiento): las naves se descargan como siempre. Mientras una nave seguida inactiva esté en la atmósfera y no aterrizada, la ventanita muestra de forma permanente "No salgas del vuelo: el booster puede perderse", de modo que el aviso está visible antes de salir. La marca "seguida" persiste y al volver al vuelo se reanuda el comportamiento.
- **Nave inactiva toca suelo/agua con física y lejos:** el terreno puede no estar construido alrededor de ella. Mitigación v1: los avisos de impacto. Si en el prototipo resulta que atraviesa el suelo, se documenta como limitación conocida y se valora una solución en v2.
- **Nave destruida o fusionada (acoplamiento):** se deja de seguir sin error.

## 4. Arquitectura

Solución .NET con tres proyectos:

```
C:\proj4\
  ReusabilityLink.sln
  Directory.Build.props            ← ruta a KSP (KSPDir), compartida
  src/
    ReusabilityLink.Core/          ← net472, sin referencias a KSP/Unity (KSP no trae netstandard.dll)
    ReusabilityLink/               ← net472, referencia KSP, Unity, Harmony y Core
  tests/
    ReusabilityLink.Core.Tests/    ← xUnit, prueba Core
  GameData/ReusabilityLink/
    Plugins/                       ← DLLs compiladas (se copian al compilar)
    Settings.cfg
```

Al compilar, `GameData/ReusabilityLink` se copia a la carpeta `GameData` de KSP.

### 4.1 Core (lógica pura, con tests)

| Unidad | Responsabilidad | Entradas → Salidas |
|---|---|---|
| `PhysicsPolicy` | Decisión física/raíles con histéresis | estado previo, `q` actual y prevista, motores encendidos, situación, distancia, tiempo → `Physics` / `Rails` / `OutOfRange` |
| `AutoTrackFilter` | Regla de seguimiento automático | datos de la nave nueva y de la activa (resumidos en un struct simple) → seguir sí/no + motivo |
| `TelemetryMath` | Presión dinámica, tiempo a impacto, ángulos de navball (cabeceo/rumbo/alabeo, prograde) | vectores y escalares en `double` → valores mostrables |
| `LinkSettings` | Valores de configuración y sus valores por defecto | — |

Core no conoce a KSP: recibe datos simples y devuelve decisiones. Eso permite probarlo sin abrir el juego.

### 4.2 Plugin KSP

| Unidad | Responsabilidad | Usa |
|---|---|---|
| `LinkVesselModule` | Campo persistente "seguida"; rangos originales para restaurarlos | `VesselModule`, `KSPField(isPersistant)` |
| `TrackingService` | Lista de naves seguidas; escucha la separación de piezas y aplica `AutoTrackFilter`; fin de seguimiento | `GameEvents` de separación/destrucción/recuperación |
| `RangeController` | Cada actualización física: lee estado de la nave → `PhysicsPolicy` → ajusta `vesselRanges` | `Vessel.vesselRanges`, `FlightGlobals` |
| `KillGuard` | Parches Harmony que evitan el borrado de naves seguidas | Harmony 2 (`000_Harmony`) |
| `WarpGuard` | Limita a warp físico cuando hace falta | `TimeWarp` |
| `SwitchController` | Tecla y clic → cambio instantáneo | `FlightGlobals.ForceSetActiveVessel` |
| `TelemetrySampler` | Extrae datos de una `Vessel` y los pasa a `TelemetryMath` | `Vessel`, `CelestialBody` |
| `MonitorWindow` / `MiniNavball` | Dibujo de la ventanita y la navball | IMGUI, AppLauncher |
| `SettingsLoader` | Lee/escribe `Settings.cfg` en `LinkSettings` | `ConfigNode` |

Punto de entrada: un `MonoBehaviour` con `[KSPAddon(KSPAddon.Startup.Flight, false)]` que crea y conecta los servicios.

### 4.3 Flujo de datos

```
Separación ──► TrackingService ──(AutoTrackFilter)──► LinkVesselModule.tracked = true
                                                   │
FixedUpdate ──► RangeController ──► TelemetrySampler ──► PhysicsPolicy ──► vesselRanges
                                                   │
Update/OnGUI ─► MonitorWindow ◄── TelemetrySampler/TelemetryMath (nave inactiva)
Tecla/clic ───► SwitchController ──► ForceSetActiveVessel
```

## 5. Plan de riesgo: prototipo de medición (fase 0)

Antes de construir el mod completo, un prototipo desechable (DLL mínima + botones de depuración) responde:

1. **Distancia de física:** con dos naves con física, ¿a qué distancia aparecen temblores/explosiones? Probar 50, 100, 200, 300, 500, 1000 km con un booster de prueba en caída atmosférica. Resultado → valor de `MaxPhysicsDistance`.
2. **Raíles en atmósfera:** ¿KSP borra una nave cargada y empaquetada dentro de la atmósfera? ¿Qué método lo hace? ¿Funciona el parche Harmony?
3. **Cambio instantáneo:** ¿`ForceSetActiveVessel` a una nave cargada y empaquetada a cientos de km evita la pantalla de carga?
4. **Suelo lejano:** ¿una nave inactiva con física atraviesa el terreno al tocar suelo lejos de la activa? ¿Y el agua?
5. **SAS en nave inactiva:** ¿mantiene la orientación una nave inactiva con física? (Informativo; no bloquea v1.)

El prototipo vive en una rama/carpeta aparte y no se reutiliza como código del mod. Sus resultados se anotan en este documento antes de seguir.

### 5.1 Resultados

Pendiente: la sesión de medición del usuario (`prototype/README.md`) aún no se ha hecho. Hasta entonces `MaxPhysicsDistance` (300 km) y `SwitchKey` (`Alt+Y`) se quedan en los valores iniciales, y las limitaciones de suelo lejano y SAS en nave inactiva están sin confirmar.

## 6. Pruebas

- **Automáticas (Core):** `PhysicsPolicy` (umbrales, histéresis, previsión, fuera de rango), `AutoTrackFilter` (cada una de las 4 condiciones, límite de naves), `TelemetryMath` (presión dinámica, tiempo a impacto, ángulos de navball en casos conocidos).
- **En el juego (lista de comprobación manual):**
  1. Lanzamiento con booster recuperable → al separar se sigue automáticamente; cofia y adaptadores no.
  2. Etapa superior a órbita → la ventanita muestra el booster con datos coherentes; booster en `RAÍLES` en el vacío.
  3. Booster entra en aire denso → pasa a `FÍSICA`, aviso visible.
  4. Cambio con tecla → sin pantalla de carga; la ventanita muestra ahora la etapa superior en `RAÍLES`.
  5. Aterrizaje en el mar → fin de seguimiento, nave conservada y recuperable.
  6. Guardar y cargar a mitad del proceso → la marca "seguida" persiste.
  7. Satélite desplegado en órbita → no se sigue automáticamente.

## 7. Fuera de alcance en v1

- Cámara en vivo en la ventanita (fase 2, opcional, activable con un botón).
- Más de 2 naves seguidas (se evaluará tras v1; el límite ya es configurable).
- Pilotos automáticos o control de la nave inactiva.
- Mantener física de naves seguidas fuera de la escena de vuelo.
- Compatibilidad comprobada con mods distintos de los instalados por el usuario.
