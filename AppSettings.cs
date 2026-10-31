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
    public string GitHubRepositories { get; set; } = "";
    public string EncryptedGitHubToken { get; set; } = "";
    public List<IntegrationConnection> Connections { get; set; } = [];
    public int PollSeconds { get; set; } = 60;
    public bool Notifications { get; set; } = true;
    public bool NotifyBuildFailed { get; set; } = true;
    public bool NotifyBuildFixed { get; set; } = true;
    public bool NotifyReviewRequested { get; set; } = true;
    public bool OnlyMyBuilds { get; set; } = false;
    public bool StartWithWindows { get; set; } = false;
    public string Language { get; set; } = "";                 // "" = system, "en", "pl"
    public List<string> WatchedItems { get; set; } = [];   // keys of watched pipelines/releases; empty = all
    public Dictionary<string, ProjectNotificationSettings> ProjectNotifications { get; set; } = new(StringComparer.OrdinalIgnoreCase);

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
    public string GitHubToken
    {
        get
        {
            if (string.IsNullOrEmpty(EncryptedGitHubToken)) return "";
            try
            {
                var bytes = ProtectedData.Unprotect(Convert.FromBase64String(EncryptedGitHubToken), null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (CryptographicException) { return ""; }
        }
        set => EncryptedGitHubToken = string.IsNullOrEmpty(value) ? "" :
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
    public string[] GitHubRepositoryList => GitHubRepositories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsAzureDevOpsConfigured => Connections.Any(connection => connection.Provider == IntegrationProvider.AzureDevOps);

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsGitHubConfigured => Connections.Any(connection => connection.Provider == IntegrationProvider.GitHub);

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsConfigured => Connections.Count > 0;

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
                if (settings.MigrateLegacyConnections()) settings.Save();
                return settings;
            }
        }
        catch (JsonException) { }
        return new AppSettings();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }

    private bool MigrateLegacyConnections()
    {
        if (Connections.Count > 0) return false;

        var oldPat = Pat;
        if (!string.IsNullOrWhiteSpace(Organization) && !string.IsNullOrWhiteSpace(oldPat))
        {
            var projects = ProjectList.Length == 0 ? [""] : ProjectList.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (var project in projects)
            {
                var connection = new IntegrationConnection
                {
                    Provider = IntegrationProvider.AzureDevOps,
                    Organization = Organization,
                    Project = project
                };
                connection.Credential = oldPat;
                MigrateSelection(connection, string.IsNullOrEmpty(project) ? null : project);
                Connections.Add(connection);
            }
        }

        var oldGitHubToken = GitHubToken;
        foreach (var repository in GitHubRepositoryList.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var connection = new IntegrationConnection
            {
                Provider = IntegrationProvider.GitHub,
                Repository = repository
            };
            connection.Credential = oldGitHubToken;
            MigrateSelection(connection, repository);
            Connections.Add(connection);
        }

        if (Connections.Count == 0) return false;

        Organization = "";
        Projects = "";
        EncryptedPat = "";
        GitHubRepositories = "";
        EncryptedGitHubToken = "";
        WatchedItems = [];
        return true;
    }

    private void MigrateSelection(IntegrationConnection connection, string? project)
    {
        if (WatchedItems.Count == 0) return;
        foreach (var key in WatchedItems)
        {
            var separator = key.IndexOf(':');
            if (separator < 0) continue;
            var resource = key[(separator + 1)..];
            if (project != null && !resource.StartsWith(project + "/", StringComparison.OrdinalIgnoreCase)) continue;
            connection.SelectedItems.Add($"{key[..(separator + 1)]}{connection.Id}:{resource}");
        }
    }
}
