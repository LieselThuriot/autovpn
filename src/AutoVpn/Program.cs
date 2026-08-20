using AutoVpn;

return await new Cli(new AutoVpn.ConsoleApp.SystemConsole(), new AutoVpn.Security.ProfileStore()).RunAsync(args);
