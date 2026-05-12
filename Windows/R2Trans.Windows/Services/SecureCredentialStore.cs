using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace R2Trans.Windows.Services;

public sealed class SecureCredentialStore
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("R2Trans.OpenAI.APIKey");
    private readonly string keyPath;

    public SecureCredentialStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var settingsDirectory = Path.Combine(appData, "R2Trans");
        Directory.CreateDirectory(settingsDirectory);
        keyPath = Path.Combine(settingsDirectory, "openai-api-key.dat");
    }

    public string LoadApiKey()
    {
        if (!File.Exists(keyPath))
        {
            return string.Empty;
        }

        try
        {
            var encrypted = File.ReadAllBytes(keyPath);
            var data = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(data);
        }
        catch
        {
            return string.Empty;
        }
    }

    public void SaveApiKey(string apiKey)
    {
        var data = Encoding.UTF8.GetBytes(apiKey.Trim());
        var encrypted = ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(keyPath, encrypted);
    }
}
