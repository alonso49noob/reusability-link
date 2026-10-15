# Reusability Link — Plan de implementación (v1)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Mod de KSP 1.12.5 que mantiene cargadas y vivas dos naves (etapa superior + booster), las alterna sin pantalla de carga y muestra la nave inactiva en una ventanita con datos y navball simplificada.

**Architecture:** Lógica pura (política física/raíles, regla de seguimiento, rangos, telemetría, configuración) en `ReusabilityLink.Core`, sin referencias a KSP y con tests xUnit. El plugin `ReusabilityLink` traduce el estado de KSP a esas entradas y aplica las salidas a través de `Vessel.vesselRanges`, `FlightGlobals.ForceSetActiveVessel` y un parche Harmony sobre `Vessel.CheckKill`. Antes del plugin, un prototipo desechable (`RLProbe`) mide en la instalación real del usuario los riesgos del §5 de la especificación.

**Tech Stack:** C# (LangVersion 9), .NET Framework 4.7.2 (todos los proyectos; KSP no trae `netstandard.dll`), SDK .NET 10, xUnit 2, Harmony 2.2 (ya instalado en `GameData/000_Harmony`), Unity IMGUI.

**Spec:** `docs/superpowers/specs/2026-10-13-reusability-link-design.md`

## Global Constraints

- KSP 1.12.5 en `C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program` (propiedad MSBuild `KSPDir`, sobrescribible con la variable de entorno `KSPDIR`).
- Todos los proyectos: `net472`, `LangVersion 9.0`, sin `record`/`init` (necesitan `IsExternalInit`) ni otras características que requieran runtime nuevo.
- `ReusabilityLink.Core` no referencia ningún ensamblado de KSP ni de Unity.
- Referencias a KSP/Unity/Harmony con `Private=false` (no se copian a la salida).
- Carpeta del mod: `GameData/ReusabilityLink/` con `Plugins/`, `Settings.cfg` y `PluginData/` (estado de ventana).
- Valores por defecto (spec §3.6): `MaxTrackedVessels=2`, `AutoTrack=true`, `AutoTrackRequireEngines=true`, `PhysicsOnDynamicPressure=100`, `RailsBelowDynamicPressure=50`, `RailsHoldSeconds=5`, `LookaheadSeconds=15`, `MaxPhysicsDistance=300000`, `RailsMinDistance=2500`, `RailsMaxStaticPressureKPa=1.0`, `LandedSettleSpeed=0.5`, `LandedSettleSeconds=10`, `ImpactWarnSeconds=60`, `ImpactCriticalSeconds=20`, `SwitchKey=Alt+B`.
- Rangos (spec §3.2): naves seguidas `load=2 500 000`, `unload=2 600 000`; física `pack=2 550 000`, `unpack=2 400 000`; raíles `pack=RailsMinDistance`, `unpack=RailsMinDistance-500`. Mismos valores en las 7 situaciones.
- Textos visibles al usuario en español.
- Logs con prefijo `[ReusabilityLink]` (o `[RLProbe]` en el prototipo).
- Compilar con KSP **cerrado** (si no, la DLL desplegada está bloqueada).
- Los commits terminan con `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Review Focus

1. **Escribir en un campo de texto** (renombrar nave, buscar piezas) con `Alt+B`: no debe cambiar de nave. → Task 9, paso de prueba en el juego.
2. **Guardado rápido / carga rápida / volver al lanzamiento** con dos naves seguidas: la marca persiste tras cargar, y tras volver al lanzamiento no quedan naves fantasma seguidas. → Task 7, paso de prueba en el juego.
3. **Nave seguida destruida, acoplada o fusionada** (incluida la activa estrellándose): el mod deja de seguirla sin excepciones en `KSP.log` y la ventanita se oculta o cambia de nave. → Task 7, paso de prueba en el juego.
4. **`Settings.cfg` con decimales con coma** (`0,5`) o valores basura: se usan los valores por defecto para esas claves y se avisa en el log, sin romper la carga. → Task 2, test `FromValues_InvalidNumber_UsesDefaultAndWarns`.
5. **Cuerpos sin atmósfera** (Mun, planetas de Kopernicus): `q=0` y presión 0 no deben impedir seguir ni aterrizar; la regla de ascenso usa periapsis < 0. → Task 4 test `Evaluate_AirlessBody_UsesSurfaceAsLimit`, Task 3 test `Step_LandedSettledOnAirless_GoesRails`.

---

### Task 1: Base de compilación + prototipo de medición `RLProbe` (fase 0)

Prototipo **desechable**: no se reutiliza su código. Mide los 5 puntos del §5 de la especificación en el juego del usuario.

**Files:**
- Create: `Directory.Build.props`, `.gitignore`
- Create: `prototype/RLProbe/RLProbe.csproj`, `prototype/RLProbe/ProbeAddon.cs`, `prototype/RLProbe/ProbeKillGuard.cs`
- Create: `prototype/README.md` (procedimiento de medición, en español)

**Interfaces:**
- Produces: `Directory.Build.props` con `KSPDir`, `KSPManaged` (`$(KSPDir)\KSP_x64_Data\Managed`), `HarmonyDll` (`$(KSPDir)\GameData\000_Harmony\0Harmony.dll`), `TargetFramework=net472`, `LangVersion=9.0`, `Nullable=disable`. Todas las tareas posteriores lo heredan.

- [ ] **Step 1: Crear `Directory.Build.props` y `.gitignore`**

```xml
<Project>
  <PropertyGroup>
    <KSPDir Condition="'$(KSPDIR)' != ''">$(KSPDIR)</KSPDir>
    <KSPDir Condition="'$(KSPDir)' == ''">C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program</KSPDir>
    <KSPManaged>$(KSPDir)\KSP_x64_Data\Managed</KSPManaged>
    <HarmonyDll>$(KSPDir)\GameData\000_Harmony\0Harmony.dll</HarmonyDll>
    <TargetFramework>net472</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <Nullable>disable</Nullable>
  </PropertyGroup>
</Project>
```

`.gitignore`: `bin/`, `obj/`, `.vs/`, `*.user`, `GameData/ReusabilityLink/Plugins/*.dll`, `GameData/ReusabilityLink/PluginData/`.

- [ ] **Step 2: Crear `RLProbe.csproj`**

Referencias (HintPath en `$(KSPManaged)`, `Private=false`): `Assembly-CSharp`, `Assembly-CSharp-firstpass`, `UnityEngine`, `UnityEngine.CoreModule`, `UnityEngine.IMGUIModule`, `UnityEngine.InputLegacyModule`, `UnityEngine.PhysicsModule`, `UnityEngine.TextRenderingModule`; y `0Harmony` desde `$(HarmonyDll)`. Target `AfterBuild` que copia `RLProbe.dll` a `$(KSPDir)\GameData\RLProbe\Plugins\`.

- [ ] **Step 3: Implementar `ProbeAddon` (`[KSPAddon(KSPAddon.Startup.Flight, false)]`)**

- `Alt+P` muestra u oculta una ventana IMGUI.
- Lista las naves de `FlightGlobals.VesselsLoaded` distintas de la activa. Columnas: nombre, distancia (km), `situation`, `packed`, presión estática `mainBody.GetPressure(altitude)` (kPa), `q` (Pa, `0.5·ρ·srfSpeed²` con `ρ = mainBody.GetDensity(p, mainBody.GetTemperature(altitude))`), y *jitter* (m).
- Botones por nave:
  - **Mantener cargada:** `load`/`unload` = 2 500 000 / 2 600 000 en las 7 situaciones.
  - **FÍSICA:** `pack`/`unpack` = 2 550 000 / 2 400 000.
  - **RAÍLES:** `pack`/`unpack` = 2 500 / 2 000.
  - **Cambiar a esta:** `FlightGlobals.ForceSetActiveVessel`.
  - **KillGuard on/off** para esa nave.
- *Jitter*: al desempaquetarse la nave, se guarda para cada pieza `rootPart.transform.InverseTransformPoint(part.transform.position)`. En cada `FixedUpdate` se calcula la desviación máxima respecto a esa foto, y se muestra el máximo del último segundo.
- Una línea de log por segundo y por nave marcada: `[RLProbe] {nombre} d={km:F1}km packed={bool} p={kPa:F2} q={Pa:F0} jitter={m:F3}`.

- [ ] **Step 4: Implementar `ProbeKillGuard`**

Harmony `Prefix` y `Finalizer` sobre `Vessel.CheckKill`. Si la nave está en el `HashSet<Guid>` protegido, el prefix guarda `FlightGlobals.overrideOrbit` en `__state` y lo pone a `true`; el finalizer lo restaura. Se registra en `Awake` con `new Harmony("rlprobe").PatchAll()` y se quita en `OnDestroy` con `UnpatchAll("rlprobe")`.

- [ ] **Step 5: Compilar y desplegar**

Run: `dotnet build prototype/RLProbe/RLProbe.csproj -c Release`
Expected: `Compilación correcta` / `Build succeeded`, y existe `<KSPDir>\GameData\RLProbe\Plugins\RLProbe.dll`.

- [ ] **Step 6: Escribir `prototype/README.md`**

El procedimiento para el usuario, una sección por cada pregunta del §5:

1. **Distancia de física:** separar un booster con sonda, pulsar *Mantener cargada* y *FÍSICA*, y seguir con la etapa superior. Anotar el *jitter* y si hay explosiones a 50/100/200/300/500/1000 km.
2. **Raíles en atmósfera:** con el booster por debajo de ~25 km (más de 1 kPa), pulsar *RAÍLES* sin KillGuard y comprobar que KSP lo destruye (mensaje "was on-rails at … kPa" en `KSP.log`). Repetir con KillGuard activo y comprobar que sobrevive.
3. **Cambio instantáneo:** con el booster en *RAÍLES* a más de 100 km, pulsar *Cambiar a esta*. Anotar si hay pantalla de carga y el tiempo aproximado.
4. **Suelo lejano:** dejar que el booster con *FÍSICA* toque tierra y luego agua a más de 50 km de la activa. Anotar si atraviesa el suelo.
5. **SAS:** con el SAS del booster activado antes de cambiar de nave, observar si mantiene la orientación mientras está inactivo.
6. **Tecla:** pulsar `Alt+B` en vuelo y anotar si algún mod o KSP reacciona.

- [ ] **Step 7: Commit**

```bash
git add Directory.Build.props .gitignore prototype
git commit -m "chore: add build props and RLProbe measurement prototype"
```

- [ ] **Step 8: PARADA — sesión de medición del usuario**

El usuario ejecuta `prototype/README.md` en KSP y comparte sus notas y `KSP.log`. Los resultados se anotan en la especificación, en una subsección §5.1 "Resultados", y se fijan `MaxPhysicsDistance` y la tecla por defecto. **Las Tasks 2–5 (Core) pueden avanzar mientras tanto. La Task 6 no empieza sin estos resultados.** Si la pregunta 2 o la 3 falla, se para y se revisa el diseño con el usuario.

Al terminar, borrar `<KSPDir>\GameData\RLProbe`.

---

### Task 2: Solución, proyecto Core y `LinkSettings`

**Files:**
- Create: `ReusabilityLink.sln`
- Create: `src/ReusabilityLink.Core/ReusabilityLink.Core.csproj`, `src/ReusabilityLink.Core/LinkSettings.cs`, `src/ReusabilityLink.Core/KeyChord.cs`
- Create: `tests/ReusabilityLink.Core.Tests/ReusabilityLink.Core.Tests.csproj` (xUnit 2.x, `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`), `tests/ReusabilityLink.Core.Tests/LinkSettingsTests.cs`
- Create: `GameData/ReusabilityLink/Settings.cfg`

**Interfaces:**
- Produces:
  - `public sealed class LinkSettings`: propiedades `int MaxTrackedVessels`; `bool AutoTrack, AutoTrackRequireEngines`; `double PhysicsOnDynamicPressure, RailsBelowDynamicPressure, RailsHoldSeconds, LookaheadSeconds, MaxPhysicsDistance, RailsMinDistance, RailsMaxStaticPressureKPa, LandedSettleSpeed, LandedSettleSeconds, ImpactWarnSeconds, ImpactCriticalSeconds`; `KeyChord SwitchKey`. Inicializadas con los valores de Global Constraints.
  - Constantes `float TrackedLoadDistance=2_500_000f, TrackedUnloadDistance=2_600_000f, PhysicsPackDistance=2_550_000f, PhysicsUnpackDistance=2_400_000f`.
  - `public static LinkSettings FromValues(IEnumerable<KeyValuePair<string,string>> values, IList<string> warnings)`.
  - `public struct KeyChord { bool Alt; bool Ctrl; bool Shift; string Key; static bool TryParse(string text, out KeyChord chord); override string ToString(); }`.

- [ ] **Step 1: Escribir los tests que fallan** (`LinkSettingsTests.cs`)

```csharp
[Fact] public void Defaults_MatchSpec() {
    var s = new LinkSettings();
    Assert.Equal(2, s.MaxTrackedVessels);
    Assert.Equal(100, s.PhysicsOnDynamicPressure);
    Assert.Equal(50, s.RailsBelowDynamicPressure);
    Assert.Equal(300000, s.MaxPhysicsDistance);
    Assert.Equal(1.0, s.RailsMaxStaticPressureKPa);
    Assert.Equal("Alt+B", s.SwitchKey.ToString());
}
[Fact] public void FromValues_ParsesInvariantNumbersAndBools() {
    var w = new List<string>();
    var s = LinkSettings.FromValues(new Dictionary<string,string>{
        ["MaxPhysicsDistance"]="250000.5", ["AutoTrack"]="False", ["SwitchKey"]="Ctrl+Shift+K" }, w);
    Assert.Equal(250000.5, s.MaxPhysicsDistance);
    Assert.False(s.AutoTrack);
    Assert.True(s.SwitchKey.Ctrl && s.SwitchKey.Shift && !s.SwitchKey.Alt);
    Assert.Equal("K", s.SwitchKey.Key);
    Assert.Empty(w);
}
[Fact] public void FromValues_InvalidNumber_UsesDefaultAndWarns() {
    var w = new List<string>();
    var s = LinkSettings.FromValues(new Dictionary<string,string>{
        ["LandedSettleSpeed"]="0,5", ["MaxTrackedVessels"]="dos" }, w);
    Assert.Equal(0.5, s.LandedSettleSpeed);   // valor por defecto, no 5
    Assert.Equal(2, s.MaxTrackedVessels);
    Assert.Equal(2, w.Count);
    Assert.Contains("LandedSettleSpeed", w[0]);
}
[Fact] public void FromValues_UnknownKey_IsIgnoredWithWarning() { /* "Foo"="1" → 1 aviso, ajustes por defecto */ }
[Theory] [InlineData("Alt+B", true, false, false, "B")] [InlineData("b", false, false, false, "B")]
public void KeyChord_Parses(string text, bool alt, bool ctrl, bool shift, string key) { /* TryParse true y campos iguales */ }
[Theory] [InlineData("")] [InlineData("Alt+")] [InlineData("Alt+B+C")]
public void KeyChord_RejectsInvalid(string text) { /* TryParse false */ }
```

- [ ] **Step 2: Ejecutar y ver que fallan**

Run: `dotnet test tests/ReusabilityLink.Core.Tests`
Expected: error de compilación (`LinkSettings` no existe).

- [ ] **Step 3: Implementar `LinkSettings` y `KeyChord`**

- Los números se parsean con `CultureInfo.InvariantCulture` y `NumberStyles.Float`. Un valor que contiene `,` se trata como inválido.
- Claves sin distinguir mayúsculas.
- Un valor inválido deja el valor por defecto y añade un aviso `"<Clave>: valor inválido '<valor>', se usa <defecto>"`.
- `KeyChord.Key` se guarda en mayúsculas. Los modificadores válidos son `Alt`, `Ctrl` y `Shift`, y debe haber exactamente una tecla final.

- [ ] **Step 4: Crear `GameData/ReusabilityLink/Settings.cfg`**

Un nodo `REUSABILITY_LINK { ... }` con todas las claves y sus valores por defecto, y un comentario `//` en español en cada una.

- [ ] **Step 5: Ejecutar los tests**

Run: `dotnet test tests/ReusabilityLink.Core.Tests`
Expected: todos PASS.

- [ ] **Step 6: Commit**

```bash
git add ReusabilityLink.sln src tests GameData
git commit -m "feat(core): add LinkSettings and KeyChord with settings file"
```

---

### Task 3: `PhysicsPolicy` y `RangePlanner`

**Files:**
- Create: `src/ReusabilityLink.Core/PhysicsPolicy.cs` (`LinkMode`, `PolicyState`, `PolicyInput`, `PhysicsPolicy`)
- Create: `src/ReusabilityLink.Core/RangePlanner.cs` (`RangeValues`, `RangePlanner`)
- Test: `tests/ReusabilityLink.Core.Tests/PhysicsPolicyTests.cs`, `tests/ReusabilityLink.Core.Tests/RangePlannerTests.cs`

**Interfaces:**
- Consumes: `LinkSettings` (Task 2).
- Produces:
  - `public enum LinkMode { Physics, Rails, OutOfRange }`
  - `public sealed class PolicyState { public LinkMode Mode = LinkMode.Physics; public double CalmSeconds; }`
  - `public struct PolicyInput { public double DynamicPressurePa, PredictedDynamicPressurePa, StaticPressureKPa, SurfaceSpeed, DistanceM; public bool EngineThrusting, LandedOrSplashed; }`
  - `public static LinkMode PhysicsPolicy.Step(PolicyState state, PolicyInput input, double dt, LinkSettings s)`: actualiza `state` y devuelve el nuevo modo.
  - `public struct RangeValues { public float Load, Unload, Pack, Unpack; }`
  - `public static RangeValues RangePlanner.ForMode(LinkMode mode, LinkSettings s)`

- [ ] **Step 1: Escribir los tests que fallan**

Todos con `new LinkSettings()`. `In(...)` es un helper del test que construye un `PolicyInput` con distancia 10 000 m y el resto a 0/false salvo lo indicado.

```csharp
[Fact] public void Step_HighQ_FromRails_GoesPhysics()            // Rails, q=150 → Physics
[Fact] public void Step_PredictedQ_GoesPhysicsEarly()            // Rails, q=10, predicted=120 → Physics
[Fact] public void Step_StaticPressureAbove1kPa_KeepsPhysics()   // Physics, q=10, p=1.2, dt=10 → Physics
[Fact] public void Step_EngineThrusting_KeepsPhysics()           // Physics, q=0, engine=true, dt=10 → Physics
[Fact] public void Step_LandedMoving_KeepsPhysics()              // Physics, landed, speed=2, p=0, dt=10 → Physics
[Fact] public void Step_LandedSettledOnAirless_GoesRails()       // Physics, landed, speed=0.1, p=0: dt=4 → Physics; dt=1.5 → Rails
[Fact] public void Step_CalmFor5Seconds_GoesRails()              // Physics, q=40: dt=4 → Physics; dt=1.5 → Rails
[Fact] public void Step_QBetweenThresholds_ResetsCalm()          // Physics: q=40 dt=4; q=70 dt=1; q=40 dt=4 → Physics
[Fact] public void Step_FromRails_QBelowOnThreshold_StaysRails() // Rails, q=80 → Rails
[Fact] public void Step_NeedsPhysicsButFar_IsOutOfRange()        // q=150, d=300001 → OutOfRange; same with d=299000 → Physics
[Fact] public void Step_OutOfRange_Calm_GoesRails()              // OutOfRange, q=10, p=0 → Rails
```

```csharp
[Fact] public void ForMode_Physics_UsesHugePackRanges() {
    var r = RangePlanner.ForMode(LinkMode.Physics, new LinkSettings());
    Assert.Equal((2_500_000f, 2_600_000f, 2_550_000f, 2_400_000f), (r.Load, r.Unload, r.Pack, r.Unpack));
}
[Theory] [InlineData(LinkMode.Rails)] [InlineData(LinkMode.OutOfRange)]
public void ForMode_RailsLike_UsesMinDistance(LinkMode m) { /* Pack=2500, Unpack=2000, Load/Unload como arriba */ }
[Theory] [InlineData(LinkMode.Physics)] [InlineData(LinkMode.Rails)] [InlineData(LinkMode.OutOfRange)]
public void ForMode_Invariants(LinkMode m) { /* Unpack < Pack && Load < Unload */ }
```

- [ ] **Step 2: Ejecutar y ver que fallan**

Run: `dotnet test tests/ReusabilityLink.Core.Tests`
Expected: error de compilación.

- [ ] **Step 3: Implementar `PhysicsPolicy.Step`**

Algoritmo (spec §3.2):

```
forced = q ≥ OnQ || predictedQ ≥ OnQ || staticKPa ≥ RailsMaxStaticPressureKPa
         || engine || (landed && speed ≥ LandedSettleSpeed)
if forced:                       wants = true;  state.CalmSeconds = 0
else if state.Mode == Physics:
    if q < RailsBelowQ:          state.CalmSeconds += dt; wants = state.CalmSeconds < RailsHoldSeconds
    else:                        state.CalmSeconds = 0;   wants = true
else:                            wants = false
state.Mode = !wants ? Rails : (distance > MaxPhysicsDistance ? OutOfRange : Physics)
return state.Mode
```

Si `wants` es true pero el modo resulta `OutOfRange`, `CalmSeconds` vuelve a 0 (solo se cuenta la calma estando en física).

- [ ] **Step 4: Implementar `RangePlanner.ForMode`**

Usa las constantes de `LinkSettings`. En raíles: `Pack = RailsMinDistance`, `Unpack = RailsMinDistance - 500`.

- [ ] **Step 5: Ejecutar los tests**

Run: `dotnet test tests/ReusabilityLink.Core.Tests`
Expected: todos PASS.

- [ ] **Step 6: Commit**

```bash
git add src/ReusabilityLink.Core tests/ReusabilityLink.Core.Tests
git commit -m "feat(core): add hybrid physics policy and range planner"
```

---

### Task 4: `AutoTrackFilter` y `LandingSettle`

**Files:**
- Create: `src/ReusabilityLink.Core/AutoTrackFilter.cs` (`FlightSituation`, `SeparationInfo`, `AutoTrackResult`, `AutoTrackFilter`)
- Create: `src/ReusabilityLink.Core/LandingSettle.cs`
- Test: `tests/ReusabilityLink.Core.Tests/AutoTrackFilterTests.cs`, `tests/ReusabilityLink.Core.Tests/LandingSettleTests.cs`

**Interfaces:**
- Consumes: `LinkSettings`.
- Produces:
  - `public enum FlightSituation { Prelaunch, Landed, Splashed, Flying, SubOrbital, Orbiting, Escaping, Docked }`
  - `public struct SeparationInfo { public bool FromActiveVessel, HasCommandModule, HasEngine, ActiveIsTracked; public FlightSituation ActiveSituation; public double ActivePeriapsisM, AtmosphereDepthM; public int TrackedCount; }`. `ActivePeriapsisM` es la altitud del periapsis sobre el nivel del mar, y `AtmosphereDepthM` vale 0 en cuerpos sin atmósfera.
  - `public enum AutoTrackResult { Track, Disabled, NotFromActive, NoCommand, NoEngine, NotAscending, LimitReached }`
  - `public static AutoTrackResult AutoTrackFilter.Evaluate(SeparationInfo info, LinkSettings s)`. Comprueba en este orden: `Disabled`, `NotFromActive`, `NoCommand`, `NoEngine` (solo si `AutoTrackRequireEngines`), `NotAscending`, `LimitReached`. Hay ascenso si la situación es `Flying` o `SubOrbital`, o si es `Orbiting` con `ActivePeriapsisM < AtmosphereDepthM`. Se alcanza el límite si `TrackedCount + (ActiveIsTracked ? 0 : 1) + 1 > MaxTrackedVessels`.
  - `public sealed class LandingSettle { public bool Update(bool landedOrSplashed, double surfaceSpeed, double dt, LinkSettings s); public void Reset(); }`. Devuelve true cuando acumula `LandedSettleSeconds` seguidos aterrizada y con velocidad < `LandedSettleSpeed`. El contador vuelve a 0 si deja de cumplirse.

- [ ] **Step 1: Escribir los tests que fallan**

`Booster()` es un helper que devuelve un `SeparationInfo` válido: viene de la activa, con mando y motor, situación `SubOrbital`, periapsis -500 000, atmósfera 70 000, `TrackedCount=0`, activa no seguida.

```csharp
[Fact] public void Evaluate_TypicalBooster_Tracks()                     // → Track
[Fact] public void Evaluate_AutoTrackDisabled_ReturnsDisabled()
[Fact] public void Evaluate_NotFromActive_IsRejected()                  // p. ej. una pieza que suelta el propio booster
[Fact] public void Evaluate_NoCommand_IsRejected()                      // cofia, adaptador
[Fact] public void Evaluate_NoEngine_IsRejected_UnlessDisabledInSettings()
[Fact] public void Evaluate_SatelliteInStableOrbit_NotAscending()       // Orbiting, Pe=100 000, atm=70 000
[Fact] public void Evaluate_OrbitingButPeInsideAtmosphere_Tracks()      // Orbiting, Pe=40 000 → Track
[Fact] public void Evaluate_AirlessBody_UsesSurfaceAsLimit()            // atm=0: Orbiting Pe=-1000 → Track; Pe=5000 → NotAscending
[Fact] public void Evaluate_SecondBoosterWithTwoTracked_LimitReached()  // TrackedCount=2, ActiveIsTracked=true
```

```csharp
[Fact] public void Update_LandedStillFor10s_ReturnsTrue()     // 9 s → false; +1.5 s → true
[Fact] public void Update_MovingResetsTimer()                 // 8 s quieta, 1 s a 2 m/s, 8 s quieta → false
[Fact] public void Update_NotLanded_NeverTrue()
```

- [ ] **Step 2: Ejecutar y ver que fallan**

Run: `dotnet test tests/ReusabilityLink.Core.Tests`
Expected: error de compilación.

- [ ] **Step 3: Implementar `AutoTrackFilter.Evaluate` y `LandingSettle`** según las Interfaces.

- [ ] **Step 4: Ejecutar los tests**

Run: `dotnet test tests/ReusabilityLink.Core.Tests`
Expected: todos PASS.

- [ ] **Step 5: Commit**

```bash
git add src/ReusabilityLink.Core tests/ReusabilityLink.Core.Tests
git commit -m "feat(core): add auto-track filter and landing settle timer"
```

---

### Task 5: `TelemetryMath` y `Vec3`

Todos los vectores llegan en un marco local **ENU dextrógiro** (x = este, y = norte, z = arriba). El plugin se encarga de convertir; así Core no depende de la orientación del sistema de coordenadas de Unity, que es levógiro.

**Files:**
- Create: `src/ReusabilityLink.Core/Vec3.cs`, `src/ReusabilityLink.Core/TelemetryMath.cs`
- Test: `tests/ReusabilityLink.Core.Tests/TelemetryMathTests.cs`

**Interfaces:**
- Produces:
  - `public readonly struct Vec3 { double X, Y, Z; Vec3(double,double,double); static double Dot(Vec3,Vec3); static Vec3 Cross(Vec3,Vec3); double Length { get; } Vec3 Normalized(); operators + - (Vec3*double) }`
  - `public enum ImpactLevel { None, Warn, Critical }`
  - `public struct Attitude { public double PitchDeg, HeadingDeg, RollDeg; }`
  - `public struct NavballMarker { public double X, Y; public bool Visible; }`
  - `TelemetryMath`:
    - `static double DynamicPressure(double densityKgM3, double speedMs)` → `0.5·ρ·v²`
    - `static double? SecondsToImpact(double heightAboveTerrainM, double verticalSpeedMs)` → `h / -vs` si `vs < 0` y `h > 0`; si no, `null`
    - `static ImpactLevel ClassifyImpact(double? seconds, LinkSettings s)` → `Critical` si < 20, `Warn` si < 60, `None` en otro caso
    - `static Attitude ComputeAttitude(Vec3 noseEnu, Vec3 topEnu)`
    - `static NavballMarker ProjectMarker(Vec3 directionEnu, Vec3 noseEnu, Vec3 topEnu)`

- [ ] **Step 1: Escribir los tests que fallan** (tolerancia 1e-6, ángulos en grados)

```csharp
[Fact] public void DynamicPressure_Basic() => Assert.Equal(1800, TelemetryMath.DynamicPressure(0.0009, 2000), 6);
[Fact] public void SecondsToImpact_Falling() => Assert.Equal(50, TelemetryMath.SecondsToImpact(5000, -100).Value, 6);
[Fact] public void SecondsToImpact_Climbing_IsNull() => Assert.Null(TelemetryMath.SecondsToImpact(5000, 10));
[Theory] [InlineData(70.0, ImpactLevel.None)] [InlineData(59.0, ImpactLevel.Warn)] [InlineData(19.0, ImpactLevel.Critical)]
public void ClassifyImpact_Thresholds(double s, ImpactLevel e) { /* ... */ }
[Fact] public void ClassifyImpact_Null_IsNone() { /* ... */ }
[Fact] public void Attitude_LevelNorth()      // nose=(0,1,0) top=(0,0,1) → pitch 0, heading 0, roll 0
[Fact] public void Attitude_LevelEast()       // nose=(1,0,0) top=(0,0,1) → heading 90
[Fact] public void Attitude_Pitch45North()    // nose=norm(0,1,1) top=norm(0,-1,1) → pitch 45, heading 0, roll 0
[Fact] public void Attitude_RolledRight90()   // nose=(0,1,0) top=(1,0,0) → roll +90
[Fact] public void Attitude_Vertical_HeadingFromTop() // nose=(0,0,1) top=(0,1,0) → pitch 90, heading 180 (de -top), roll 0
[Fact] public void Marker_AlongNose_IsCenter()        // dir=nose → (0,0) visible
[Fact] public void Marker_RightOfNose()               // nose=(0,1,0) top=(0,0,1), dir=norm(1,1,0) → X≈0.7071, Y=0, visible
[Fact] public void Marker_Behind_IsNotVisible()       // dir=-nose → Visible=false
```

- [ ] **Step 2: Ejecutar y ver que fallan**

Run: `dotnet test tests/ReusabilityLink.Core.Tests`
Expected: error de compilación.

- [ ] **Step 3: Implementar**

- **Cabeceo:** `asin(nose.z)`.
- **Rumbo:** `atan2(nose.x, nose.y)` normalizado a `[0,360)`. Si la proyección horizontal de `nose` mide menos que `sin(0.5°)`, se usa `-top` en lugar de `nose` cuando el cabeceo es positivo y `top` cuando es negativo. Así una nave vertical toma el rumbo hacia el que mira su lado inferior, que es la convención de la navball de KSP.
- **Alabeo:** `r = up - nose·dot(up,nose)`. Si `|r|` < 1e-6, alabeo 0. Si no, `atan2(dot(cross(r, top), nose), dot(r, top))`.
- **Marcador:** `right = cross(nose, top)`, `X = dot(d, right)`, `Y = dot(d, top)`, `Visible = dot(d, nose) ≥ 0`.

- [ ] **Step 4: Ejecutar los tests**

Run: `dotnet test tests/ReusabilityLink.Core.Tests`
Expected: todos PASS.

- [ ] **Step 5: Commit**

```bash
git add src/ReusabilityLink.Core tests/ReusabilityLink.Core.Tests
git commit -m "feat(core): add telemetry math (dynamic pressure, impact, navball)"
```

---

### Task 6: Proyecto plugin, despliegue, configuración y `LinkVesselModule`

**Requisito:** resultados de la Task 1 anotados en la especificación (§5.1) y `MaxPhysicsDistance` y `SwitchKey` por defecto actualizados en `LinkSettings`, en `Settings.cfg` y en el test `Defaults_MatchSpec`.

**Files:**
- Create: `src/ReusabilityLink/ReusabilityLink.csproj`
- Create: `src/ReusabilityLink/LinkAddon.cs`, `src/ReusabilityLink/SettingsLoader.cs`, `src/ReusabilityLink/LinkVesselModule.cs`, `src/ReusabilityLink/Log.cs`
- Modify: `ReusabilityLink.sln` (añadir el proyecto)

**Interfaces:**
- Consumes: `LinkSettings.FromValues` (Task 2).
- Produces:
  - `public static class Log { static void Info(string m); static void Warn(string m); static void Error(string m); }`, con prefijo `[ReusabilityLink]`.
  - `public static class SettingsLoader { static LinkSettings Load(); static void SaveWindow(Rect r, bool visible); static bool TryLoadWindow(out Rect r, out bool visible); }`. Lee el primer nodo `REUSABILITY_LINK` de `GameDatabase.Instance.GetConfigNodes`, o en su defecto `ConfigNode.Load` sobre `KSPUtil.ApplicationRootPath + "GameData/ReusabilityLink/Settings.cfg"`. La ventana se guarda en `GameData/ReusabilityLink/PluginData/Window.cfg`.
  - `public class LinkVesselModule : VesselModule`: `[KSPField(isPersistant = true)] public bool tracked`, `[KSPField(isPersistant = true)] public bool rangesSaved`, una copia en memoria `VesselRanges originalRanges`, y los métodos `void SaveOriginalRanges()` (copia `vessel.vesselRanges` con `new VesselRanges(...)` solo si `!rangesSaved`) y `void RestoreOriginalRanges()` (si no hay copia, usa `new VesselRanges(PhysicsGlobals.Instance.VesselRangesDefault)`).
  - `[KSPAddon(KSPAddon.Startup.Flight, false)] public class LinkAddon : MonoBehaviour`: punto de entrada. Expone `public static LinkAddon Instance` y `public LinkSettings Settings`. Las tareas siguientes le añaden servicios.

- [ ] **Step 1: Crear `ReusabilityLink.csproj`**

Mismas referencias que `RLProbe.csproj` más un `ProjectReference` a Core. Target `AfterBuild`:

1. Copia `ReusabilityLink.dll` y `ReusabilityLink.Core.dll` a `$(SolutionDir)GameData\ReusabilityLink\Plugins\`.
2. Si `'$(DeployToKSP)' != 'false'`, copia toda la carpeta `GameData\ReusabilityLink` (sin `PluginData`) a `$(KSPDir)\GameData\ReusabilityLink\`.

- [ ] **Step 2: Implementar `Log`, `SettingsLoader`, `LinkVesselModule` y `LinkAddon`**

`LinkAddon.Start` carga la configuración, escribe cada aviso con `Log.Warn` y escribe `Log.Info("v1 cargado: MaxPhysicsDistance=… SwitchKey=…")`.

- [ ] **Step 3: Compilar**

Run: `dotnet build ReusabilityLink.sln -c Release`
Expected: build correcto. Existen `GameData/ReusabilityLink/Plugins/ReusabilityLink.dll` y `ReusabilityLink.Core.dll`, y la carpeta está copiada en el `GameData` de KSP.

- [ ] **Step 4: Verificar en el juego (usuario)**

Abrir KSP, entrar en vuelo y buscar en `KSP.log` la línea `[ReusabilityLink] v1 cargado`. No debe haber excepciones con `ReusabilityLink`. Poner a mano `MaxPhysicsDistance = 1,5` en `Settings.cfg` y comprobar que aparece el aviso y se usa el valor por defecto. Después restaurar el valor.

- [ ] **Step 5: Commit**

```bash
git add src/ReusabilityLink ReusabilityLink.sln
git commit -m "feat: add KSP plugin project with settings loading and vessel module"
```

---

### Task 7: `TrackingService`

**Files:**
- Create: `src/ReusabilityLink/TrackingService.cs`, `src/ReusabilityLink/KspAdapters.cs`
- Modify: `src/ReusabilityLink/LinkAddon.cs` (crearlo y liberarlo)

**Interfaces:**
- Consumes: `AutoTrackFilter.Evaluate`, `LandingSettle`, `LinkVesselModule`, `LinkSettings`.
- Produces:
  - `public sealed class TrackingService : IDisposable`:
    - `IReadOnlyList<Vessel> Tracked` (solo naves vivas y con `tracked=true`)
    - `Vessel OtherTracked` (la primera seguida distinta de la activa, o `null`)
    - `bool Track(Vessel v, bool manual)`, `void Untrack(Vessel v, string reason)`
    - `void FixedTick(double dt)`: fin de seguimiento por aterrizaje con `LandingSettle`, una instancia por nave
    - `event Action<Vessel> TrackingStarted`, `event Action<Vessel> TrackingEnded`
  - `internal static class KspAdapters`:
    - `static FlightSituation ToSituation(Vessel.Situations s)`
    - `static bool HasCommand(Vessel v)` (alguna pieza con `ModuleCommand`)
    - `static bool HasEngine(Vessel v)` (alguna con `ModuleEngines`)
    - `static bool EngineThrusting(Vessel v)` (algún `ModuleEngines` con `finalThrust > 0`)
    - `static LinkVesselModule Module(Vessel v)` (`v.FindVesselModuleImplementing<LinkVesselModule>()`)

- [ ] **Step 1: Implementar `KspAdapters` y `TrackingService`**

- Se suscribe a `GameEvents.onPartDeCoupleNewVesselComplete(Vessel original, Vessel nueva)`. La candidata es la de las dos que no es `FlightGlobals.ActiveVessel`, y `FromActiveVessel` es true si la otra es la activa. Se construye `SeparationInfo` con los datos de la activa (`orbit.PeA`, `mainBody.atmosphere ? mainBody.atmosphereDepth : 0`) y se llama a `Evaluate`.
  - Si el resultado es `Track`: se marcan la activa y la candidata, y se avisa con `ScreenMessages.PostScreenMessage("Reusability Link: siguiendo " + nombre, 4f)`.
  - Si es `LimitReached`: aviso "límite de naves seguidas alcanzado".
  - En cualquier otro caso: solo `Log.Info` con el motivo.
- Al seguir una nave: `SaveOriginalRanges()` y `tracked=true`. Al dejar de seguirla: `RestoreOriginalRanges()` y `tracked=false`.
- Fin de seguimiento:
  - `onVesselWillDestroy`, `onVesselRecovered` y `onVesselPartCountChanged` cuando la nave deja de existir o se ha fusionado (`v.state == Vessel.State.DEAD` o ya no está en `FlightGlobals.Vessels`): `Untrack(v, "...")` sin excepción.
  - `LandingSettle` devuelve true: `Untrack(v, "aterrizado")` y aviso "Booster aterrizado — seguimiento terminado".
  - Si después solo queda la nave activa como seguida, también deja de seguirse (no hay pareja).
- Al cargar la escena (`Start`), se reconstruye `Tracked` a partir de `FlightGlobals.Vessels` con `tracked=true`.

- [ ] **Step 2: Compilar**

Run: `dotnet build ReusabilityLink.sln -c Release`
Expected: build correcto.

- [ ] **Step 3: Prueba en el juego (usuario)** — cubre Review Focus 2 y 3

1. Cohete de prueba: etapa superior con sonda + booster con sonda y motores + cofia. Al separar el booster aparece "siguiendo <booster>". Al soltar la cofia, nada.
2. Desplegar un satélite con sonda y motor en órbita estable: no se sigue.
3. Guardado rápido (F5) con dos naves seguidas, carga rápida (F9): `KSP.log` muestra las dos seguidas otra vez. Volver al lanzamiento: ninguna seguida.
4. Estrellar la nave activa con otra seguida, y acoplar el booster seguido a otra nave: sin excepciones `[ReusabilityLink]` en `KSP.log`, y las naves que ya no existen salen de `Tracked`.

- [ ] **Step 4: Commit**

```bash
git add src/ReusabilityLink
git commit -m "feat: add tracking service with auto-track on separation"
```

---

### Task 8: `RangeController`, `KillGuard` y `WarpGuard`

**Files:**
- Create: `src/ReusabilityLink/RangeController.cs`, `src/ReusabilityLink/KillGuard.cs`, `src/ReusabilityLink/WarpGuard.cs`, `src/ReusabilityLink/AtmosphereSampler.cs`
- Modify: `src/ReusabilityLink/LinkAddon.cs` (crear `Harmony("reusabilitylink")`, `PatchAll` en `Awake`, `UnpatchAll` en `OnDestroy`; llamar a los servicios en `FixedUpdate`)

**Interfaces:**
- Consumes: `TrackingService.Tracked`, `PhysicsPolicy.Step`, `RangePlanner.ForMode`, `TelemetryMath.DynamicPressure`, `KspAdapters.EngineThrusting`.
- Produces:
  - `internal static class AtmosphereSampler`:
    - `static double StaticPressureKPa(CelestialBody b, double alt)` (0 si `!b.atmosphere`)
    - `static double Density(CelestialBody b, double alt)` (`b.GetDensity(b.GetPressure(alt), b.GetTemperature(alt))`)
    - `static double PredictedMaxQ(Vessel v, double seconds)`: muestras en t = 5, 10 y 15 s con `v.orbit.getPositionAtUT(ut+t)` y `getOrbitalVelocityAtUT(ut+t).xzy - b.getRFrmVel(pos)`, devuelve el máximo de `q`
  - `public sealed class RangeController`: `LinkMode ModeOf(Vessel v)`, `void FixedTick(double dt)`. Mantiene un `PolicyState` por `Vessel.id`.
  - `public static class KillGuard`: `static bool IsProtected(Vessel v)` (seguida); clase `[HarmonyPatch(typeof(Vessel), nameof(Vessel.CheckKill))]` con `Prefix(Vessel __instance, out bool __state)` y `Finalizer(bool __state)`, igual que `ProbeKillGuard` pero comprobando `IsProtected`.
  - `public sealed class WarpGuard`: `void Tick()`. Si alguna seguida está en `Physics` y `TimeWarp.WarpMode == TimeWarp.Modes.HIGH` con `TimeWarp.CurrentRateIndex > 0`, llama a `TimeWarp.SetRate(0, true)` y muestra el aviso "Warp limitado: el booster necesita física" (como mucho una vez cada 5 s).

- [ ] **Step 1: Implementar `AtmosphereSampler` y `RangeController.FixedTick`**

Para cada nave seguida:

- Si es la activa, solo se le aplican `load`/`unload` de seguida (`RangePlanner.ForMode(LinkMode.Physics)` también vale) y se pone su estado a `Physics`.
- Si no es la activa:
  1. Construir `PolicyInput`: `q` actual de `Density(alt)` y `srfSpeed`, `PredictedMaxQ(v, LookaheadSeconds)`, presión estática, `EngineThrusting`, `LandedOrSplashed`, `srfSpeed`, y la distancia `Vector3d.Distance(v.CoMD, active.CoMD)`.
  2. Llamar a `Step`.
  3. Escribir `RangePlanner.ForMode(modo)` en las 7 situaciones de `v.vesselRanges` (`prelaunch, landed, splashed, flying, subOrbital, orbit, escaping`).
  4. Al cambiar de modo, escribir `Log.Info($"{nombre}: {anterior} → {nuevo} (q={q:F0} Pa, d={km:F0} km)")`.

- [ ] **Step 2: Implementar `KillGuard` y `WarpGuard`**

- [ ] **Step 3: Compilar**

Run: `dotnet build ReusabilityLink.sln -c Release`
Expected: build correcto.

- [ ] **Step 4: Prueba en el juego (usuario)** — perfil típico: barcaza a distancia moderada

1. Tras separar, con el booster en arco balístico por encima de la atmósfera, `KSP.log` muestra `Physics → Rails`.
2. Al bajar a aire denso: `Rails → Physics` **antes** de que `q` llegue a 100 Pa, gracias a la previsión.
3. Llevar la etapa superior a más de `MaxPhysicsDistance` del booster mientras este cae en atmósfera: `→ OutOfRange`, el booster no se destruye (no aparece "was on-rails at" en el log) y al acercarse vuelve a `Physics`.
4. Intentar warp normal con el booster en `Physics`: se limita y aparece el aviso.
5. Descenso con paracaídas del booster inactivo: se queda en `Physics` (presión ≥ 1 kPa).

- [ ] **Step 5: Commit**

```bash
git add src/ReusabilityLink
git commit -m "feat: apply hybrid physics policy via vessel ranges, kill and warp guards"
```

---

### Task 9: `SwitchController`

**Files:**
- Create: `src/ReusabilityLink/SwitchController.cs`
- Modify: `src/ReusabilityLink/LinkAddon.cs` (llamarlo en `Update`)

**Interfaces:**
- Consumes: `TrackingService.OtherTracked`, `LinkSettings.SwitchKey`.
- Produces: `public sealed class SwitchController { void Update(); bool SwitchToOther(); }`. `SwitchToOther` llama a `FlightGlobals.ForceSetActiveVessel(OtherTracked)` y devuelve false si es `null`.

- [ ] **Step 1: Implementar**

- La tecla se traduce a `KeyCode` con `Enum.Parse(typeof(KeyCode), Key, true)`. Si falla, `Log.Warn` y la tecla queda desactivada.
- Los modificadores se comprueban con `Input.GetKey` (`LeftAlt`/`RightAlt`, `LeftControl`/`RightControl`, `LeftShift`/`RightShift`), y la tecla con `Input.GetKeyDown`.
- No se cambia de nave si `GUIUtility.keyboardControl != 0` (hay un campo de texto IMGUI con foco), si `InputLockManager.IsLocked(ControlTypes.All)` está activo, o si el juego está en pausa (`FlightDriver.Pause`).

- [ ] **Step 2: Compilar**

Run: `dotnet build ReusabilityLink.sln -c Release`
Expected: build correcto.

- [ ] **Step 3: Prueba en el juego (usuario)** — cubre Review Focus 1

1. Con el booster sobre raíles a más de 100 km, `Alt+B` cambia **sin pantalla de carga**. Pulsar otra vez vuelve a la etapa superior.
2. Abrir el renombrado de nave o el buscador de piezas, escribir una `B` con `Alt`: no cambia de nave.
3. Con el juego en pausa (Esc), `Alt+B` no hace nada.

- [ ] **Step 4: Commit**

```bash
git add src/ReusabilityLink
git commit -m "feat: add instant vessel switch hotkey"
```

---

### Task 10: Ventanita: `TelemetrySampler`, `MonitorWindow`, `MiniNavball`

**Files:**
- Create: `src/ReusabilityLink/TelemetrySampler.cs`, `src/ReusabilityLink/MonitorWindow.cs`, `src/ReusabilityLink/MiniNavball.cs`
- Modify: `src/ReusabilityLink/LinkAddon.cs` (`OnGUI`, botón de AppLauncher)

**Interfaces:**
- Consumes: `TelemetryMath.*`, `Vec3`, `RangeController.ModeOf`, `TrackingService.OtherTracked`, `SwitchController.SwitchToOther`, `SettingsLoader.SaveWindow` / `TryLoadWindow`, `AtmosphereSampler`.
- Produces:
  - `public struct TelemetrySnapshot { string Name; double SurfaceSpeed, AltitudeASL, HeightAboveTerrain, VerticalSpeed, DistanceM; double? SecondsToImpact; ImpactLevel Impact; Attitude Attitude; NavballMarker Prograde, Retrograde; LinkMode Mode; bool LandedOrSplashed, InAtmosphereAndFlying; }`
  - `public static TelemetrySnapshot TelemetrySampler.Sample(Vessel v, Vessel active, LinkMode mode, LinkSettings s)`
  - `public sealed class MonitorWindow { void Draw(); bool Visible; }`, `public static class MiniNavball { static void Draw(Rect area, TelemetrySnapshot t); }`

- [ ] **Step 1: Implementar `TelemetrySampler.Sample`**

- Marco ENU calculado por nosotros, porque no podemos fiarnos de que KSP actualice estos vectores en una nave empaquetada:
  - `up = (v.CoMD - b.position).normalized`
  - `north = Vector3d.Exclude(up, b.position + b.transform.up * b.Radius - v.CoMD).normalized`
  - `east = b.getRFrmVel(v.CoMD).normalized`
- Para pasar un vector de Unity a ENU: `new Vec3(dot(w,east), dot(w,north), dot(w,up))`.
- `nose = v.ReferenceTransform.up`, `top = -v.ReferenceTransform.forward`.
- Prograde de superficie = `v.srf_velocity`; retrógrado = `-v.srf_velocity`.
- Altura sobre el terreno: `v.altitude - max(0, b.TerrainAltitude(v.latitude, v.longitude))` si `b.ocean`; sin océano, sin `max`.
- `InAtmosphereAndFlying` = `b.atmosphere && v.altitude < b.atmosphereDepth && !v.LandedOrSplashed`.

- [ ] **Step 2: Implementar `MiniNavball.Draw`**

- Círculo de 120 px: cielo azul y tierra marrón separados por una línea de horizonte. La línea está desplazada `-sin(cabeceo)·R` desde el centro y girada `-alabeo` con `GUIUtility.RotateAroundPivot`.
- Cruz fija en el centro (el morro).
- Marcador de prograde (círculo verde) y de retrógrado (aspa amarilla) en `centro + (X, -Y)·R`, solo si `Visible`.
- Rumbo en texto debajo (`RUMBO 090°`).
- Texturas de 1×1 px creadas en código; nada de assets.

- [ ] **Step 3: Implementar `MonitorWindow`**

- `GUILayout.Window` de unos 220×300 px, que se puede arrastrar y guarda su posición con `SaveWindow` al soltar.
- Solo es visible si `OtherTracked != null` y el usuario no la ha ocultado.
- Filas: nombre; `VEL 1234 m/s`; `ALT 12.3 km` / `SUELO 11.9 km`; `V.VERT -123 m/s`; navball; `ESTADO: RAÍLES | FÍSICA | ATERRIZADO | FUERA DE RANGO`; `DIST 85.2 km`.
- Avisos con color: azul "Entrando en aire denso" durante 5 s tras pasar a `Physics`; amarillo "Impacto estimado en < 60 s"; rojo "Impacto estimado en < 20 s"; rojo "Fuera de rango de física — cambia a esta nave"; y si `InAtmosphereAndFlying`, "No salgas del vuelo: el booster puede perderse".
- Botón "Cambiar (Alt+B)" que llama a `SwitchToOther`. El texto del botón usa `SwitchKey.ToString()`.
- Botón del AppLauncher (`ApplicationLauncher.Instance.AddModApplication`, solo en `FLIGHT`) con un icono de 38×38 px generado en código, que muestra u oculta la ventana. Dentro de la ventana, un botón "Seguir/Dejar de seguir esta nave" para la nave activa: seguimiento manual sin filtro.

- [ ] **Step 4: Compilar**

Run: `dotnet build ReusabilityLink.sln -c Release`
Expected: build correcto.

- [ ] **Step 5: Prueba en el juego (usuario)**

1. Con el booster inactivo, comparar los valores de la ventanita con los de la navball de KSP al cambiar a él (velocidad ±1 %, altitud ±1 %, velocidad vertical con el signo correcto).
2. Navball: con el booster en ascenso vertical, el horizonte queda abajo y el prograde está centrado. Girar el booster a la derecha: el horizonte se inclina hacia la izquierda. **Si el alabeo o el cabeceo salen invertidos, se corrige el signo de `top` en `TelemetrySampler`, no en Core.**
3. Aviso de impacto: en caída libre sin frenar aparecen el amarillo y luego el rojo.
4. Mover la ventana, salir y volver a entrar en vuelo: conserva la posición.

- [ ] **Step 6: Commit**

```bash
git add src/ReusabilityLink
git commit -m "feat: add monitor window with telemetry and mini navball"
```

---

### Task 11: Prueba de aceptación y README

**Files:**
- Create: `README.md` (español: qué hace, instalación, uso, configuración, limitaciones conocidas según §5.1)
- Modify: `docs/superpowers/specs/2026-10-13-reusability-link-design.md` (anotar las limitaciones encontradas)

- [ ] **Step 1: Ejecutar todos los tests**

Run: `dotnet test tests/ReusabilityLink.Core.Tests`
Expected: todos PASS.

- [ ] **Step 2: Lista de comprobación del usuario (spec §6, puntos 1–7)**

Un vuelo completo: lanzamiento, separación, etapa superior a órbita, cambio al booster, reentrada y aterrizaje en el mar. Incluye guardar y cargar a mitad del vuelo, y un satélite desplegado que no debe seguirse. Se anota el resultado de cada punto.

- [ ] **Step 3: Escribir `README.md`** con los resultados y las limitaciones (p. ej. suelo lejano, si la Task 1 lo confirmó).

- [ ] **Step 4: Commit**

```bash
git add README.md docs
git commit -m "docs: add README and record acceptance results"
```
