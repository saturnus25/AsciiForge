using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AsciiForge;

internal static class PowerShellImport
{
    private static readonly Regex Ansi = new("\\x1B\\[[0-?]*[ -/]*[@-~]", RegexOptions.Compiled);

    public static string StripAnsi(string text) => Ansi.Replace(text, "");

    public static (List<string> Frames, double Fps) Parse(string text)
    {
        var payloadMatch = Regex.Match(
            text,
            @"\$payload\s*=\s*@'\s*(.*?)\s*'@",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (payloadMatch.Success)
        {
            byte[] packed = Convert.FromBase64String(Regex.Replace(payloadMatch.Groups[1].Value, @"\s+", ""));
            using var ms = new MemoryStream(packed);
            using var gz = new GZipStream(ms, CompressionMode.Decompress);
            using var sr = new StreamReader(gz, Encoding.UTF8);
            var frames = JsonSerializer.Deserialize<List<string>>(sr.ReadToEnd()) ?? [];
            return (frames, DetectFps(text));
        }

        var framesMatch = Regex.Match(
            text,
            @"\$frames\s*=\s*@\((.*?)\)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (framesMatch.Success)
        {
            var frames = new List<string>();
            foreach (Match match in Regex.Matches(
                         framesMatch.Groups[1].Value,
                         "'((?:''|[^'])*)'|\"((?:`.|[^\"])*)\"",
                         RegexOptions.Singleline))
            {
                frames.Add(match.Groups[1].Success
                    ? match.Groups[1].Value.Replace("''", "'")
                    : match.Groups[2].Value.Replace("`n", "\n").Replace("`r", "\r").Replace("`t", "\t"));
            }

            if (frames.Count > 0)
                return (frames, DetectFps(text));
        }

        throw new InvalidDataException(
            Localization.English
                ? "No recognized static frames were found. AsciiForge never executes arbitrary PowerShell code."
                : "No encontré frames estáticos reconocibles. AsciiForge nunca ejecuta PowerShell arbitrario.");
    }

    private static double DetectFps(string text)
    {
        var match = Regex.Match(
            text,
            @"\$frameMs\s*=\s*1000(?:\.0)?\s*/\s*([0-9.]+)",
            RegexOptions.IgnoreCase);

        return match.Success && double.TryParse(
            match.Groups[1].Value,
            System.Globalization.CultureInfo.InvariantCulture,
            out double fps)
            ? fps
            : 20;
    }
}
