using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace CicdLight;

public sealed class IntegrationConnection
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public IntegrationProvider Provider { get; set; }
    public string Repository { get; set; } = "";
    public string Organization { get; set; } = "";
    public string Project { get; set; } = "";
    public string EncryptedCredential { get; set; } = "";
    public List<string> SelectedItems { get; set; } = [];
    public bool HasSelection { get; set; }

    [JsonIgnore]
    public string? LastError { get; set; }

    [JsonIgnore]
    public bool HasStoredCredential => !string.IsNullOrWhiteSpace(EncryptedCredential);

    [JsonIgnore]
    public string Credential
    {
        get
        {
            if (string.IsNullOrEmpty(EncryptedCredential))
            {
                return "";
            }

            try
            {
                var protectedBytes = Convert.FromBase64String(EncryptedCredential);
                return Encoding.UTF8.GetString(ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser));
            }
            catch (CryptographicException)
            {
                return "";
            }
        }
        set => EncryptedCredential = string.IsNullOrEmpty(value)
            ? ""
            : Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser));
    }

    [JsonIgnore]
    public string DisplayName => Provider == IntegrationProvider.GitHub
        ? Repository
        : string.IsNullOrWhiteSpace(Project) ? Organization : $"{Organization}/{Project}";

    [JsonIgnore]
    public string OrganizationUrl
    {
        get
        {
            var value = Organization.Trim().TrimEnd('/');
            return value.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? value : $"https://dev.azure.com/{value}";
        }
    }
}
