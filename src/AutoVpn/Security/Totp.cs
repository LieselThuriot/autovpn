using System.Security.Cryptography;

namespace AutoVpn.Security;

public static class Totp
{
    public static string Generate(string secret, DateTimeOffset? timestamp = null)
    {
        byte[] key = DecodeBase32(secret);
        byte[] counter = BitConverter.GetBytes((timestamp ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds() / 30);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counter);
        }

        byte[] hash = HMACSHA1.HashData(key, counter);
        int offset = hash[^1] & 0x0f;
        int value = ((hash[offset] & 0x7f) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (value % 1_000_000).ToString("D6");
    }

    public static byte[] DecodeBase32(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        string normalized = input.Trim().Replace(" ", "").Replace("-", "").ToUpperInvariant();
        int padding = normalized.IndexOf('=');
        if (padding >= 0 && normalized[padding..].TrimEnd('=') is not "")
        {
            throw new FormatException("The TOTP secret must be a valid Base32 value.");
        }

        normalized = normalized.TrimEnd('=');
        if (normalized.Length == 0 || normalized.Any(c => !(c is >= 'A' and <= 'Z') && !(c is >= '2' and <= '7')))
        {
            throw new FormatException("The TOTP secret must be a valid Base32 value.");
        }

        var result = new List<byte>();
        int buffer = 0;
        int bits = 0;
        foreach (char c in normalized)
        {
            int value = c is >= 'A' and <= 'Z' ? c - 'A' : c - '2' + 26;
            if (value > 31)
            {
                throw new FormatException("The TOTP secret must be a valid Base32 value.");
            }

            buffer = (buffer << 5) | value;
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                result.Add((byte)(buffer >> bits));
            }
        }

        if (result.Count == 0 || bits >= 5 || (bits > 0 && (buffer & ((1 << bits) - 1)) != 0))
        {
            throw new FormatException("The TOTP secret must be a valid Base32 value.");
        }

        return [.. result];
    }
}
