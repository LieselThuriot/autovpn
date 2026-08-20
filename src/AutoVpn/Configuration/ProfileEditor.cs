namespace AutoVpn.Configuration;

public enum ProfileProperty
{
    VpnCliPath,
    Server,
    Group,
    Username,
    Password,
    TotpSecret
}

public static class ProfileEditor
{
    public static readonly ProfileProperty[] AllProperties =
    [
        ProfileProperty.VpnCliPath,
        ProfileProperty.Server,
        ProfileProperty.Group,
        ProfileProperty.Username,
        ProfileProperty.Password,
        ProfileProperty.TotpSecret
    ];

    public static bool TryParse(string value, out ProfileProperty property) =>
        value.ToLowerInvariant() switch
        {
            "vpn-cli-path" => Set(ProfileProperty.VpnCliPath, out property),
            "server" => Set(ProfileProperty.Server, out property),
            "group" => Set(ProfileProperty.Group, out property),
            "username" => Set(ProfileProperty.Username, out property),
            "password" => Set(ProfileProperty.Password, out property),
            "totp-secret" => Set(ProfileProperty.TotpSecret, out property),
            _ => Set(default, out property, false)
        };

    public static string Name(ProfileProperty property) => property switch
    {
        ProfileProperty.VpnCliPath => "vpn-cli-path",
        ProfileProperty.Server => "server",
        ProfileProperty.Group => "group",
        ProfileProperty.Username => "username",
        ProfileProperty.Password => "password",
        ProfileProperty.TotpSecret => "totp-secret",
        _ => throw new ArgumentOutOfRangeException(nameof(property))
    };

    public static string Get(Profile profile, ProfileProperty property) => property switch
    {
        ProfileProperty.VpnCliPath => profile.VpnCliPath,
        ProfileProperty.Server => profile.Server,
        ProfileProperty.Group => profile.Group,
        ProfileProperty.Username => profile.Username,
        ProfileProperty.Password => profile.Password,
        ProfileProperty.TotpSecret => profile.TotpSecret,
        _ => throw new ArgumentOutOfRangeException(nameof(property))
    };

    public static Profile Set(Profile profile, ProfileProperty property, string value) => property switch
    {
        ProfileProperty.VpnCliPath => profile with { VpnCliPath = value },
        ProfileProperty.Server => profile with { Server = value },
        ProfileProperty.Group => profile with { Group = value },
        ProfileProperty.Username => profile with { Username = value },
        ProfileProperty.Password => profile with { Password = value },
        ProfileProperty.TotpSecret => profile with { TotpSecret = value },
        _ => throw new ArgumentOutOfRangeException(nameof(property))
    };

    private static bool Set(ProfileProperty value, out ProfileProperty property, bool success = true)
    {
        property = value;
        return success;
    }
}
