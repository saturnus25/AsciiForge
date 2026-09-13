using System.Reflection;
using System.Text.Json;

namespace AsciiForge;

internal sealed class AppData
{
    public Dictionary<string, Dictionary<string, Dictionary<string, JsonElement>>> Presets { get; private set; } = new();
    public Dictionary<string, List<string>> Palettes { get; private set; } = new();
    public Dictionary<string, string> Charsets { get; private set; } = new();

    public static AppData Load()
    {
        var asm = Assembly.GetExecutingAssembly();
        return new AppData
        {
            Presets = LoadJson<Dictionary<string, Dictionary<string, Dictionary<string, JsonElement>>>>(asm, "Presets.json"),
            Palettes = LoadJson<Dictionary<string, List<string>>>(asm, "Palettes.json"),
            Charsets = LoadJson<Dictionary<string, string>>(asm, "Charsets.json")
        };
    }

    private static T LoadJson<T>(Assembly asm, string suffix) where T : new()
    {
        var name = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        if (name is null) return new T();
        using var stream = asm.GetManifestResourceStream(name)!;
        return JsonSerializer.Deserialize<T>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new T();
    }
}

internal sealed class EffectSettings
{
    public int Width { get; set; } = 178;
    public int Height { get; set; } = 50;
    public int Fps { get; set; } = 30;
    public double Duration { get; set; } = 6.0;
    public int Seed { get; set; } = 1337;
    public string Effect { get; set; } = "Plasma";
    public string Preset { get; set; } = "Classic Plasma";
    public string CharsetName { get; set; } = "Classic";
    public string Charset { get; set; } = " .:-=+*#%@";
    public string PaletteName { get; set; } = "Plasma";
    public List<string> PaletteStops { get; set; } = new() { "#090122", "#3d0f7d", "#9c179e", "#e84a8a", "#ffad5a", "#fff0b6" };
    public bool ColorEnabled { get; set; } = true;
    public bool IncludeExportCredit { get; set; } = true;
    public bool Invert { get; set; }
    public string ShapeMode { get; set; } = "Square";

    public Dictionary<string, double> V { get; } = new(StringComparer.OrdinalIgnoreCase);

    public EffectSettings() => ResetValues();

    public void ResetValues()
    {
        V.Clear();
        foreach (var kv in ParameterCatalog.Defaults) V[kv.Key] = kv.Value;
    }

    public double Get(string key) => V.TryGetValue(key, out var value) ? value : ParameterCatalog.Defaults.GetValueOrDefault(key, 0.0);
    public float F(string key) => (float)Get(key);
    public void Set(string key, double value) => V[key] = value;

    public EffectSettings Clone()
    {
        var x = new EffectSettings
        {
            Width = Width, Height = Height, Fps = Fps, Duration = Duration, Seed = Seed,
            Effect = Effect, Preset = Preset, CharsetName = CharsetName, Charset = Charset,
            PaletteName = PaletteName, PaletteStops = new List<string>(PaletteStops),
            ColorEnabled = ColorEnabled, IncludeExportCredit = IncludeExportCredit, Invert = Invert, ShapeMode = ShapeMode
        };
        x.V.Clear();
        foreach (var kv in V) x.V[kv.Key] = kv.Value;
        return x;
    }
}

internal readonly record struct ParamDesc(string Label, string Key, double Min, double Max, double Default, int Digits = 2, string Help = "");

internal static class ParameterCatalog
{
    public static readonly ParamDesc[] General =
    [
        new("Velocidad", "speed", .03, 4.0, 1.0, 2, "Hace que toda la animación vaya más lenta o más rápida."),
        new("Escala", "scale", .2, 4.0, 1.0, 2, "Hace el dibujo más grande o más pequeño."),
        new("Gamma", "gamma", .25, 2.5, 1.0, 2, "Hace destacar más las zonas oscuras o las claras sin cambiar la forma."),
        new("Aspecto", "aspect", .25, 1.0, .50, 2, "Corrige si el dibujo se ve demasiado ancho o demasiado estrecho."),
        new("Amplitud", "osc_amp", 0.0, 2.5, 1.0, 2, "Hace más fuerte o más suave el efecto principal."),
        new("Frecuencia X", "freq_x", .05, 30.0, 1.35, 2, "Junta o separa el patrón en horizontal."),
        new("Frecuencia Y", "freq_y", .05, 30.0, 1.75, 2, "Junta o separa el patrón en vertical."),
        new("Freq. diagonal", "freq_diag", .05, 30.0, 1.05, 2, "Cambia cuántas diagonales, brazos o giros aparecen cuando el efecto los usa."),
        new("Freq. radial", "freq_radial", .05, 30.0, 2.8, 2, "Junta o separa anillos y ondas que salen desde un centro."),
        new("Freq. temporal", "time_freq", .03, 4.0, 1.0, 2, "Cambia lo rápido que oscilan y se deforman las partes animadas."),
        new("Fase", "phase_deg", 0.0, 360.0, 0.0, 0, "Cambia el punto del ciclo en el que empieza la animación."),
        new("Turbulencia", "turbulence", 0.0, 3.0, 0.0, 2, "Añade movimiento irregular y rompe formas demasiado perfectas."),
        new("Warp", "warp", 0.0, 2.0, .25, 2, "Retuerce y deforma el patrón."),
        new("Deriva X", "drift_x", -2.0, 2.0, 0.0, 2, "Hace fluir el patrón interno hacia los lados; ya no arrastra el efecto entero fuera de pantalla."),
        new("Deriva Y", "drift_y", -2.0, 2.0, 0.0, 2, "Hace fluir el patrón interno hacia arriba o abajo; ya no arrastra el efecto entero fuera de pantalla."),
        new("Pulso", "pulse", 0.0, 2.0, .15, 2, "Hace que el efecto crezca y disminuya como si respirase."),
        new("Densidad", "density", .1, 2.5, 1.0, 2, "Añade o quita elementos y detalle, según el efecto."),
        new("Iteraciones", "iterations", 4, 250, 42, 0, "Añade detalle a los fractales. Más valor suele costar más GPU.")
    ];

    public static readonly Dictionary<string, ParamDesc[]> Specific = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Fire"] = [
            new("Altura", "fire_height", .15, 3, 1, 2, "Hace las llamas más altas o más bajas."),
            new("Anchura", "fire_width", .10, 3, 1, 2, "Hace la base y el cuerpo del fuego más anchos o estrechos."),
            new("Viento", "fire_wind", -4, 4, 0, 2, "Inclina las llamas hacia un lado sin llevarse la hoguera fuera de pantalla."),
            new("Partículas", "fire_particles", 0, 4, .55, 2, "Añade o quita chispas que salen del fuego."),
            new("Tamaño partículas", "fire_particle_size", .1, 4, 1, 2, "Cambia el tamaño de las chispas."),
            new("Ascenso partículas", "fire_particle_lift", 0, 4, 1, 2, "Hace que las chispas suban más despacio o más rápido.")],
        ["Bouncing Balls"] = [
            new("Cantidad", "ball_count", 1, 64, 8, 0, "Cambia cuántas pelotas hay en pantalla."),
            new("Tamaño", "ball_radius", .02, .45, .12, 3, "Hace las pelotas más grandes o pequeñas."),
            new("Gravedad", "ball_gravity", 0, 3, .85, 2, "Hace el arco del salto más pesado o más flotante."),
            new("Rebote", "ball_bounce", .1, 1.4, .95, 2, "Cambia cuánto suben después de tocar el suelo."),
            new("Velocidad", "ball_speed", .05, 4, 1, 2, "Hace que las pelotas se muevan más rápido o más lento."),
            new("Estela", "ball_trails", 0, 1, 0, 2, "Deja una pequeña sombra detrás de las pelotas en movimiento.")],
        ["Fireworks"] = [
            new("Explosiones", "firework_count", 1, 20, 5, 0, "Cambia cuántos fuegos artificiales pueden coincidir."),
            new("Tamaño", "firework_size", .1, 3, 1, 2, "Hace cada explosión más grande o pequeña."),
            new("Chispas", "firework_sparks", 4, 80, 28, 0, "Cambia cuántas chispas salen de cada explosión."),
            new("Gravedad", "firework_gravity", 0, 3, .65, 2, "Hace que las chispas caigan más o menos al apagarse."),
            new("Persistencia", "firework_decay", .1, 3, 1, 2, "Cambia cuánto tardan las chispas en desaparecer.")],
        ["Rain Drops"] = [
            new("Cantidad gotas", "rain_count", 1, 30, 8, 0, "Cambia cuántos impactos producen anillos."),
            new("Tamaño anillo", "rain_ring_size", .1, 3, 1, 2, "Hace que los anillos se expandan más o menos."),
            new("Grosor", "rain_ring_width", .01, .18, .045, 3, "Hace los anillos más finos o gruesos."),
            new("Desvanecimiento", "rain_decay", .1, 4, 1, 2, "Cambia lo rápido que desaparece cada onda.")],
        ["Rotating Galaxy"] = [
            new("Brazos", "galaxy_arms", 1, 10, 3, 0, "Cambia cuántos brazos tiene la galaxia."),
            new("Núcleo", "galaxy_core", .05, .8, .24, 2, "Hace el centro brillante más grande o pequeño."),
            new("Twist", "galaxy_twist", .1, 12, 4.5, 2, "Enrolla más o menos los brazos."),
            new("Halo", "galaxy_halo", 0, 3, 1, 2, "Añade brillo y estrellas alrededor del disco.")],
        ["Spinning Donut"] = [
            new("Radio mayor", "donut_major", .2, 3, 1.55, 2, "Cambia el tamaño general del donut."),
            new("Radio tubo", "donut_minor", .05, 1.5, .62, 2, "Hace más gordo o fino el tubo del donut."),
            new("Rotación X", "donut_spin_x", -3, 3, .75, 2, "Cambia cuánto gira en un eje."),
            new("Rotación Y", "donut_spin_y", -3, 3, .43, 2, "Cambia cuánto gira en el otro eje."),
            new("Detalle", "donut_detail", .2, 3, 1, 2, "Hace la superficie más fina o más basta.")],
        ["Starfield"] = [
            new("Estrellas", "star_amount", .1, 4, 1, 2, "Añade o quita estrellas."),
            new("Tamaño estrella", "star_size", .2, 4, 1, 2, "Hace los puntos de luz más grandes o pequeños."),
            new("Profundidad", "star_depth", .2, 4, 1, 2, "Aumenta la sensación de capas y distancia.")],
        ["Matrix Rain"] = [
            new("Longitud estela", "matrix_trail", .1, 4, 1, 2, "Hace más largas o cortas las columnas que caen."),
            new("Separación", "matrix_spacing", .25, 4, 1, 2, "Deja más o menos espacio entre columnas."),
            new("Brillo cabeza", "matrix_head", .2, 3, 1, 2, "Hace destacar más la parte delantera de cada columna.")],
        ["Tunnel"] = [
            new("Anillos", "tunnel_rings", .1, 4, 1, 2, "Junta o separa los anillos del túnel."),
            new("Twist", "tunnel_twist", -4, 4, 1, 2, "Hace que el túnel se retuerza hacia un lado u otro."),
            new("Profundidad", "tunnel_depth", .1, 4, 1, 2, "Cambia lo profunda que parece la perspectiva.")],
        ["Horizon"] = [
            new("Altura horizonte", "horizon_height", -.8, .8, 0, 2, "Sube o baja la línea del horizonte."),
            new("Perspectiva", "horizon_fov", .2, 4, 1, 2, "Hace la rejilla más abierta o más comprimida hacia el fondo."),
            new("Ondulación", "horizon_wave", 0, 3, .25, 2, "Hace que el suelo se curve y ondule.")],
        ["Radio Waves"] = [
            new("Grosor pulso", "radio_thickness", .01, .5, .12, 3, "Hace los anillos más finos o gruesos."),
            new("Caída", "radio_decay", 0, 4, .6, 2, "Hace que las ondas pierdan fuerza más rápido al alejarse."),
            new("Expansión", "radio_expand", .05, 4, 1, 2, "Hace que los pulsos se expandan más rápido o más lento.")],
        ["Cellular Automaton"] = [
            new("Regla", "ca_rule", 0, 255, 30, 0, "Elige la regla del autómata. 30 es caótica, 90 crea triángulos y 110 forma estructuras complejas."),
            new("Velocidad pasos", "ca_step_rate", .1, 30, 6, 2, "Cambia lo rápido que aparecen nuevas generaciones."),
            new("Historial", "ca_history", 4, 32, 24, 0, "Cambia cuántas generaciones se ven a la vez."),
            new("Densidad inicial", "ca_seed_density", 0, 1, .48, 2, "Decide cuántas celdas empiezan encendidas. Cero usa una única celda central."),
            new("Contraste vivo", "ca_alive", .1, 2.5, 1, 2, "Hace destacar más o menos las celdas vivas."),
            new("Bloques / scroll", "ca_scroll", 0, 2, 1, 2, "Hace que el historial avance por la pantalla. Cero lo deja quieto.")],
        ["Wave Field"] = [
            new("Cantidad ondas", "wave_count", 1, 8, 3, 0, "Superpone más o menos ondas."),
            new("Altura", "wave_height", 0, 2.5, 1, 2, "Hace las crestas y valles más marcados."),
            new("Longitud", "wave_length", .1, 4, 1, 2, "Hace las ondas más largas o más cortas."),
            new("Dirección", "wave_direction", -180, 180, 0, 0, "Gira la dirección en la que viajan las ondas."),
            new("Apertura", "wave_spread", 0, 120, 28, 0, "Separa las direcciones de las distintas ondas."),
            new("Cresta", "wave_sharpness", .2, 5, 1, 2, "Hace las crestas más suaves o más afiladas.")],
        ["Ocean Waves"] = [
            new("Altura olas", "ocean_height", 0, 2.5, 1, 2, "Hace el mar más calmado o más bravo."),
            new("Tamaño ola", "ocean_length", .15, 4, 1, 2, "Hace las olas grandes y largas o pequeñas y juntas."),
            new("Capas", "ocean_layers", 1, 8, 5, 0, "Añade más tamaños de ola al mismo tiempo."),
            new("Dirección viento", "ocean_direction", -180, 180, 15, 0, "Gira la dirección principal del oleaje."),
            new("Caos", "ocean_choppiness", 0, 3, .8, 2, "Rompe las olas perfectas y hace el mar más irregular."),
            new("Espuma", "ocean_foam", 0, 2.5, .65, 2, "Añade brillo en las crestas más altas.")],
        ["Ripple Tank"] = [
            new("Fuentes", "tank_sources", 1, 12, 4, 0, "Cambia cuántos puntos generan ondas."),
            new("Frecuencia", "tank_frequency", 1, 30, 11, 2, "Junta o separa los anillos de cada fuente."),
            new("Velocidad", "tank_speed", .05, 4, 1, 2, "Hace que las ondas avancen más rápido o más lento."),
            new("Amortiguación", "tank_damping", 0, 4, .55, 2, "Hace que las ondas pierdan fuerza al alejarse."),
            new("Movimiento fuentes", "tank_motion", 0, 2, .35, 2, "Hace que los puntos que generan ondas se muevan."),
            new("Interferencia", "tank_interference", 0, 2, 1, 2, "Hace más fuerte o suave el dibujo donde las ondas se cruzan.")],
        ["Oscilloscope"] = [
            new("Forma (0-4)", "scope_waveform", 0, 4, 0, 0, "0 seno, 1 cuadrada, 2 triangular, 3 diente de sierra, 4 mezcla."),
            new("Frecuencia", "scope_frequency", .1, 12, 1.5, 2, "Cambia cuántas ondas aparecen a lo ancho."),
            new("Amplitud", "scope_amplitude", .05, .95, .55, 2, "Hace la señal más alta o más plana."),
            new("Grosor", "scope_thickness", .005, .18, .035, 3, "Hace la línea del osciloscopio más fina o gruesa."),
            new("Segunda señal", "scope_dual", 0, 1, 0, 2, "Añade una segunda señal para crear cruces y figuras."),
            new("Desfase", "scope_phase", 0, 360, 90, 0, "Desplaza la segunda señal respecto a la primera.")],
        ["Water Caustics"] = [
            new("Tamaño", "caustic_scale", .2, 5, 1.3, 2, "Hace las manchas de luz más grandes o pequeñas."),
            new("Distorsión", "caustic_distortion", 0, 3, 1, 2, "Retuerce las líneas de luz como agua en movimiento."),
            new("Velocidad", "caustic_speed", .05, 4, 1, 2, "Hace que los reflejos se muevan más rápido o lento."),
            new("Brillo líneas", "caustic_sharpness", .2, 8, 3.5, 2, "Hace las líneas de luz más suaves o más concentradas."),
            new("Capas", "caustic_layers", 1, 6, 3, 0, "Superpone más patrones de luz para añadir detalle.")],
        ["Aurora"] = [
            new("Bandas", "aurora_bands", 1, 10, 4, 0, "Cambia cuántas cortinas de luz aparecen."),
            new("Anchura", "aurora_width", .03, .8, .2, 2, "Hace las cortinas más finas o anchas."),
            new("Flujo", "aurora_flow", .05, 4, .65, 2, "Hace que las cortinas recorran el cielo más rápido o lento."),
            new("Curvatura", "aurora_curl", 0, 3, 1, 2, "Hace las bandas más rectas o más serpenteantes."),
            new("Centelleo", "aurora_shimmer", 0, 2, .45, 2, "Añade pequeñas variaciones de brillo."),
            new("Altura", "aurora_height", .2, 2, 1, 2, "Cambia cuánto espacio vertical ocupa la aurora.")],
        ["3D Shapes"] = [
            new("Figura (0-6)", "shape3d_type", 0, 6, 0, 0, "0 esfera, 1 cubo, 2 octaedro, 3 toro, 4 cilindro, 5 cápsula, 6 pirámide."),
            new("Giro X", "shape3d_spin_x", -3, 3, .55, 2, "Hace girar la figura hacia delante y atrás."),
            new("Giro Y", "shape3d_spin_y", -3, 3, .75, 2, "Hace girar la figura hacia los lados."),
            new("Giro Z", "shape3d_spin_z", -3, 3, .25, 2, "Hace girar la figura sobre sí misma."),
            new("Cámara", "shape3d_camera", 2.2, 8, 4.2, 2, "Acerca o aleja la cámara de la figura."),
            new("Perspectiva", "shape3d_fov", .7, 3.5, 2.0, 2, "Abre o cierra la perspectiva de la cámara."),
            new("Luz", "shape3d_light", 0, 2, 1, 2, "Hace más fuerte o suave el sombreado de la superficie."),
            new("Modo", "shape3d_mode", 0, 2, 0, 0, "0 sólido, 1 bandas de profundidad, 2 contorno técnico.")],
        ["3D Terrain"] = [
            new("Altura", "terrain_height", 0, 2.5, 1, 2, "Hace las montañas más altas o más planas."),
            new("Detalle", "terrain_detail", .2, 4, 1, 2, "Añade o quita detalle pequeño al terreno."),
            new("Velocidad", "terrain_speed", 0, 4, .8, 2, "Hace que avances sobre el terreno más rápido o lento."),
            new("Cámara", "terrain_camera", .5, 3, 1, 2, "Sube o baja el punto de vista sobre el paisaje."),
            new("Agua", "terrain_water", -1, 1, -.35, 2, "Sube o baja el nivel del agua visible entre montañas."),
            new("Rejilla", "terrain_grid", 0, 2, .45, 2, "Añade líneas que remarcan la forma del terreno.")],
        ["SDF Lab"] = [
            new("Forma (0-4)", "sdf_shape", 0, 4, 0, 0, "0 blobs, 1 cajas, 2 toros, 3 cápsulas, 4 mezcla."),
            new("Repeticiones", "sdf_repeat", 0, 4, 1, 0, "Añade copias alrededor de la figura principal sin encerrar la cámara dentro de ellas."),
            new("Separación", "sdf_spacing", 1.15, 4.5, 2.15, 2, "Separa o acerca las copias entre sí."),
            new("Twist", "sdf_twist", -4, 4, 1, 2, "Retuerce cada figura sobre sí misma."),
            new("Fusión", "sdf_smooth", 0, 1, .35, 2, "Hace que las partes de una figura se fundan entre ellas."),
            new("Giro figura", "sdf_spin", -3, 3, .7, 2, "Hace girar las figuras dentro de la escena."),
            new("Cámara horizontal", "sdf_yaw", -180, 180, 0, 1, "Rodea la escena hacia la izquierda o derecha. También puedes arrastrar la preview."),
            new("Cámara vertical", "sdf_pitch", -75, 75, -8, 1, "Mira la escena desde más arriba o más abajo. También puedes arrastrar la preview."),
            new("Profundidad", "sdf_depth", 1.8, 24, 5.2, 2, "Acerca o aleja la cámara. Con repeticiones, la cámara nunca entra dentro del conjunto. También puedes usar la rueda sobre la preview.")],
        ["Flow Field"] = [
            new("Trazas", "flow_particles", 4, 80, 28, 0, "Añade o quita líneas que siguen el flujo."),
            new("Escala flujo", "flow_scale", .2, 6, 1.5, 2, "Hace los remolinos más grandes o pequeños."),
            new("Fuerza", "flow_strength", .1, 3, 1, 2, "Hace que las trazas se curven más o menos."),
            new("Estela", "flow_trails", .1, 3, 1, 2, "Alarga o acorta las trazas."),
            new("Curl", "flow_curl", 0, 4, 1, 2, "Añade remolinos y giros al campo."),
            new("Velocidad", "flow_speed", .05, 4, .7, 2, "Hace que el campo fluya más rápido o lento.")],
        ["Lightning"] = [
            new("Ramas", "lightning_branches", 1, 12, 5, 0, "Cambia cuántas descargas secundarias aparecen."),
            new("Grosor", "lightning_width", .005, .18, .025, 3, "Hace los rayos más finos o gruesos."),
            new("Caos", "lightning_jitter", 0, 3, 1, 2, "Hace el recorrido del rayo más recto o más salvaje."),
            new("Bifurcación", "lightning_forks", 0, 2, .8, 2, "Hace más visibles las ramas pequeñas."),
            new("Destello", "lightning_flash", 0, 3, 1, 2, "Aumenta el fogonazo alrededor del rayo."),
            new("Velocidad", "lightning_speed", .05, 5, 1, 2, "Cambia lo rápido que aparece una descarga nueva.")],
        ["Black Hole"] = [
            new("Horizonte", "blackhole_size", .08, .8, .28, 2, "Hace el agujero negro más grande o pequeño."),
            new("Disco", "blackhole_disk", .1, 3, 1, 2, "Hace el disco brillante más grande o compacto."),
            new("Giro", "blackhole_spin", -4, 4, 1, 2, "Cambia la velocidad y sentido del disco de acreción."),
            new("Lente", "blackhole_lens", 0, 3, 1, 2, "Curva más o menos el fondo alrededor del agujero."),
            new("Jets", "blackhole_jets", 0, 2, .35, 2, "Añade chorros de energía por los polos."),
            new("Estrellas", "blackhole_stars", 0, 3, 1, 2, "Añade o quita estrellas alrededor.")],
        ["Strange Attractor"] = [
            new("Tipo (0-3)", "attractor_type", 0, 3, 0, 0, "0 De Jong, 1 Clifford, 2 Hopalong, 3 Lissajous caótico."),
            new("Puntos", "attractor_points", 12, 96, 64, 0, "Añade más puntos a la curva. Más valor cuesta más GPU."),
            new("Zoom", "attractor_zoom", .2, 3, 1, 2, "Acerca o aleja el atractor."),
            new("Rotación", "attractor_rotation", -3, 3, .3, 2, "Hace girar lentamente el patrón."),
            new("Estela", "attractor_trail", .2, 3, 1, 2, "Hace la curva más continua o más punteada."),
            new("Brillo", "attractor_glow", 0, 3, 1, 2, "Añade un halo alrededor de las líneas.")],
        ["Voronoi Cells"] = [
            new("Células", "voronoi_cells", 2, 12, 6, 0, "Añade o quita regiones del mosaico."),
            new("Velocidad", "voronoi_speed", 0, 4, .5, 2, "Hace que los centros de las células se muevan más rápido."),
            new("Bordes", "voronoi_edges", 0, 3, 1, 2, "Hace más visibles las fronteras entre células."),
            new("Relleno", "voronoi_fill", 0, 2, .65, 2, "Hace más visible el interior de cada célula."),
            new("Deformación", "voronoi_warp", 0, 3, .4, 2, "Hace que las células dejen de ser tan geométricas."),
            new("Pulso", "voronoi_pulse", 0, 2, .25, 2, "Hace que las células respiren suavemente.")],
        ["Snowstorm"] = [
            new("Cantidad", "snow_amount", .1, 4, 1, 2, "Añade o quita copos de nieve."),
            new("Tamaño", "snow_size", .2, 4, 1, 2, "Hace los copos más grandes o pequeños."),
            new("Viento", "snow_wind", -3, 3, .35, 2, "Empuja la nieve hacia un lado."),
            new("Profundidad", "snow_depth", .2, 4, 1, 2, "Aumenta la diferencia entre copos cercanos y lejanos."),
            new("Ráfagas", "snow_gust", 0, 3, .7, 2, "Añade cambios de viento y remolinos."),
            new("Brillo", "snow_twinkle", 0, 2, .25, 2, "Hace que algunos copos centelleen.")],
        ["DNA Helix"] = [
            new("Vueltas", "dna_turns", .5, 8, 3, 2, "Cambia cuántas vueltas da la doble hélice."),
            new("Radio", "dna_radius", .1, .9, .45, 2, "Separa o junta las dos hebras."),
            new("Velocidad", "dna_speed", -4, 4, .8, 2, "Hace girar la hélice más rápido o al revés."),
            new("Peldaños", "dna_rungs", 2, 40, 16, 0, "Cambia cuántas uniones se ven entre las hebras."),
            new("Inclinación", "dna_tilt", -1.5, 1.5, .25, 2, "Inclina la hélice para darle más profundidad."),
            new("Profundidad", "dna_depth", 0, 2, .7, 2, "Hace más fuerte la sensación de delante y detrás.")],
        ["Warp Grid 3D"] = [
            new("Densidad", "warpgrid_density", 2, 24, 10, 0, "Junta o separa las líneas de la rejilla."),
            new("Profundidad", "warpgrid_depth", .2, 4, 1, 2, "Hace la perspectiva más plana o profunda."),
            new("Twist", "warpgrid_twist", -3, 3, .6, 2, "Retuerce la rejilla alrededor del centro."),
            new("Onda", "warpgrid_wave", 0, 3, .7, 2, "Añade ondulación continua al suelo, como una tela o una superficie flexible."),
            new("Velocidad", "warpgrid_speed", -4, 4, .8, 2, "Hace avanzar o retroceder la rejilla."),
            new("Horizonte", "warpgrid_horizon", -.6, .6, 0, 2, "Sube o baja el punto donde desaparece la rejilla."),
            new("Altura terreno", "warpgrid_terrain_height", 0, 3, 0, 2, "Levanta y hunde los vértices para formar colinas, montañas y valles."),
            new("Escala terreno", "warpgrid_terrain_scale", .12, 4, .65, 2, "Cambia el tamaño de las montañas y colinas. Bajo crea formas grandes; alto crea terreno más apretado."),
            new("Suavidad terreno", "warpgrid_terrain_smooth", 0, 1, .65, 2, "Hace el terreno más redondeado y suave o más abrupto y rocoso."),
            new("Valles", "warpgrid_terrain_valleys", 0, 2.5, .35, 2, "Profundiza las zonas bajas sin hacer más altas las montañas."),
            new("Detalle terreno", "warpgrid_terrain_detail", 0, 2.5, .55, 2, "Añade pequeñas irregularidades sobre las formas grandes del terreno.")],
    };

    public static readonly Dictionary<string, double> Defaults = BuildDefaults();
    private static Dictionary<string, double> BuildDefaults()
    {
        var d = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in General) d[p.Key] = p.Default;
        foreach (var group in Specific.Values) foreach (var p in group) d[p.Key] = p.Default;
        return d;
    }
}
