using System.Globalization;
using System.Resources;

namespace CicdLight;

/// <summary>Dostęp do zasobów tekstowych (Strings.resx, Strings.pl.resx).</summary>
internal static class Strings
{
    private static readonly ResourceManager Manager = new("CicdLight.Strings", typeof(Strings).Assembly);

    private static CultureInfo? _culture;

    /// <summary>Ustawia język interfejsu: "" = język systemu, "en", "pl".</summary>
    public static void SetLanguage(string? code) =>
        _culture = string.IsNullOrWhiteSpace(code) ? null : CultureInfo.GetCultureInfo(code);

    public static string Get(string name) => Manager.GetString(name, _culture ?? CultureInfo.CurrentUICulture) ?? name;

    public static string Format(string name, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, Get(name), args);
}
