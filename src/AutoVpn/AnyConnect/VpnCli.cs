using AutoVpn.Configuration;
using AutoVpn.Security;
using System.Diagnostics;
using System.Text;
using System.Threading.Channels;
using static AutoVpn.AnyConnect.VpnCliRegexes;

namespace AutoVpn.AnyConnect;

public sealed record VpnResult(bool Success, string Message, string Output);

public static class VpnCli
{
    public static async Task<VpnResult> ConnectAsync(Profile profile, CancellationToken cancellationToken)
    {
        if (!File.Exists(profile.VpnCliPath))
        {
            return new(false, $"vpncli.exe was not found: {profile.VpnCliPath}", "");
        }

        Totp.DecodeBase32(profile.TotpSecret);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));

        var transcript = new StringBuilder();

        using var process = Start(profile.VpnCliPath);

        var stream = Channel.CreateUnbounded<string>();
        var stdoutTask = ConsumeAsync(process.StandardOutput, stream.Writer, timeout.Token);
        var stderrTask = ConsumeAsync(process.StandardError, stream.Writer, timeout.Token);
        _ = CompleteWhenBothFinishAsync(stdoutTask, stderrTask, stream.Writer);

        WriteProgress("Starting vpncli");

        await process.StandardInput.WriteLineAsync($"connect {profile.Server}");
        await process.StandardInput.FlushAsync(cancellationToken);

        WriteProgress("Connect command sent");

        bool sentGroup = false;
        bool sentUser = false;
        bool sentPassword = false;
        bool sentOtp = false;
        int promptPosition = 0;

        try
        {
            while (await stream.Reader.WaitToReadAsync(timeout.Token))
            {
                while (stream.Reader.TryRead(out string? chunk))
                {
                    transcript.Append(chunk);
                    string output = transcript.ToString();
                    if (!sentGroup && TryFindPrompt(output, "Group:", ref promptPosition))
                    {
                        WriteProgress("Group prompt detected");
                        await Send(process, profile.Group);
                        sentGroup = true;
                    }

                    if (sentGroup && !sentUser && TryFindPrompt(output, "Username:", ref promptPosition))
                    {
                        WriteProgress("Username prompt detected");
                        await Send(process, profile.Username);
                        sentUser = true;
                    }

                    if (sentUser && !sentPassword && TryFindPrompt(output, "Password:", ref promptPosition))
                    {
                        WriteProgress("Password prompt detected");
                        await Send(process, profile.Password);
                        sentPassword = true;
                    }

                    if (sentPassword && !sentOtp && TryFindPrompt(output, "Second Password:", ref promptPosition))
                    {
                        WriteProgress("Second password prompt detected");
                        await Send(process, Totp.Generate(profile.TotpSecret));
                        sentOtp = true;
                    }

                    if (VpnOutputParser.IsConnected(output))
                    {
                        return new(true, "VPN connected.", output);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            transcript.AppendLine("Connection timed out after 2 minutes.");
        }
        finally
        {
            if (!process.HasExited)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                }
            }

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch
            {
            }
        }

        try
        {
            await Task.WhenAll(stdoutTask, stderrTask);
        }
        catch (OperationCanceledException)
        {
        }

        string text = transcript.ToString();
        bool connected = VpnOutputParser.IsConnected(text);

        return new(connected, connected ? "VPN connected." : VpnOutputParser.FailureMessage(text), text);
    }

    public static async Task<VpnResult> StatusAsync(Profile profile, CancellationToken cancellationToken)
    {
        if (!File.Exists(profile.VpnCliPath))
        {
            return new(false, $"vpncli.exe was not found: {profile.VpnCliPath}", "");
        }

        string output = await RunInteractiveCommandAsync(profile.VpnCliPath, "state", TimeSpan.FromSeconds(5), cancellationToken);
        bool connected = VpnOutputParser.IsConnected(output);

        return new(connected, connected ? "VPN connected." : "VPN disconnected.", output);
    }

    public static async Task<VpnResult> DisconnectAsync(Profile profile, CancellationToken cancellationToken)
    {
        if (!File.Exists(profile.VpnCliPath))
        {
            return new(false, $"vpncli.exe was not found: {profile.VpnCliPath}", "");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));

        var transcript = new StringBuilder();

        using var process = Start(profile.VpnCliPath);

        var stream = Channel.CreateUnbounded<string>();
        var stdoutTask = ConsumeAsync(process.StandardOutput, stream.Writer, timeout.Token);
        var stderrTask = ConsumeAsync(process.StandardError, stream.Writer, timeout.Token);

        _ = CompleteWhenBothFinishAsync(stdoutTask, stderrTask, stream.Writer);

        WriteProgress("Starting vpncli");

        await process.StandardInput.WriteLineAsync("disconnect");
        await process.StandardInput.FlushAsync(cancellationToken);

        WriteProgress("Disconnect command sent");

        // AnyConnect keeps the interactive process alive while the GUI performs
        // the asynchronous disconnect. Verify the subsystem state independently.
        WriteProgress("Verifying VPN state");

        var finalState = await WaitForDisconnectedAsync(profile, cancellationToken);

        try
        {
            if (!finalState.Success)
            {
                transcript.AppendLine("Disconnect verification timed out.");
            }
        }
        finally
        {
            if (!process.HasExited)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                }
            }

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch
            {
            }
        }

        try
        {
            while (stream.Reader.TryRead(out string? chunk))
            {
                transcript.Append(chunk);
            }

            await Task.WhenAll(stdoutTask, stderrTask);
            while (stream.Reader.TryRead(out string? chunk))
            {
                transcript.Append(chunk);
            }
        }
        catch (OperationCanceledException)
        {
        }

        string text = transcript.ToString();

        if (finalState.Success)
        {
            return new(true, "VPN disconnected.", $"{text}{Environment.NewLine}{finalState.Output}");
        }

        return new(false, "VPN did not reach the disconnected state.", $"{text}{Environment.NewLine}{finalState.Output}");
    }

    public static bool IsConnectedOutput(string output)
        => VpnOutputParser.IsConnected(output);

    public static bool IsDisconnectedOutput(string output)
        => VpnOutputParser.IsDisconnected(output);

    public static bool IsDisconnectCompletedOutput(string output)
        => VpnOutputParser.IsDisconnectCompleted(output);

    private static async Task<VpnResult> WaitForDisconnectedAsync(Profile profile, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));

        string lastOutput = "";

        while (!timeout.IsCancellationRequested)
        {
            var result = await StatusAsync(profile, timeout.Token);
            lastOutput = result.Output;
            if (VpnOutputParser.IsDisconnected(result.Output))
            {
                WriteProgress("VPN state: Disconnected");
                return new(true, "VPN disconnected.", result.Output);
            }

            WriteProgress(result.Success ? "VPN state: Connected" : "VPN state: Disconnected or unavailable");
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), timeout.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        return new(false, "VPN is still connected.", lastOutput);
    }

    private static async Task<string> RunInteractiveCommandAsync(string path, string command, TimeSpan timeoutDuration, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(timeoutDuration);

        using var process = Start(path);

        var output = new StringBuilder();

        var stream = Channel.CreateUnbounded<string>();
        var stdoutTask = ConsumeAsync(process.StandardOutput, stream.Writer, timeout.Token);
        var stderrTask = ConsumeAsync(process.StandardError, stream.Writer, timeout.Token);

        _ = CompleteWhenBothFinishAsync(stdoutTask, stderrTask, stream.Writer);

        await process.StandardInput.WriteLineAsync(command);
        await process.StandardInput.FlushAsync(cancellationToken);

        process.StandardInput.Close();

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            output.AppendLine($"The '{command}' command timed out.");
        }
        finally
        {
            if (!process.HasExited)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                }
            }

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch
            {
            }
        }

        try
        {
            while (stream.Reader.TryRead(out string? chunk))
            {
                output.Append(chunk);
            }

            await Task.WhenAll(stdoutTask, stderrTask);
            while (stream.Reader.TryRead(out string? chunk))
            {
                output.Append(chunk);
            }
        }
        catch (OperationCanceledException)
        {
        }

        return output.ToString();
    }

    private static Process Start(string path) => new Process
    {
        StartInfo = new ProcessStartInfo(path, "-s")
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        }
    }.AlsoStart();

    private static async Task Send(Process process, string value)
    {
        await process.StandardInput.WriteLineAsync(value);
        await process.StandardInput.FlushAsync();
    }

    private static bool TryFindPrompt(string output, string prompt, ref int searchPosition)
    {
        int index = output.IndexOf(prompt, searchPosition, StringComparison.OrdinalIgnoreCase);

        if (index < 0)
        {
            return false;
        }

        searchPosition = index + prompt.Length;
        return true;
    }

    private static async Task ConsumeAsync(StreamReader reader, ChannelWriter<string> writer, CancellationToken cancellationToken)
    {
        char[] buffer = new char[256];
        try
        {
            while (true)
            {
                int count = await reader.ReadAsync(buffer, cancellationToken);
                if (count == 0)
                {
                    break;
                }

                await writer.WriteAsync(new string(buffer, 0, count), cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private static async Task CompleteWhenBothFinishAsync(Task first, Task second, ChannelWriter<string> writer)
    {
        try
        {
            await Task.WhenAll(first, second);
        }
        finally
        {
            writer.TryComplete();
        }
    }

    private static void WriteProgress(string message)
    {
        if (!Console.IsOutputRedirected)
        {
            Console.Error.WriteLine($"[autovpn] {message}");
        }
    }
}

file static class ProcessExtensions
{
    public static Process AlsoStart(this Process process)
    {
        process.Start();
        return process;
    }
}
