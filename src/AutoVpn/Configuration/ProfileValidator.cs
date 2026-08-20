using AutoVpn.Security;

namespace AutoVpn.Configuration;

public static class ProfileValidator
{
    public static void Validate(Profile profile, bool requireExecutable)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (string.IsNullOrWhiteSpace(profile.VpnCliPath) ||
            string.IsNullOrWhiteSpace(profile.Server) ||
            string.IsNullOrWhiteSpace(profile.Group) ||
            string.IsNullOrWhiteSpace(profile.Username) ||
            string.IsNullOrWhiteSpace(profile.Password) ||
            string.IsNullOrWhiteSpace(profile.TotpSecret))
        {
            throw new InvalidDataException("All configuration properties are required.");
        }

        Totp.DecodeBase32(profile.TotpSecret);

        if (requireExecutable && !File.Exists(profile.VpnCliPath))
        {
            throw new FileNotFoundException("vpncli.exe was not found.", profile.VpnCliPath);
        }
    }
}
