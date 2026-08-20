using static AutoVpn.AnyConnect.VpnCliRegexes;

namespace AutoVpn.AnyConnect;

public static class VpnOutputParser
{
    public static bool IsConnected(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return false;
        }

        var states = StateRegex().Matches(output);
        if (states.Count > 0)
        {
            return string.Equals(states[^1].Groups[1].Value, "connected", StringComparison.OrdinalIgnoreCase);
        }

        return EstablishedRegex().IsMatch(output) && !NotConnectedRegex().IsMatch(output);
    }

    public static bool IsDisconnected(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return false;
        }

        var states = StateRegex().Matches(output);
        return states.Count > 0 && string.Equals(states[^1].Groups[1].Value, "disconnected", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsDisconnectCompleted(string output) =>
        !string.IsNullOrWhiteSpace(output) && VpnDisconnectedRegex().IsMatch(output);

    public static string FailureMessage(string output)
    {
        string? line = output.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim(' ', '\t', '>', '-'))
            .Where(x => !FailureMessageRegex().IsMatch(x))
            .FirstOrDefault(x => IsFailureOutputRegex().IsMatch(x));
        return line is null ? "AnyConnect did not report a successful connection." : $"AnyConnect connection failed: {line}";
    }
}
