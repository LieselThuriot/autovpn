using System.Text.RegularExpressions;

namespace AutoVpn.AnyConnect;

public static partial class VpnCliRegexes
{
    [GeneratedRegex(@"(?im)^\s*(?:>>\s*)?(?:vpn\s+)?state\s*:\s*(connected|disconnected|failed)\s*$", RegexOptions.IgnoreCase)]
    public static partial Regex StateRegex();
    [GeneratedRegex(@"vpn connection is established|successfully connected", RegexOptions.IgnoreCase)]
    public static partial Regex EstablishedRegex();
    [GeneratedRegex(@"\bnot connected\b|\bfailed\b|\berror\s*:", RegexOptions.IgnoreCase)]
    public static partial Regex NotConnectedRegex();
    [GeneratedRegex(@"VPN>\s*disconnect\b[\s\S]*?(?:state\s*:\s*disconnected|notice\s*:\s*ready to connect)", RegexOptions.IgnoreCase, "en-BE")]
    public static partial Regex VpnDisconnectedRegex();
    [GeneratedRegex(@"authentication failed|failed to connect|connection failed|state\s*:\s*(?:disconnected|failed)|error\s*:", RegexOptions.IgnoreCase, "en-BE")]
    public static partial Regex IsFailureOutputRegex();
    [GeneratedRegex(@"^state\s*:\s*disconnected$", RegexOptions.IgnoreCase, "en-BE")]
    public static partial Regex FailureMessageRegex();
}
