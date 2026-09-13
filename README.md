<img width="1917" height="1025" alt="image" src="https://github.com/user-attachments/assets/febc7df3-bc87-4c84-85c7-c68710979555" />

# AsciiForge 5.2.0 — C# GPU Edition

**English** | [Español](#español)

AsciiForge is a GPU-accelerated ASCII animation editor and procedural visual playground for Windows. It renders the live preview directly with OpenGL instead of rebuilding a text widget every frame, while still exporting real selectable and copyable ASCII text.

## Highlights

- C# / .NET 10 + WinForms
- Native OpenGL 3.3 through WGL / `opengl32.dll`
- GPU-resident live preview; GPU readback only when actual ASCII data is needed
- 42 effect families and 316 presets
- 3D shapes, procedural terrain, SDF scenes, fire, fireworks, waves, cellular automata, flow fields, lightning, black holes, fractals and more
- Interactive camera controls for supported 3D effects
- Terrain generation and deformation controls for 3D grids
- 22 color palettes and 27 character ramps
- Spanish and English interface
- Plain-language tooltips explaining what controls actually do
- Effect-specific controls, live sliders and editable numeric fields
- Visible Export button plus `Ctrl+E` shortcut
- Export to PowerShell, selectable HTML, JSON, standalone C#, ANSI and TXT
- Optional AsciiForge attribution in supported exports
- Static PowerShell import: recognized frame data is parsed without executing arbitrary scripts
- Self-contained Windows x64 builds
- No third-party NuGet packages

## AI-assisted development

AsciiForge was developed with the help of AI tools across different parts of the process, including code generation and review, debugging, effect design, and documentation.

This project is not simply AI-generated code published without review: its features, effects, behavior, and design decisions have been manually tested, adjusted, and directed throughout development.

## Generated content and attribution

Animations and other visual content generated with AsciiForge may be used freely, including in personal and commercial projects.

Attribution is not required, but it is explicitly requested and greatly appreciated when reasonably possible.

Suggested credit:

```text
Created with AsciiForge — https://github.com/saturnus25/AsciiForge
```

See [`OUTPUT-NOTICE.md`](OUTPUT-NOTICE.md) for more information.

## Build

Install **.NET desktop development** and the **.NET 10 SDK** from Visual Studio Installer.

Then open `AsciiForge.sln` and build in `Release`, or run:

```powershell
.\Build-Release.ps1
```

You can also use:

```text
Build-Release.cmd
```

The self-contained Windows x64 executable is written to:

```text
publish\win-x64\AsciiForge.exe
```

The build script also creates:

```text
dist\AsciiForge-5.2.0-win-x64.zip
```

## Requirements

- Windows x64
- OpenGL 3.3 capable GPU and driver
- No separate .NET installation is required for the self-contained Release build

## License

AsciiForge source code is licensed under the [MIT License](LICENSE).

---

# Español

AsciiForge es un editor de animaciones ASCII y laboratorio visual procedural acelerado por GPU para Windows. La vista previa se renderiza directamente con OpenGL en vez de reconstruir un widget de texto en cada frame, pero las exportaciones siguen siendo texto ASCII real, seleccionable y copiable.

## Características

- C# / .NET 10 + WinForms
- OpenGL 3.3 nativo mediante WGL / `opengl32.dll`
- Preview residente en GPU; solo hay readback cuando se necesitan datos ASCII reales
- 42 familias de efectos y 316 presets
- Figuras 3D, terreno procedural, escenas SDF, fuego, fireworks, ondas, autómatas celulares, flow fields, rayos, black holes, fractales y más
- Controles de cámara interactivos en efectos 3D compatibles
- Generación y deformación de terreno para rejillas 3D
- 22 paletas de color y 27 rampas de caracteres
- Interfaz en español e inglés
- Tooltips sencillos que explican directamente qué hace cada control
- Controles específicos por efecto, sliders y campos numéricos editables en vivo
- Botón visible de Exportar además del atajo `Ctrl+E`
- Exportación a PowerShell, HTML seleccionable, JSON, C# standalone, ANSI y TXT
- Crédito opcional de AsciiForge en las exportaciones compatibles
- Importación estática de PowerShell sin ejecutar scripts arbitrarios
- Builds self-contained para Windows x64
- Sin paquetes NuGet de terceros

## Desarrollo asistido por IA

AsciiForge ha sido desarrollado con apoyo de herramientas de inteligencia artificial en distintas partes del proceso, incluyendo generación y revisión de código, depuración, diseño de efectos y documentación.

El proyecto no es simplemente código generado y publicado sin revisar: las funciones, efectos, comportamiento y decisiones de diseño han sido probados, ajustados y dirigidos manualmente durante el desarrollo.

## Contenido generado y atribución

Las animaciones y demás contenido visual generado con AsciiForge pueden utilizarse libremente, incluyendo proyectos personales y comerciales.

La atribución no es obligatoria, pero se solicita expresamente y se agradece siempre que sea razonablemente posible.

Crédito sugerido:

```text
Creado con AsciiForge — https://github.com/saturnus25/AsciiForge
```

Consulta [`OUTPUT-NOTICE.md`](OUTPUT-NOTICE.md) para más información.

## Compilar

Instala **.NET desktop development** y el **.NET 10 SDK** desde Visual Studio Installer.

Después abre `AsciiForge.sln` y compila en `Release`, o ejecuta:

```powershell
.\Build-Release.ps1
```

También puedes utilizar:

```text
Build-Release.cmd
```

El ejecutable self-contained para Windows x64 queda en:

```text
publish\win-x64\AsciiForge.exe
```

El script de compilación también genera:

```text
dist\AsciiForge-5.2.0-win-x64.zip
```

## Requisitos

- Windows x64
- GPU y driver compatibles con OpenGL 3.3
- La build self-contained no requiere instalar .NET por separado

## Licencia

El código fuente de AsciiForge está publicado bajo la [licencia MIT](LICENSE).
