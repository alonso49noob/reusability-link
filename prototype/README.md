# RLProbe — prototipo de medición (fase 0)

Herramienta **desechable** para medir en tu KSP los riesgos que quedan antes de terminar Reusability Link. No forma parte del mod. Al acabar, borra `GameData\RLProbe`.

## Preparación

1. Compila (con KSP cerrado): `dotnet build prototype/RLProbe/RLProbe.csproj -c Release`. Se instala solo en `GameData\RLProbe\Plugins\RLProbe.dll`.
2. Construye un cohete de prueba: **etapa superior** con sonda + **booster** con sonda, motores y algo de combustible sobrante, más aletas o frenos aerodinámicos si sueles usarlos.
3. En vuelo, `Alt+P` muestra/oculta la ventana de RLProbe. Lista las naves cargadas que no son la activa, con distancia, situación, `packed` (sobre raíles), presión estática, presión dinámica `q` y *jitter* (cuánto se deforma la nave respecto a su forma inicial; valores de milímetros son normales, de decímetros o más indican problema).
4. Botones de cada nave:
   - **Mantener cargada:** evita que KSP la descargue (hay que pulsarlo **antes de que se aleje 22 km**, justo tras separar). También activa una línea por segundo en `KSP.log`.
   - **FÍSICA / RAÍLES:** fuerza física completa o sobre raíles.
   - **KillGuard:** activa la protección contra el borrado de KSP.
   - **Cambiar a esta:** cambio de nave forzado (como hará el mod).

`KSP.log` está en la carpeta de KSP. Las líneas del prototipo empiezan por `[RLProbe]`.

## Pruebas

Anota el resultado de cada una (copiar las líneas `[RLProbe]` del log también vale).

### 1. Distancia de física

Separa el booster, pulsa **Mantener cargada** y **FÍSICA**, activa **KillGuard**, y sigue volando la etapa superior hacia órbita mientras el booster cae.
Anota el *jitter* y si el booster tiembla, se rompe o explota a unos **50, 100, 200, 300, 500 y 1000 km** de distancia (lo que llegues a alcanzar).

### 2. Raíles en atmósfera

Con el booster por debajo de ~25 km de altitud (presión > 1 kPa en la ventana) y a más de 3 km de ti:
1. Sin KillGuard, pulsa **RAÍLES**. Lo esperado es que KSP lo destruya y en el log aparezca `was on-rails at … kPa pressure and was destroyed`.
2. Repite con otro lanzamiento **con KillGuard** activado: el booster debe sobrevivir sobre raíles.

### 3. Cambio instantáneo

Con el booster en **RAÍLES** (y KillGuard) a más de 100 km, pulsa **Cambiar a esta**. ¿Hay pantalla de carga? ¿Cuánto tarda? (El log muestra `switch to … took=…ms`.) Vuelve a la etapa superior con el mismo botón.

### 4. Suelo lejano

Deja el booster en **FÍSICA** y que toque **tierra** estando tú a más de 50 km. ¿Aterriza/choca normalmente o atraviesa el suelo? Repite cayendo al **agua**.

### 5. SAS

Antes de cambiar a la etapa superior, activa el SAS del booster (en modo estabilidad o retrógrado). Ya en la etapa superior, observa en la ventana (o volviendo a él) si mantuvo la orientación mientras estaba inactivo.

### 6. Tecla Alt+B

En vuelo pulsa `Alt+B`. RLProbe muestra "Alt+B detectado". ¿Reacciona además KSP o algún otro mod (se abre algo, cambia algo)?

## Al terminar

Pásame tus notas y `KSP.log`. Después borra la carpeta `GameData\RLProbe`.
