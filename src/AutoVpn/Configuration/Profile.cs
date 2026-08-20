namespace AutoVpn.Configuration;

public sealed record Profile
{
    public string VpnCliPath { get; init; } = "";
    public string Server { get; init; } = "";
    public string Group { get; init; } = "";
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public string TotpSecret { get; init; } = "";
}
