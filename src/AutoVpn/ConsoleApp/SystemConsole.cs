namespace AutoVpn.ConsoleApp;

public sealed class SystemConsole : IConsole
{
    public bool IsOutputRedirected => Console.IsOutputRedirected;
    public string? ReadLine() => Console.ReadLine();
    public void Write(string value) => Console.Write(value);
    public void WriteLine(string value = "") => Console.WriteLine(value);
    public void WriteError(string value) => Console.Error.WriteLine(value);

    public string ReadSecret(string label)
    {
        Write($"{label}: ");
        var value = new List<char>();
        ConsoleKeyInfo key;
        while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
        {
            if (key.Key == ConsoleKey.Backspace && value.Count > 0)
            {
                value.RemoveAt(value.Count - 1);
            }
            else if (!char.IsControl(key.KeyChar))
            {
                value.Add(key.KeyChar);
            }
        }

        WriteLine();
        return new string([.. value]);
    }
}
