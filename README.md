# AsciiForge 5.1.1 — C# GPU Edition

**English** | [Español](#español)

AsciiForge is a GPU-accelerated ASCII animation editor and procedural visual playground for Windows. It renders the live preview directly with OpenGL instead of rebuilding a text widget every frame, while still exporting real selectable/copyable ASCII text.

## Highlights

- C# / .NET 10 + WinForms
- Native OpenGL 3.3 through WGL / `opengl32.dll`
- GPU-resident live preview; GPU readback only when text is copied or exported
- 42 effect families and 300+ presets, including 3D shapes, procedural terrain, SDF scenes, fire, fireworks, waves, cellular automata, flow fields, lightning and black holes
- 22 color palettes and 27 character ramps
- Spanish and English interface with plain-language tooltips
- Effect-specific controls plus live sliders and numeric fields
- Export to PowerShell, selectable HTML, JSON, standalone C#, ANSI and TXT
- Static PowerShell import: recognized frame data is parsed without executing arbitrary scripts
- No third-party NuGet packages

## Build

Install **.NET desktop development** and the .NET 10 SDK from Visual Studio Installer, then open `AsciiForge.sln` and build `Release`, or run:

```powershell
.\Build-Release.ps1
```

The self-contained Windows x64 executable is written to:

```text
publish\win-x64\AsciiForge.exe
```

`Build-Release.ps1` also creates `dist\AsciiForge-5.1.1-win-x64.zip`.

## Requirements

- Windows x64
- OpenGL 3.3 capable GPU/driver
- No separate .NET installation is required for the self-contained Release build

## License

MIT.

---

# Español

AsciiForge es un editor de animaciones ASCII y laboratorio visual procedural acelerado por GPU para Windows. La vista previa se renderiza directamente con OpenGL en vez de reconstruir un widget de texto en cada frame, pero las exportaciones siguen siendo texto ASCII real, seleccionable y copiable.

## Características

- C# / .NET 10 + WinForms
- OpenGL 3.3 nativo mediante WGL / `opengl32.dll`
- Preview residente en GPU; solo hay readback cuando copias o exportas texto
- 42 familias de efectos y más de 300 presets: figuras 3D, terreno procedural, SDF, fuego, fireworks, ondas, autómatas celulares, flow fields, rayos, black holes y más
- 22 paletas y 27 rampas de caracteres
- Interfaz en español e inglés con tooltips sencillos
- Controles específicos por efecto, sliders y campos numéricos editables en vivo
- Exportación a PowerShell, HTML seleccionable, JSON, C# standalone, ANSI y TXT
- Importación estática de PowerShell sin ejecutar scripts arbitrarios
- Sin paquetes NuGet de terceros

## Compilar

Instala **.NET desktop development** y .NET 10 SDK desde Visual Studio Installer. Abre `AsciiForge.sln` y compila en `Release`, o ejecuta:

```powershell
.\Build-Release.ps1
```

El EXE self-contained x64 queda en:

```text
publish\win-x64\AsciiForge.exe
```

## Licencia

MIT.
