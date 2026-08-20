using AutoVpn.Configuration;
using System.Security.Cryptography;
using System.Text.Json;
using System.Runtime.Versioning;

namespace AutoVpn.Security;

[SupportedOSPlatform("windows")]
public sealed class ProfileStore(string? path = null)
{
    public string Path { get; } = path ?? System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "autovpn", "profile.dat");

    public Profile? Load()
    {
        if (!File.Exists(Path))
        {
            return null;
        }

        try
        {
            byte[] encrypted = File.ReadAllBytes(Path);
            byte[] json = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return JsonSerializer.Deserialize(json, ProfileJsonContext.Default.Profile)
                ?? throw new InvalidDataException("The encrypted profile is invalid.");
        }
        catch (CryptographicException ex)
        {
            throw new InvalidDataException("The encrypted profile could not be decrypted.", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("The encrypted profile is invalid.", ex);
        }
    }

    public void Save(Profile profile)
    {
        string directory = System.IO.Path.GetDirectoryName(Path)!;
        Directory.CreateDirectory(directory);
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(profile, ProfileJsonContext.Default.Profile);
        byte[] encrypted = ProtectedData.Protect(json, null, DataProtectionScope.CurrentUser);
        string temporary = $"{Path}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllBytes(temporary, encrypted);
            File.Move(temporary, Path, true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }
}
