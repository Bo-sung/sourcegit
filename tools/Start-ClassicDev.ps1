param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
    [string]$Repository,
    [string]$SdkRoot = "$env:LOCALAPPDATA/Microsoft/dotnet10",
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent

# Keep this run's git away from the machine's own configuration files: the build asks git for a version
# stamp and the launched client runs git of its own, and neither may read or write a user's config.
$gitEnvRoot = Join-Path $env:TEMP 'sourcegit-tests\git-env'
New-Item -ItemType Directory -Path $gitEnvRoot -Force | Out-Null
foreach ($name in 'global.gitconfig', 'system.gitconfig') {
    $f = Join-Path $gitEnvRoot $name
    if (!(Test-Path -LiteralPath $f)) { Set-Content -LiteralPath $f -Value '' -NoNewline }
}
$env:GIT_CONFIG_GLOBAL = Join-Path $gitEnvRoot 'global.gitconfig'
$env:GIT_CONFIG_SYSTEM = Join-Path $gitEnvRoot 'system.gitconfig'
$env:GIT_CONFIG_NOSYSTEM = '1'
$dotnetExe = Join-Path $SdkRoot 'dotnet.exe'
if (!(Test-Path -LiteralPath $dotnetExe)) { throw "SDK executable missing: $dotnetExe" }
$env:DOTNET_ROOT = $SdkRoot
$env:PATH = "$SdkRoot;$env:PATH"
Push-Location $projectRoot
try {
    if (!$NoBuild) {
        & $dotnetExe build src/SourceGit.csproj -c $Configuration
        if ($LASTEXITCODE -ne 0) { throw "Build failed: $LASTEXITCODE" }
    }
    $executable = Join-Path $projectRoot "src/bin/$Configuration/net10.0/SourceGit.exe"
    if (!(Test-Path -LiteralPath $executable)) { throw "Application missing: $executable" }
    New-Item -ItemType Directory -Path (Join-Path (Split-Path $executable) 'data') -Force | Out-Null
    # Use the apphost so portable data resolves next to SourceGit.exe, never dotnet.exe.
    if ($Repository) { & $executable $Repository } else { & $executable }
} finally {
    Pop-Location
}
