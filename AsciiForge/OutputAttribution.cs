namespace AsciiForge;

internal static class OutputAttribution
{
    public const string ProjectUrl = "https://github.com/saturnus25/AsciiForge";
    public const string CreditEnglish = "Created with AsciiForge — " + ProjectUrl;
    public const string CreditSpanish = "Creado con AsciiForge — " + ProjectUrl;

    public static string Credit => Localization.English ? CreditEnglish : CreditSpanish;
}
