# Reusability Link

Mod para **Kerbal Space Program 1.12.5** que permite recuperar etapas reutilizables pilotándolas a mano sin perderlas.

Al separar un booster durante el ascenso, Reusability Link:

- **lo mantiene cargado** aunque se aleje: KSP no lo descarga ni lo borra por estar en la atmósfera;
- le da **física completa solo cuando la necesita** (aire denso, motores encendidos, aterrizando) y lo lleva **sobre raíles** en el vacío, sin coste de rendimiento;
- te deja **cambiar entre la etapa superior y el booster al instante**, sin pantalla de carga (`Alt+B`);
- muestra la otra nave en una **ventanita** con velocidad, altitud, altura sobre el suelo, velocidad vertical, una **navball simplificada**, su estado y avisos ("entrando en aire denso", "impacto en < 20 s", "fuera de rango de física").

## Requisitos

- KSP 1.12.5
- [Harmony 2](https://github.com/KSPModdingLibs/HarmonyKSP) (`GameData/000_Harmony`)

## Instalación

Copia la carpeta `GameData/ReusabilityLink` dentro de la carpeta `GameData` de KSP.

Para compilar desde el código (con KSP cerrado):

```bash
dotnet build ReusabilityLink.sln -c Release
```

La compilación copia el mod a la instalación de KSP indicada en `Directory.Build.props` (`KSPDir`, o la variable de entorno `KSPDIR`). Para compilar sin copiar: `-p:DeployToKSP=false`.

## Uso

1. Construye el booster con un **núcleo de control** (sonda o cabina) y **motores**.
2. Al separarlo durante el ascenso aparece "Reusability Link: siguiendo …". Cofias, adaptadores y satélites desplegados en órbita estable **no** se siguen.
3. Sigue pilotando la etapa superior. La ventanita muestra el booster. Cuando avise de aire denso o del encendido de reentrada, pulsa `Alt+B` o el botón **Cambiar**.
4. Aterriza el booster. Cuando lleva 10 s quieto en tierra o agua, deja de seguirse y queda como una nave normal.

El botón de la barra de aplicaciones muestra u oculta la ventana. Abriéndola desde ahí también puedes **seguir o dejar de seguir la nave activa a mano**, sin filtro. Como máximo se siguen 2 naves a la vez (configurable).

No salgas de la escena de vuelo mientras el booster está en la atmósfera: fuera del vuelo KSP descarga las naves con sus reglas normales.

## Configuración

`GameData/ReusabilityLink/Settings.cfg` (decimales con **punto**). Lo principal:

| Clave | Por defecto | Qué hace |
|---|---|---|
| `SwitchKey` | `Alt+B` | Tecla para cambiar de nave |
| `MaxPhysicsDistance` | `300000` | Distancia (m) máxima para mantener la física de la nave inactiva |
| `PhysicsOnDynamicPressure` | `100` | Presión dinámica (Pa) que activa la física |
| `RailsMaxStaticPressureKPa` | `1.0` | Por encima de esta presión la nave inactiva nunca va sobre raíles |
| `AutoTrack` | `True` | Seguimiento automático al separar |
| `MaxTrackedVessels` | `2` | Naves seguidas a la vez |

Los valores inválidos se ignoran y se avisa en `KSP.log` (líneas `[ReusabilityLink]`).

## Limitaciones conocidas

Pendientes de confirmar con la sesión de medición (`prototype/README.md`):

- **Distancia de física:** más allá de `MaxPhysicsDistance` la nave inactiva queda sobre raíles sin aerodinámica, y la ventanita pide que cambies a ella.
- **Terreno lejano:** una nave inactiva con física que toca suelo lejos de la nave activa podría atravesarlo, porque KSP solo genera el terreno cerca de la cámara. Los avisos de impacto existen para que cambies a ella antes.
- El warp normal se limita a warp físico mientras el booster necesita física.

## Desarrollo

- `src/ReusabilityLink.Core`: lógica pura (política física/raíles, regla de seguimiento, telemetría, configuración), con tests en `tests/ReusabilityLink.Core.Tests` (`dotnet test tests/ReusabilityLink.Core.Tests`).
- `src/ReusabilityLink`: el plugin de KSP.
- `prototype/RLProbe`: herramienta desechable de medición (fase 0).
- Diseño: `docs/superpowers/specs/2026-10-13-reusability-link-design.md`.
