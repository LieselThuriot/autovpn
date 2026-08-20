using AutoVpn.AnyConnect;

using Xunit;

namespace AutoVpn.Tests;

public class VpnCliTests
{
    [Theory]
    [InlineData("State: Disconnected")]
    [InlineData(">> state: Disconnected")]
    [InlineData("VPN state: Not Connected")]
    [InlineData("State: Disconnected\r\nThe VPN connection failed.")]
    public void DoesNotTreatDisconnectedOutputAsConnected(string output)
    {
        Assert.False(VpnOutputParser.IsConnected(output));
    }

    [Theory]
    [InlineData("State: Connected")]
    [InlineData(">> state: Connected")]
    [InlineData("VPN connection is established")]
    [InlineData("Successfully connected to the VPN")]
    public void RecognizesExplicitConnectedOutput(string output)
    {
        Assert.True(VpnOutputParser.IsConnected(output));
    }

    [Fact]
    public void RecognizesConnectedStateAfterInitialDisconnectedState()
    {
        const string output = """
            >> state: Disconnected
            >> notice: Ready to connect.
            VPN> connect vpn.example.com
            >> contacting host for login information...
            >> state: Connected
            """;

        Assert.True(VpnOutputParser.IsConnected(output));
    }

    [Theory]
    [InlineData(">> state: Disconnected")]
    [InlineData(">> state: Connected\n>> state: Disconnected")]
    public void RecognizesDisconnectedState(string output)
    {
        Assert.True(VpnOutputParser.IsDisconnected(output));
    }

    [Fact]
    public void UsesLatestStateWhenInteractiveStateOutputStartsConnected()
    {
        const string output = """
            >> state: Connected
            VPN> state
            >> state: Disconnected
            """;

        Assert.False(VpnOutputParser.IsConnected(output));
        Assert.True(VpnOutputParser.IsDisconnected(output));
    }

    [Fact]
    public void DisconnectedStatusIsSuccessfulForDisconnectVerification()
    {
        const string output = """
            >> state: Disconnected
            >> notice: Ready to connect.
            VPN> state
            >> state: Disconnected
            """;

        Assert.True(VpnOutputParser.IsDisconnected(output));
    }

    [Fact]
    public void RecognizesReadyStateAfterDisconnectCommand()
    {
        const string output = """
            VPN> disconnect
            >> notice: Disconnecting...
            >> notice: Ready to connect.
            """;

        Assert.True(VpnOutputParser.IsDisconnectCompleted(output));
    }
}
