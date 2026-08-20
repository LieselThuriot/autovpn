using AutoVpn.AnyConnect;
using AutoVpn.Configuration;
using AutoVpn.ConsoleApp;
using AutoVpn.Security;

namespace AutoVpn;

public sealed class Cli(IConsole console, ProfileStore profiles)
{
    private const string Version = "1.0.0";
    private readonly IConsole _console = console;
    private readonly ProfileStore _profiles = profiles;

    public async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            PrintHelp(args.FirstOrDefault());
            return 0;
        }

        if (args[0] is "--version" or "-v" or "version")
        {
            _console.WriteLine(Version);
            return 0;
        }

        try
        {
            return args[0] switch
            {
                "configure" => Configure(args[1..]),
                "connect" => await RunVpnAsync(VpnCommand.Connect),
                "disconnect" => await RunVpnAsync(VpnCommand.Disconnect),
                "status" => await RunVpnAsync(VpnCommand.Status),
                _ => Error($"Unknown command '{args[0]}'. Use --help for usage.")
            };
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or InvalidDataException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return Error(ex.Message);
        }
    }

    private int Configure(string[] args)
    {
        if (args.Length > 2)
        {
            return Error("configure accepts one property and optional value. Use --help for usage.");
        }

        Profile profile = _profiles.Load() ?? new Profile();
        if (args.Length == 0)
        {
            foreach (ProfileProperty property in ProfileEditor.AllProperties)
            {
                profile = Set(profile, property, null);
            }
        }
        else if (!ProfileEditor.TryParse(args[0], out ProfileProperty property))
        {
            throw new ArgumentException($"Unknown property '{args[0]}'.");
        }
        else
        {
            profile = Set(profile, property, args.Length == 2 ? args[1] : null);
        }

        ProfileValidator.Validate(profile, false);
        _profiles.Save(profile);
        _console.WriteLine($"Configuration saved to {_profiles.Path}");
        return 0;
    }

    private Profile Set(Profile profile, ProfileProperty property, string? supplied)
    {
        string value;
        if (property is ProfileProperty.Password or ProfileProperty.TotpSecret)
        {
            value = _console.ReadSecret(property == ProfileProperty.Password ? "Password" : "TOTP secret");
            if (property == ProfileProperty.Password && value != _console.ReadSecret("Confirm password"))
            {
                throw new InvalidOperationException("Passwords do not match.");
            }
        }
        else if (supplied is not null)
        {
            value = supplied;
        }
        else
        {
            string name = ProfileEditor.Name(property);
            _console.Write($"{name} [{ProfileEditor.Get(profile, property)}]: ");
            value = _console.ReadLine() ?? "";
            if (value.Length == 0)
            {
                value = ProfileEditor.Get(profile, property);
            }
        }

        return ProfileEditor.Set(profile, property, value);
    }

    private async Task<int> RunVpnAsync(VpnCommand command)
    {
        Profile profile = _profiles.Load() ?? throw new InvalidDataException("No profile found. Run 'autovpn configure'.");
        ProfileValidator.Validate(profile, true);
        VpnResult result = command switch
        {
            VpnCommand.Connect => await VpnCli.ConnectAsync(profile, CancellationToken.None),
            VpnCommand.Disconnect => await VpnCli.DisconnectAsync(profile, CancellationToken.None),
            _ => await VpnCli.StatusAsync(profile, CancellationToken.None)
        };

        _console.WriteLine(result.Message);
        if (!result.Success && !string.IsNullOrWhiteSpace(result.Output) && command != VpnCommand.Status)
        {
            _console.WriteError("AnyConnect output:");
            _console.WriteError(result.Output);
        }

        return result.Success ? 0 : 1;
    }

    private int Error(string message)
    {
        _console.WriteError($"Error: {message}");
        return 2;
    }

    private void PrintHelp(string? command)
    {
        if (command == "configure")
        {
            _console.WriteLine("Usage: autovpn configure [vpn-cli-path|server|group|username|password|totp-secret]\nConfigure all values or one property. Password and TOTP are prompted securely.");
        }
        else if (command == "connect")
        {
            _console.WriteLine("Usage: autovpn connect\nConnect using Cisco AnyConnect.");
        }
        else if (command == "disconnect")
        {
            _console.WriteLine("Usage: autovpn disconnect\nDisconnect Cisco AnyConnect.");
        }
        else if (command == "status")
        {
            _console.WriteLine("Usage: autovpn status\nShow the current VPN status.");
        }
        else
        {
            _console.WriteLine("Usage: autovpn <command>\n\nCommands:\n  configure  Create or update encrypted profile\n  connect    Connect using Cisco AnyConnect\n  disconnect Disconnect Cisco AnyConnect\n  status     Show VPN status\n  version    Show version\n\nOptions:\n  -h, --help     Show help\n  -v, --version  Show version");
        }
    }

    private enum VpnCommand { Connect, Disconnect, Status }
}
