using AutoVpn.ConsoleApp;
using AutoVpn.Security;
using Xunit;

namespace AutoVpn.Tests;

public sealed class CliTests
{
    [Fact]
    public async Task VersionWritesVersionAndSucceeds()
    {
        var console = new TestConsole();
        var cli = new AutoVpn.Cli(console, new ProfileStore(Path.Combine(Path.GetTempPath(), "autovpn-tests", Guid.NewGuid().ToString(), "profile.dat")));

        int result = await cli.RunAsync(["--version"]);

        Assert.Equal(0, result);
        Assert.Equal("1.0.0", console.Output.Single());
    }

    private sealed class TestConsole : IConsole
    {
        public bool IsOutputRedirected => true;
        public List<string> Output { get; } = [];
        public List<string> Errors { get; } = [];
        public string? ReadLine() => "";
        public string ReadSecret(string label) => "";
        public void Write(string value) { }
        public void WriteLine(string value = "") => Output.Add(value);
        public void WriteError(string value) => Errors.Add(value);
    }
}
