<div align="center">

# autovpn

**A secure Windows CLI for connecting to Cisco AnyConnect VPNs with TOTP support.**

[![.NET 10](https://img.shields.io/badge/.NET-10-512bd4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Windows](https://img.shields.io/badge/platform-Windows-0078d4?style=flat-square&logo=windows&logoColor=white)](https://learn.microsoft.com/windows/)
[![Binary](https://img.shields.io/badge/binary-Windows-004880?style=flat-square&logo=windows&logoColor=white)](https://learn.microsoft.com/windows/)

[Features](#features) • [Installation](#installation) • [Configuration](#configuration) • [Usage](#usage) • [Development](#development)

</div>

autovpn automates the interactive login flow of Cisco AnyConnect's `vpncli.exe`. It keeps your VPN profile encrypted for your Windows user, supplies the configured group and credentials when prompted, and generates a fresh one-time password when AnyConnect requests a second password.

> [!IMPORTANT]
> autovpn does not include Cisco AnyConnect. Install AnyConnect separately and make sure `vpncli.exe` is available on the machine.

## Features

- Connect and disconnect through Cisco AnyConnect's command-line client.
- Show the current VPN connection status.
- Handle AnyConnect prompts for group, username, password, and second password.
- Generate six-digit TOTP codes from a Base32 secret using RFC 6238-compatible timing.
- Encrypt the complete profile with Windows DPAPI scoped to the current user.
- Run as a standalone Windows executable with no .NET runtime required.

## Prerequisites

- Windows with Cisco Secure Client / AnyConnect installed.
- The path to AnyConnect's `vpncli.exe`.
- The [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) only for building from source.
- A Base32 TOTP secret for VPN profiles that require a second password.

## Installation

Install the .NET global tool on Windows:

```powershell
dotnet tool install --global autovpn
```

The tool requires the .NET 10 runtime. For a standalone executable with no
runtime dependency, build from source:

```powershell
.\scripts\publish.ps1
```

The published executable is standalone and does not require the .NET runtime.

The executable is written to `artifacts\publish\win-x64\autovpn.exe`. Add that directory to `PATH`, or copy the executable to a directory already on `PATH`. Use `-Runtime win-arm64` for Windows ARM64.

Check that the CLI is available:

```powershell
artifacts\publish\win-x64\autovpn.exe --version
```

## Configuration

Run the wizard to enter all profile values. Password and TOTP prompts do not echo input:

```powershell
autovpn configure
```

The wizard asks for:

| Property | Description |
| --- | --- |
| `vpn-cli-path` | Full path to `vpncli.exe` |
| `server` | VPN server hostname or URL |
| `group` | AnyConnect connection profile / group |
| `username` | VPN username |
| `password` | VPN password |
| `totp-secret` | Base32 TOTP secret |

You can update one non-secret property at a time:

```powershell
autovpn configure server vpn.example.com
autovpn configure group Employees
autovpn configure username alice
autovpn configure vpn-cli-path 'C:\Program Files\Cisco Systems\VPN\vpncli.exe'
```

Secret values are always requested interactively:

```powershell
autovpn configure password
autovpn configure totp-secret
```

The encrypted profile is stored at:

```text
%LOCALAPPDATA%\autovpn\profile.dat
```

> [!NOTE]
> The profile is protected with Windows DPAPI using `DataProtectionScope.CurrentUser`. It can be decrypted by the same Windows user on that machine, but it is not designed to be portable between user accounts or computers.

## Usage

Once configured, use the commands below:

```powershell
# Connect to the configured VPN
autovpn connect

# Show whether the VPN is connected
autovpn status

# Disconnect and verify the disconnected state
autovpn disconnect
```

Get command-specific help with `--help`:

```powershell
autovpn --help
autovpn configure --help
```

`connect` and `disconnect` return exit code `0` on success and `1` when AnyConnect does not reach the requested state. Invalid commands or configuration return exit code `2`.

## How It Works

1. autovpn loads and decrypts the current user's profile.
2. It starts `vpncli.exe -s` with redirected input and output.
3. It sends the configured server, group, username, and password as prompts appear.
4. When AnyConnect displays `Second Password:`, it generates and sends the current TOTP code.
5. It reports the final connection state and preserves AnyConnect output when a connection fails.

Connections time out after two minutes. Status checks time out after five seconds, while disconnect verification polls for up to 30 seconds.

## Development

Build the solution:

```powershell
dotnet build autovpn.slnx
```

Run the test suite:

```powershell
dotnet test autovpn.slnx
```

Publish a standalone NativeAOT executable:

```powershell
.\scripts\publish.ps1
```

The publish script accepts `-Runtime win-arm64` and `-Configuration Debug`.

The test project covers CLI dispatch, profile validation, TOTP generation and validation, plus AnyConnect connection-state parsing. Live VPN operations require a configured AnyConnect installation and are not part of the automated tests.

## Troubleshooting

**`vpncli.exe was not found`**

Run `autovpn configure vpn-cli-path` and provide the full path to the executable installed with Cisco Secure Client / AnyConnect.

**`No profile found`**

Create a profile with `autovpn configure` before running `connect`, `disconnect`, or `status`.

**Invalid TOTP secret**

The secret must be a valid Base32 value using `A-Z` and `2-7`. Spaces, hyphens, and trailing `=` padding are accepted.

**Authentication or connection failure**

Check the configured server, group, username, password, TOTP secret, and AnyConnect installation. autovpn prints relevant AnyConnect output to help diagnose failed connections.

## Project Layout

```text
src/autovpn/             CLI, profile storage, TOTP, and AnyConnect integration
tests/autovpn.Tests/     Unit tests for TOTP and VPN output parsing
scripts/publish.ps1      NativeAOT publish helper
artifacts/publish/        Standalone executables
```

## License

autovpn is licensed under the [MIT License](LICENSE).
