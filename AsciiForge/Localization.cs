namespace AsciiForge;

internal static class Localization
{
    public static bool English { get; set; }

    private static readonly Dictionary<string, (string Es, string En)> Ui = new(StringComparer.OrdinalIgnoreCase)
    {
        ["group.language"]=("Idioma / Language","Language / Idioma"),
        ["group.effect"]=("Efecto y preset","Effect and preset"),
        ["group.output"]=("Salida","Output"),
        ["group.general"]=("Parámetros generales / oscilaciones","General parameters / oscillations"),
        ["group.specific"]=("Ajustes específicos del efecto","Effect-specific controls"),
        ["group.charset"]=("Caracteres ASCII / Unicode","ASCII / Unicode characters"),
        ["group.color"]=("Color / gradiente","Color / gradient"),
        ["group.shape"]=("Figura","Shape"),
        ["button.restart"]=("↻ Reiniciar","↻ Restart"), ["button.pause"]=("Pausar","Pause"), ["button.resume"]=("Continuar","Resume"),
        ["button.benchmark"]=("Benchmark","Benchmark"), ["button.random"]=("🎲 Seed","🎲 Seed"), ["button.copy"]=("Copiar frame","Copy frame"), ["button.addcolor"]=("+ Color","+ Color"),
        ["button.export"]=("Exportar…","Export…"), ["button.import"]=("Importar PS1","Import PS1"), ["button.save"]=("Guardar frame","Save frame"),
        ["label.width"]=("Ancho","Width"), ["label.height"]=("Alto","Height"), ["label.duration"]=("Duración","Duration"),
        ["check.invert"]=("Invertir rampa","Invert ramp"), ["check.color"]=("Color en preview/export","Color in preview/export"),
        ["specific.none"]=("Este efecto usa los controles generales.","This effect uses the general controls."),
        ["status.init"]=("Inicializando OpenGL…","Initializing OpenGL…")
    };


    private static readonly Dictionary<string, (string Es, string En)> Tips = new(StringComparer.OrdinalIgnoreCase)
    {
        ["button.random"]=("Cambia la semilla para obtener otra variación del mismo efecto.","Changes the seed to create another variation of the same effect."),
        ["button.restart"]=("Vuelve a empezar la animación desde el principio.","Restarts the animation from the beginning."),
        ["button.pause"]=("Congela o continúa la animación.","Pauses or resumes the animation."),
        ["button.benchmark"]=("Mide cuánto tarda en dibujarse el efecto con la GPU.","Measures how quickly the GPU can draw the effect."),
        ["button.copy"]=("Copia el frame visible como texto ASCII real.","Copies the visible frame as real ASCII text."),
        ["button.addcolor"]=("Añade otro color al gradiente.","Adds another color to the gradient."),
        ["button.export"]=("Guarda la animación en PowerShell, HTML, JSON, C#, ANSI o texto.","Exports the animation as PowerShell, HTML, JSON, C#, ANSI or text."),
        ["button.import"]=("Carga frames de un PowerShell reconocido sin ejecutar el script.","Loads frames from a recognized PowerShell file without executing the script."),
        ["button.save"]=("Guarda el frame visible como texto ASCII.","Saves the visible frame as ASCII text."),
        ["check.invert"]=("Intercambia los caracteres usados para zonas claras y oscuras.","Swaps the characters used for bright and dark areas."),
        ["check.color"]=("Activa o desactiva el color sin quitar el arte ASCII.","Turns color on or off without removing the ASCII art.")
    };

    private static readonly Dictionary<string, string> LabelEn = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Velocidad"]="Speed", ["Escala"]="Scale", ["Gamma"]="Gamma", ["Aspecto"]="Aspect", ["Amplitud"]="Amplitude",
        ["Frecuencia X"]="X frequency", ["Frecuencia Y"]="Y frequency", ["Freq. diagonal"]="Diagonal frequency", ["Freq. radial"]="Radial frequency", ["Freq. temporal"]="Time frequency",
        ["Fase"]="Phase", ["Turbulencia"]="Turbulence", ["Warp"]="Warp", ["Deriva X"]="X drift", ["Deriva Y"]="Y drift", ["Pulso"]="Pulse", ["Densidad"]="Density", ["Iteraciones"]="Iterations",
        ["Altura"]="Height", ["Anchura"]="Width", ["Viento"]="Wind", ["Partículas"]="Particles", ["Tamaño partículas"]="Particle size", ["Ascenso partículas"]="Particle lift",
        ["Cantidad"]="Amount", ["Tamaño"]="Size", ["Gravedad"]="Gravity", ["Rebote"]="Bounce", ["Estela"]="Trail", ["Explosiones"]="Explosions", ["Chispas"]="Sparks", ["Persistencia"]="Persistence",
        ["Cantidad gotas"]="Drop count", ["Tamaño anillo"]="Ring size", ["Grosor"]="Thickness", ["Desvanecimiento"]="Fade", ["Brazos"]="Arms", ["Núcleo"]="Core", ["Twist"]="Twist", ["Halo"]="Halo",
        ["Radio mayor"]="Major radius", ["Radio tubo"]="Tube radius", ["Rotación X"]="X rotation", ["Rotación Y"]="Y rotation", ["Detalle"]="Detail", ["Estrellas"]="Stars", ["Tamaño estrella"]="Star size", ["Profundidad"]="Depth",
        ["Longitud estela"]="Trail length", ["Separación"]="Spacing", ["Brillo cabeza"]="Head brightness", ["Anillos"]="Rings", ["Altura horizonte"]="Horizon height", ["Perspectiva"]="Perspective", ["Ondulación"]="Waves",
        ["Grosor pulso"]="Pulse thickness", ["Caída"]="Falloff", ["Expansión"]="Expansion", ["Regla"]="Rule", ["Velocidad pasos"]="Step speed", ["Historial"]="History", ["Densidad inicial"]="Initial density", ["Contraste vivo"]="Alive contrast", ["Bloques / scroll"]="Blocks / scroll",
        ["Cantidad ondas"]="Wave count", ["Longitud"]="Length", ["Dirección"]="Direction", ["Apertura"]="Spread", ["Cresta"]="Crest", ["Altura olas"]="Wave height", ["Tamaño ola"]="Wave size", ["Capas"]="Layers", ["Dirección viento"]="Wind direction", ["Caos"]="Chaos", ["Espuma"]="Foam",
        ["Fuentes"]="Sources", ["Frecuencia"]="Frequency", ["Amortiguación"]="Damping", ["Movimiento fuentes"]="Source motion", ["Interferencia"]="Interference", ["Forma (0-4)"]="Waveform (0-4)", ["Segunda señal"]="Second signal", ["Desfase"]="Phase offset",
        ["Distorsión"]="Distortion", ["Brillo líneas"]="Line brightness", ["Bandas"]="Bands", ["Flujo"]="Flow", ["Curvatura"]="Curvature", ["Centelleo"]="Shimmer",
        ["Figura (0-6)"]="Shape (0-6)", ["Giro X"]="X spin", ["Giro Y"]="Y spin", ["Giro Z"]="Z spin", ["Cámara"]="Camera", ["Luz"]="Light", ["Modo"]="Mode", ["Agua"]="Water", ["Rejilla"]="Grid",
        ["Repetición"]="Repeat", ["Fusión"]="Blend", ["Giro"]="Spin", ["Trazas"]="Traces", ["Escala flujo"]="Flow scale", ["Fuerza"]="Strength", ["Curl"]="Curl", ["Ramas"]="Branches", ["Bifurcación"]="Forking", ["Destello"]="Flash",
        ["Horizonte"]="Horizon", ["Disco"]="Disk", ["Lente"]="Lens", ["Jets"]="Jets", ["Tipo (0-3)"]="Type (0-3)", ["Puntos"]="Points", ["Zoom"]="Zoom", ["Rotación"]="Rotation", ["Brillo"]="Brightness", ["Células"]="Cells", ["Bordes"]="Edges", ["Relleno"]="Fill", ["Deformación"]="Warp", ["Ráfagas"]="Gusts",
        ["Vueltas"]="Turns", ["Radio"]="Radius", ["Peldaños"]="Rungs", ["Inclinación"]="Tilt", ["Onda"]="Wave"
    };

    private static readonly Dictionary<string, string> EffectHelpEn = new(StringComparer.OrdinalIgnoreCase)
    {
        ["3D Shapes"]="Renders rotating 3D shapes with perspective and lighting.", ["3D Terrain"]="Generates a procedural 3D landscape moving under the camera.",
        ["SDF Lab"]="Experiments with 3D solids, repetition, twisting and smooth blending.", ["Flow Field"]="Draws trails that follow invisible currents and vortices.",
        ["Lightning"]="Generates branching lightning bolts and electrical flashes.", ["Black Hole"]="Creates a black hole with disk, lensing, stars and jets.",
        ["Strange Attractor"]="Draws chaotic systems that form mathematical curves and clouds.", ["Voronoi Cells"]="Creates moving cellular mosaics and their borders.",
        ["Snowstorm"]="Simulates snow with depth, wind and gusts.", ["DNA Helix"]="Draws an animated double helix with a sense of depth.",
        ["Warp Grid 3D"]="Shows a perspective grid that bends and twists.", ["Fire"]="Generates flames, sparks and wind-driven fire.", ["Fireworks"]="Launches rockets and spark explosions.",
        ["Cellular Automaton"]="Creates cell patterns where each row grows from the previous one using a rule.", ["Wave Field"]="Mixes several waves travelling in different directions.",
        ["Ocean Waves"]="Simulates irregular layered ocean waves and foam.", ["Ripple Tank"]="Creates circular waves from several sources and shows their interference.",
        ["Oscilloscope"]="Draws sine, square, triangle, sawtooth and mixed signals.", ["Water Caustics"]="Imitates moving light patterns seen under water.", ["Aurora"]="Creates flowing light curtains like an aurora.",
        ["Horizon"]="Draws a perspective grid fading into the horizon.", ["Ripples"]="Combines soft circular waves.", ["Radio Waves"]="Shows pulses expanding outward from a center point."
    };

    public static string Text(string key) => Ui.TryGetValue(key, out var v) ? (English ? v.En : v.Es) : key;
    public static string Tip(string key) => Tips.TryGetValue(key, out var v) ? (English ? v.En : v.Es) : "";
    public static string Label(ParamDesc d) => English ? LabelEn.GetValueOrDefault(d.Label, HumanizeKey(d.Key)) : d.Label;
    public static string Help(ParamDesc d)
    {
        if (!English) return d.Help;
        if (d.Key == "drift_x") return "Makes the internal pattern flow sideways without dragging the whole effect off screen.";
        if (d.Key == "drift_y") return "Makes the internal pattern flow up or down without dragging the whole effect off screen.";
        if (d.Key == "speed") return "Makes the whole animation run slower or faster.";
        if (d.Key == "scale") return "Makes the pattern look larger or smaller.";
        if (d.Key == "turbulence") return "Adds irregular motion and breaks up shapes that look too perfect.";
        if (d.Key == "warp") return "Twists and deforms the pattern.";
        if (d.Key == "pulse") return "Makes the effect grow and shrink as if it were breathing.";
        if (d.Key == "fire_wind") return "Tilts the flames sideways without moving the whole fire off screen.";
        if (d.Key.StartsWith("firework_")) return $"Controls {Label(d).ToLowerInvariant()} for the firework explosions.";
        if (d.Key.StartsWith("ocean_")) return $"Controls {Label(d).ToLowerInvariant()} for the ocean surface.";
        if (d.Key.StartsWith("wave_")) return $"Controls {Label(d).ToLowerInvariant()} for the wave field.";
        if (d.Key.StartsWith("shape3d_")) return $"Controls {Label(d).ToLowerInvariant()} for the 3D object.";
        if (d.Key.StartsWith("terrain_")) return $"Controls {Label(d).ToLowerInvariant()} for the 3D terrain.";
        if (d.Key.StartsWith("blackhole_")) return $"Controls {Label(d).ToLowerInvariant()} around the black hole.";
        if (d.Key.StartsWith("lightning_")) return $"Controls {Label(d).ToLowerInvariant()} for the lightning effect.";
        if (d.Key.StartsWith("ca_")) return $"Controls {Label(d).ToLowerInvariant()} for the cellular automaton.";
        return $"Changes the {Label(d).ToLowerInvariant()} of this effect.";
    }

    public static ParamDesc Param(ParamDesc d) => English ? d with { Label = Label(d), Help = Help(d) } : d;
    public static string EffectHelp(string effect) => English ? EffectHelpEn.GetValueOrDefault(effect, "Generates this kind of animation. Change the controls and watch the result live.") : "";

    private static string HumanizeKey(string key)
    {
        string[] knownPrefixes = ["shape3d_","firework_","fire_","ball_","rain_","galaxy_","donut_","star_","matrix_","tunnel_","horizon_","radio_","ca_","wave_","ocean_","tank_","scope_","caustic_","aurora_","terrain_","sdf_","flow_","lightning_","blackhole_","attractor_","voronoi_","snow_","dna_","warpgrid_"];
        foreach (var p in knownPrefixes) if (key.StartsWith(p, StringComparison.OrdinalIgnoreCase)) { key = key[p.Length..]; break; }
        return string.Join(' ', key.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(x => char.ToUpperInvariant(x[0]) + x[1..]));
    }
}
