using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CicdLight;

public class AppSettings
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CicdLight", "settings.json");

    public string Organization { get; set; } = "";          // e.g. "myorg" or full https://dev.azure.com/myorg
    public string Projects { get; set; } = "";              // comma separated; empty = all projects
    public string EncryptedPat { get; set; } = "";          // DPAPI (current user)
    public int PollSeconds { get; set; } = 60;
    public bool Notifications { get; set; } = true;
    public bool OnlyMyBuilds { get; set; } = false;
    public bool StartWithWindows { get; set; } = false;
    public string Language { get; set; } = "";                 // "" = system, "en", "pl"
    public List<string> WatchedItems { get; set; } = [];   // keys of watched pipelines/releases; empty = all

    [System.Text.Json.Serialization.JsonIgnore]
    public string Pat
    {
        get
        {
            if (string.IsNullOrEmpty(EncryptedPat)) return "";
            try
            {
                var bytes = ProtectedData.Unprotect(Convert.FromBase64String(EncryptedPat), null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (CryptographicException) { return ""; }
        }
        set => EncryptedPat = string.IsNullOrEmpty(value) ? "" :
            Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser));
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public string OrganizationUrl
    {
        get
        {
            var o = Organization.Trim().TrimEnd('/');
            return o.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? o : $"https://dev.azure.com/{o}";
        }
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public string[] ProjectList => Projects.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Organization) && !string.IsNullOrEmpty(Pat);

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch (JsonException) { }
        return new AppSettings();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
