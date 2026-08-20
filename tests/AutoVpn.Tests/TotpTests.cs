using Xunit;

using TotpGenerator = AutoVpn.Security.Totp;

namespace AutoVpn.Tests;

public class TotpTests
{
    [Theory]
    [InlineData("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ", 59, "94287082")]
    [InlineData("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ", 1111111109, "07081804")]
    public void GeneratesRfc6238Code(string secret, long unixTime, string expected)
    {
        string actual = TotpGenerator.Generate(secret, DateTimeOffset.FromUnixTimeSeconds(unixTime));
        Assert.Equal(expected[^6..], actual);
    }

    [Fact]
    public void RejectsInvalidSecret() => Assert.Throws<FormatException>(() => TotpGenerator.DecodeBase32("not valid!"));

    [Theory]
    [InlineData("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ=")]
    [InlineData("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ==")]
    public void AcceptsBase32Padding(string secret)
    {
        Assert.NotEmpty(TotpGenerator.DecodeBase32(secret));
    }

    [Fact]
    public void RejectsNonZeroTrailingBits() =>
        Assert.Throws<FormatException>(() => TotpGenerator.DecodeBase32("AB"));
}
