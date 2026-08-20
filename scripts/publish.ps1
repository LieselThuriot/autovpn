[CmdletBinding()]
param(
    [ValidateSet('win-x64', 'win-arm64')]
    [string] $Runtime = 'win-x64',
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'src\AutoVpn\AutoVpn.csproj'
$outputDirectory = Join-Path $repoRoot "artifacts\publish\$Runtime"
$packageDirectory = Join-Path $repoRoot 'artifacts\nuget'

if (-not (Test-Path -LiteralPath $project)) {
    throw "Project not found: $project"
}

dotnet publish $project `
    --configuration $Configuration `
    --runtime $Runtime `
    -p:TargetFramework=net10.0-windows `
    --self-contained true `
    --output $outputDirectory `
    -p:PublishAot=true `
    -p:PublishSingleFile=true `
    -p:StripSymbols=true

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Published standalone executable: $(Join-Path $outputDirectory 'autovpn.exe')"

dotnet pack $project `
    --configuration $Configuration `
    --output $packageDirectory `
    -p:TargetFrameworks=net10.0 `
    -p:TargetFramework=net10.0

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Created NuGet package in: $packageDirectory"
