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
| `LANDED`/`SPLASHED`/`PRELAUNCH` | Lo que haga KSP normalmente (raíles si está quieta) |
| Presión dinámica `q` actual **o prevista en los próximos 15 s** ≥ 100 Pa | Física |
| Algún motor de la nave encendido con empuje > 0 | Física |
| `q` < 50 Pa durante al menos 5 s y sin motores encendidos | Raíles |
| En cualquier otro caso | Mantener el estado actual (histéresis) |

`q = ½ · ρ · v²`, con `ρ` la densidad atmosférica en la posición de la nave y `v` la velocidad respecto a la superficie. Se calcula igual estando en raíles, a partir de la órbita. Usar `q` en vez de una altitud fija hace que la física se active antes si la nave entra rápida.

**Límite de distancia de física.** Si la política pide física pero la nave está a más de `MaxPhysicsDistance` de la activa (valor inicial **300 km**, se fija con el prototipo), la nave se queda en raíles y la ventanita muestra un aviso crítico: "Fuera de rango de física — cambia a esta nave". Al cambiar, la nave pasa a ser la activa y recupera la física con normalidad.

**Aplicación.** La decisión se aplica mediante los rangos que KSP ya trae por nave (`Vessel.vesselRanges`, para cada situación: `load`, `unload`, `pack`, `unpack`):

- Para toda nave seguida: `load`/`unload` muy grandes (p. ej. 2.500 km / 2.600 km) → nunca se descarga.
- Física: `pack`/`unpack` > distancia actual.
- Raíles: `pack`/`unpack` < distancia actual.

Se respeta siempre `unpack < pack` y `load < unload`. Al dejar de seguir una nave se restauran sus rangos originales.

### 3.3 Protección contra borrado

KSP destruye naves que considera en una situación imposible: naves sobre raíles dentro de la atmósfera por debajo de cierta presión/altitud, y naves que atraviesan el terreno al cargarse. El mod debe impedir el primer caso para las naves seguidas. Los métodos exactos de KSP que lo hacen se localizarán en el prototipo (descompilando `Assembly-CSharp.dll`) y se parchearán con Harmony solo para naves seguidas.

### 3.4 Cambio de nave

- Tecla configurable (por defecto **`Alt + B`**; el prototipo comprueba que no choca con otras teclas de KSP o de los mods instalados) y clic en la ventanita → `FlightGlobals.ForceSetActiveVessel` a la otra nave seguida.
- Como la nave está cargada, no hay pantalla de carga. Si estaba en raíles, KSP la desempaqueta al hacerse activa.
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
- `MaxPhysicsDistance` (300000 m)
- `SwitchKey`
- `ImpactWarnSeconds` (60), `ImpactCriticalSeconds` (20)
- Posición y visibilidad de la ventana (se guardan solas)

### 3.7 Casos límite

- **Warp sobre raíles:** KSP empaqueta todo. Si una nave seguida necesita física según la política, el mod limita el warp a warp físico y muestra un aviso.
- **Salir de la escena de vuelo** (Centro Espacial, Estación de Seguimiento): las naves se descargan como siempre. Si una nave seguida está en la atmósfera, se avisa antes de salir. La marca "seguida" persiste y al volver al vuelo se reanuda el comportamiento.
- **Nave inactiva toca suelo/agua con física y lejos:** el terreno puede no estar construido alrededor de ella. Mitigación v1: los avisos de impacto. Si en el prototipo resulta que atraviesa el suelo, se documenta como limitación conocida y se valora una solución en v2.
- **Nave destruida o fusionada (acoplamiento):** se deja de seguir sin error.

## 4. Arquitectura

Solución .NET con tres proyectos:

```
C:\proj4\
  ReusabilityLink.sln
  Directory.Build.props            ← ruta a KSP (KSPDir), compartida
  src/
    ReusabilityLink.Core/          ← netstandard2.0, sin referencias a KSP/Unity
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
