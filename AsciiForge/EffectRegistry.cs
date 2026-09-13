namespace AsciiForge;

internal static class EffectRegistry
{
    private static readonly Dictionary<string, int> EffectIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Plasma"] = 1, ["Mandelbrot Zoom"] = 2, ["Julia"] = 3, ["Burning Ship"] = 4,
        ["Value Noise"] = 5, ["FBM Noise"] = 6, ["Tunnel"] = 7, ["Starfield"] = 8,
        ["Interference"] = 9, ["Metaballs"] = 10, ["Kaleidoscope"] = 11, ["Ripples"] = 12,
        ["Fire"] = 13, ["Matrix Rain"] = 14, ["XOR Pattern"] = 15, ["Moire"] = 16,
        ["Fireworks"] = 17, ["Radio Waves"] = 18, ["Rain Drops"] = 19, ["Rotating Galaxy"] = 20,
        ["Spinning Donut"] = 21, ["Spinning Shapes"] = 22, ["Horizon"] = 23, ["Bouncing Balls"] = 24,
        ["Cellular Automaton"] = 25, ["Wave Field"] = 26, ["Ocean Waves"] = 27, ["Ripple Tank"] = 28,
        ["Oscilloscope"] = 29, ["Water Caustics"] = 30, ["Aurora"] = 31, ["3D Shapes"] = 32,
        ["3D Terrain"] = 33, ["SDF Lab"] = 34, ["Flow Field"] = 35, ["Lightning"] = 36,
        ["Black Hole"] = 37, ["Strange Attractor"] = 38, ["Voronoi Cells"] = 39, ["Snowstorm"] = 40,
        ["DNA Helix"] = 41, ["Warp Grid 3D"] = 42
    };

    private static readonly Dictionary<string, int> ShapeIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Square"] = 0, ["Diamond"] = 1, ["Star"] = 2, ["Hex"] = 3, ["Cross"] = 4
    };

    public static int EffectId(string effect) => EffectIds.GetValueOrDefault(effect, 1);
    public static int ShapeId(string shape) => ShapeIds.GetValueOrDefault(shape, 0);
}
