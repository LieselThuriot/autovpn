namespace AutoVpn.ConsoleApp;

public interface IConsole
{
    public bool IsOutputRedirected { get; }
    public string? ReadLine();
    public string ReadSecret(string label);
    public void Write(string value);
    public void WriteLine(string value = "");
    public void WriteError(string value);
}
