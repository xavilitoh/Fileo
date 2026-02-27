# Fileo

Fileo es una herramienta CLI para organizar archivos en carpetas como `Images`, `Documents`, `Archives` y `Apps`.

## Instalación

- Instalación global (desde NuGet.org):

```bash
dotnet tool install -g fileo-cli
```

- Instalación de una versión concreta:

```bash
dotnet tool install -g fileo-cli --version 1.2.3
```

- Instalación local al repositorio (dev):

```bash
dotnet new tool-manifest # si aún no existe
dotnet tool install fileo-cli
```

Luego ejecútalo con `dotnet tool run fileo` desde la carpeta del repositorio.

## Actualizar / Desinstalar

- Actualizar a la última versión (global):

```bash
dotnet tool update -g fileo-cli
```

- Actualizar a una versión concreta:

```bash
dotnet tool update -g fileo-cli --version 1.2.4
```

- Desinstalar (global):

```bash
dotnet tool uninstall -g fileo-cli
```

## Uso básico

Comandos principales:

- Ordenar la carpeta `Downloads` del sistema:

```bash
fileo -d
```

- Ordenar la carpeta `Documents` del sistema:

```bash
fileo -m
```

- Ordenar una ruta específica:

```bash
fileo -p /ruta/a/carpeta
```

- Ejecución en modo simulación (dry-run):

```bash
fileo -p /ruta/a/carpeta --dry-run
```

También puedes usar `fileo --help` o `fileo -h` para ver las opciones disponibles.

### Ejemplos

- Ejecutar sobre `~/Downloads` y ver qué ocurriría sin mover nada:

```bash
fileo -p ~/Downloads --dry-run
```

- Ejecutar de forma interactiva (sin argumentos):

```bash
fileo
```

Fileo mostrará un menú para elegir la carpeta a ordenar.

## Comportamiento y opciones importantes

- `--dry-run` o `-n`: no mueve archivos; imprime qué movimientos haría.
- Categorías y extensiones: Fileo clasifica por grupos (Images, Documents, Archives, Apps). Las extensiones soportadas están embebidas en la herramienta.
- `Apps` puede incluir directorios (p. ej. `*.app`) y Fileo detecta si un archivo está dentro de un paquete de aplicación para no moverlo por error.

## Ejecutar tests localmente (para desarrolladores)

Si trabajas en el código fuente y quieres ejecutar la suite de pruebas:

```bash
dotnet test Fileo.Core.Tests/Fileo.Core.Tests.csproj
```

La solución también contiene el proyecto de tests `Fileo.Core.Tests`.

## Actualizar la instalación cuando se publica una nueva versión

1. Publicar nueva versión en NuGet (mantenedor).
2. Usuarios globales ejecutan:

```bash
dotnet tool update -g fileo-cli
```

Si tienes una instalación local (tool manifest), ejecuta:

```bash
dotnet tool update fileo-cli
```

## Solución de problemas comunes

- Error: "Directorio no existe": verifica la ruta y permisos. Para rutas con `~` Fileo expande automáticamente al HOME.
- Permisos: si el CLI necesita mover archivos en carpetas del sistema, asegúrate de tener permisos de lectura/escritura.
- PATH: después de una instalación global, asegúrate de que el directorio de herramientas de .NET esté en tu `PATH` (dotnet tools global path). En macOS suele ser `~/.dotnet/tools`.

## Informar problemas y contribuir

- Para reportar un bug o pedir una mejora, crea un issue en el repositorio.
- Para contribuir con código: clona el repositorio, agrega tests a `Fileo.Core.Tests`, y abre un PR.

## Notas finales

- El paquete NuGet se publica como `fileo-cli` y el ejecutable se invoca como `fileo`.
- Usa `--dry-run` antes de ejecutar por primera vez para revisar qué archivos se moverían.
