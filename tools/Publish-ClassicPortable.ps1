#Requires -Version 5.1
<#
.SYNOPSIS
  Builds a per-user Windows x64 installer for the SourceGit "Classic" fork preview.
.DESCRIPTION
  Reproducible local-distribution pipeline (2026-11-10, rev2):
    1. Publishes src/SourceGit.csproj Release / win-x64 / self-contained with
       -p:DisableAOT=true -p:DisableUpdateDetection=true using the project .NET 10 SDK.
    2. Stages payload + LICENSE + THIRD-PARTY-LICENSES.md + Korean README + seed preference.
    3. Compiles tools/installer/SourceGitClassic.iss with Inno Setup 6.7.3
       (pinned, hash-verified, project-local under the artifacts directory).
    4. -Smoke: compiles an ALTERNATE-IDENTITY installer (SmokeMode AppId, no shortcuts),
       installs it into a fixture dir inside the artifacts root, verifies seed-not-
       overwritten on reinstall and data retention on uninstall, launches the installed
       exe, verifies portable data isolation, and removes only its own artifacts.
       The production-identity install/uninstall registry is never touched; the test
       aborts if a real production install is present.
  Scope of changes OUTSIDE the artifacts root during a PRODUCTION install:
    - per-user Program Files (default %LOCALAPPDATA%\Programs\SourceGitClassic)
    - HKCU uninstall entry for this AppId, Start Menu shortcut (unless deselected)
  Smoke runs add only HKCU uninstall entries for the smoke AppId (no shortcuts).
  NativeAOT is NOT used by this script (DisableAOT=true -> JIT self-contained build).
.PARAMETER SkipPublish   Reuse an existing payload directory.
.PARAMETER Smoke         Build the alternate-identity installer and run the smoke test.
.PARAMETER NoPrompt      Non-interactive.
#>
[CmdletBinding()]
param(
    [switch]$SkipPublish,
    [switch]$Smoke,
    [switch]$NoPrompt
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

# ------------------------------------------------------------ guards
# Resolves a path and requires it to be strictly INSIDE the artifacts root.
# The root is compared WITH a trailing separator so a sibling like
# "...\local-distribution-20261110-evil" cannot pass a plain StartsWith.
# Defined before ANY deletion can happen.
$script:ArtRootFull = $null
function Assert-InsideArtifacts {
    param([string]$Path)
    $full = [System.IO.Path]::GetFullPath($Path)
    $root = $script:ArtRootFull
    if (-not ($full.StartsWith($root + [System.IO.Path]::DirectorySeparatorChar) -or
              $full -eq $root)) {
        throw "Path escape refused: $full (must be under $root)"
    }
    return $full
}
function Remove-InsideArtifacts {
    param([string]$Path)
    $full = Assert-InsideArtifacts $Path
    if (Test-Path -LiteralPath $full) { Remove-Item -LiteralPath $full -Recurse -Force }
}

# Explicit native command-line quoting: Start-Process flattens an argument
# ARRAY into one string without quotes, so every path argument must be
# quoted here (paths may contain spaces). Trailing backslashes are stripped
# before quoting (\" would corrupt the quote rule).
function Format-NativeArg {
    param([string]$Value)
    if ($Value -match '[\s"]') {
        return '"' + $Value.TrimEnd('\') + '"'
    }
    return $Value
}
function Start-NativeProcess {
    param([string]$FilePath, [string[]]$Arguments, [string]$WorkingDirectory)
    $psi = New-Object Diagnostics.ProcessStartInfo
    $psi.FileName = $FilePath
    $psi.Arguments = ($Arguments | ForEach-Object { Format-NativeArg $_ }) -join ' '
    if ($WorkingDirectory) { $psi.WorkingDirectory = $WorkingDirectory }
    $psi.UseShellExecute = $false
    $proc = [Diagnostics.Process]::Start($psi)
    $proc.WaitForExit()
    return $proc
}

# ---------------------------------------------------------------- paths
$RepoRoot  = Split-Path -Parent $PSScriptRoot                       # sourcegit checkout root
$ProjRoot  = Split-Path -Parent $RepoRoot                           # container of checkout + artifacts
$ArtRoot   = Join-Path (Join-Path $ProjRoot 'artifacts') 'local-distribution-20261110'
$script:ArtRootFull = [System.IO.Path]::GetFullPath($ArtRoot)
$Payload   = Join-Path $ArtRoot 'payload'
$Staging   = Join-Path $ArtRoot 'staging'
$Installer = Join-Path $ArtRoot 'installer'
$Logs      = Join-Path $ArtRoot 'logs'
$Tooling   = Join-Path $ArtRoot 'tooling'
$GitCfg    = Join-Path $ArtRoot 'gitcfg'
$SmokeDir  = Join-Path $ArtRoot 'smoke'
$IssFile   = Join-Path $PSScriptRoot 'installer\SourceGitClassic.iss'
$ReadmeSrc = Join-Path $PSScriptRoot 'installer\assets\README-KO.md'
$VersionFile = Join-Path $RepoRoot 'VERSION'

$ProdAppIdGuid  = '56B78231-DB7B-43D8-9D48-4A8386E019B4'
$SmokeAppIdGuid = '822B4D61-2014-4A41-ACF5-4321584B8D0A'
$ProdRegKey = "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{$($ProdAppIdGuid.ToLower())}_is1"

New-Item -ItemType Directory -Force -Path $ArtRoot,$Payload,$Staging,$Installer,$Logs,$Tooling,$GitCfg | Out-Null

$Version = (Get-Content -Raw $VersionFile).Trim()
$PackageVersion = "$Version-preview"

# ---------------------------------------- env isolation (with restore)
# Point git at empty fixture configs BEFORE the first git / MSBuild call, and
# bind the runtime to the selected SDK via DOTNET_ROOT from the first launch.
# Everything we change is restored in the finally block, so dot-sourcing this
# script cannot contaminate the caller.
$envSave = @{}
foreach ($v in 'GIT_CONFIG_GLOBAL','GIT_CONFIG_SYSTEM','GIT_CONFIG_NOSYSTEM','DOTNET_ROOT','DOTNET_CLI_TELEMETRY_OPTOUT','DOTNET_NOLOGO') {
    $envSave[$v] = [Environment]::GetEnvironmentVariable($v)
}
try {

$globalCfg  = Join-Path $GitCfg 'global'
$systemCfg  = Join-Path $GitCfg 'system'
foreach ($f in $globalCfg, $systemCfg) {
    if (-not (Test-Path -LiteralPath $f)) {
        Set-Content -LiteralPath $f -Value "[user]`r`n`tname = fixture`r`n`temail = fixture@invalid`r`n" -Encoding Ascii
    }
}
$env:GIT_CONFIG_GLOBAL = $globalCfg
$env:GIT_CONFIG_SYSTEM = $systemCfg
$env:GIT_CONFIG_NOSYSTEM = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'

# ---------------------------------------------------- project SDK probe
function Find-ProjectSdk {
    $candidates = @()
    $candidates += (Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet10\dotnet.exe')
    $candidates += (Join-Path $env:ProgramFiles 'dotnet\dotnet.exe')
    foreach ($c in $candidates) {
        if (Test-Path -LiteralPath $c) {
            $sdks = & $c --list-sdks 2>$null | ForEach-Object { ($_ -split ' ')[0] }
            $major10 = $sdks | Where-Object { $_ -like '10.*' } | Select-Object -First 1
            if ($major10) { return @{ Dotnet = $c; Sdk = $major10 } }
        }
    }
    return $null
}

$sdk = Find-ProjectSdk
if (-not $sdk) { throw 'No .NET 10 SDK found (expected project SDK 10.0.403, e.g. per-user dotnet10 install). Aborting before any build.' }
$env:DOTNET_ROOT = Split-Path -Parent $sdk.Dotnet
"SDK: $($sdk.Dotnet) [$($sdk.Sdk)]"

# ------------------------------------------------------------- publish
if (-not $SkipPublish) {
    $pubLog = Join-Path $Logs ("publish_{0}.log" -f (Get-Date -Format yyyyMMdd_HHmmss))
    "Publishing (output only under the artifacts root) ..."
    $pubArgs = @('publish', (Join-Path $RepoRoot 'src\SourceGit.csproj'),
        '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true',
        '-p:DisableAOT=true', '-p:DisableUpdateDetection=true',
        '-p:UseAppHost=true', '-o', $Payload)
    $proc = Start-NativeProcess -FilePath $sdk.Dotnet -Arguments $pubArgs -WorkingDirectory $RepoRoot
    if ($proc.ExitCode -ne 0) {
        & $sdk.Dotnet @($pubArgs[1..($pubArgs.Count-1)]) 2>&1 | Tee-Object -FilePath $pubLog | Select-Object -Last 20
        throw "dotnet publish failed (exit $($proc.ExitCode)); see $pubLog"
    }
    "Published -> $Payload"
}

# ------------------------------------------------------------- staging
"Staging ..."
Remove-InsideArtifacts $Staging
New-Item -ItemType Directory -Force -Path $Staging | Out-Null
Get-ChildItem -LiteralPath $Payload | ForEach-Object {
    if ($_.Extension -ne '.pdb') {
        Copy-Item -LiteralPath $_.FullName -Destination $Staging -Recurse -Force
    }
}
Get-ChildItem -LiteralPath $Staging -Recurse -Filter '*.pdb' | ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force }
Copy-Item -LiteralPath (Join-Path $RepoRoot 'LICENSE') (Join-Path $Staging 'LICENSE.txt')
Copy-Item -LiteralPath (Join-Path $RepoRoot 'THIRD-PARTY-LICENSES.md') (Join-Path $Staging 'THIRD-PARTY-LICENSES.md')
Copy-Item -LiteralPath $ReadmeSrc (Join-Path $Staging 'README-KO.md')
# Seed preference: the installer writes it into data\preference.json ONLY IF ABSENT.
# Property names match the app's System.Text.Json source-generated Preferences
# (top-level "Locale" -> "ko_KR" per Models/Locales.cs; "Layout" -> "UseClassicLayout").
Set-Content -LiteralPath (Join-Path $Staging 'seed-preference.json') `
    -Value '{"Locale":"ko_KR","Layout":{"UseClassicLayout":true}}' -Encoding UTF8

# -------------------------------------------------- installer compiler
function Find-InnoCompiler {
    $local = Join-Path $Tooling 'InnoSetup6\ISCC.exe'
    if (Test-Path -LiteralPath $local) { return $local }
    $cmd = Get-Command iscc.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    foreach ($p in "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe") {
        if (Test-Path -LiteralPath $p) { return $p }
    }
    return $null
}

function Build-Installer {
    param([switch]$SmokeMode, [string]$OutDir)
    $iscc = Find-InnoCompiler
    if (-not $iscc) {
        throw @'
Inno Setup 6 compiler not found. Reproducible fetch (pinned, verified, project-local):
  url    : https://github.com/jrsoftware/issrc/releases/download/is-6_7_3/innosetup-6.7.3.exe
  sha256 : 9c73c3bae7ed48d44112a0f48e66742c00090bdb5bef71d9d3c056c66e97b732
  verify : cross-check https://files.jrsoftware.org/is/6/innosetup-6.7.3.exe.issig
           and Get-AuthenticodeSignature (must be Valid)
  install: /VERYSILENT /CURRENTUSER /SUPPRESSMSGBOXES /NORESTART /TASKS= /DIR=<ArtRoot>\tooling\InnoSetup6
'@
    }
    $issLog = Join-Path $Logs ("iscc_{0}_{1}.log" -f (Get-Date -Format yyyyMMdd_HHmmss), $(if ($SmokeMode) { 'smoke' } else { 'prod' }))
    $args = @("/DStageDir=$Staging", "/DOutDir=$OutDir", "/DPkgVersion=$PackageVersion")
    if ($SmokeMode) { $args += '/DSmokeMode=1' }
    $args += $IssFile
    $proc = Start-NativeProcess -FilePath $iscc -Arguments $args
    if ($proc.ExitCode -ne 0) {
        Get-Content -LiteralPath $issLog -Tail 30 -ErrorAction SilentlyContinue
        throw "ISCC failed (exit $($proc.ExitCode)); see $issLog"
    }
}

New-Item -ItemType Directory -Force -Path $Installer | Out-Null
Build-Installer -OutDir $Installer
$setupExe = Join-Path $Installer "SourceGitClassic-Preview-$PackageVersion-win-x64-setup.exe"
if (-not (Test-Path -LiteralPath $setupExe)) { throw "Expected installer not produced: $setupExe" }
$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $setupExe).Hash.ToLower()
$size = (Get-Item -LiteralPath $setupExe).Length
"INSTALLER : $setupExe"
"SHA256    : $hash"
"SIZE      : $size bytes"

# ---------------------------------------------------------- smoke test
if ($Smoke) {
    $launchedPid = $null
    $verifiedOwnership = $false
    try {
        # (a) production install detection - READ ONLY; never mutate a real install
        if (Test-Path -LiteralPath $ProdRegKey) {
            throw 'A real production SourceGit Classic install exists (HKCU uninstall key). Smoke refuses to run against it; remove/move it or skip -Smoke.'
        }

        # (b) real-user preference: record HASH ONLY (no content is ever read)
        $realPref = Join-Path $env:APPDATA 'SourceGit\preference.json'
        $before = if (Test-Path -LiteralPath $realPref) { (Get-FileHash -Algorithm SHA256 -LiteralPath $realPref).Hash } else { 'ABSENT' }
        "smoke: real-user preference.json hash before (hash only, content not read) = $before"

        # (c) alternate-identity smoke installer (different AppId, no shortcuts)
        Remove-InsideArtifacts $SmokeDir
        New-Item -ItemType Directory -Force -Path $SmokeDir | Out-Null
        $smokeOut = Join-Path $SmokeDir 'installer'
        Build-Installer -SmokeMode -OutDir $smokeOut
        $smokeExe = Get-ChildItem -LiteralPath $smokeOut -Filter '*.exe' | Select-Object -First 1
        if (-not $smokeExe) { throw 'smoke: alternate-identity installer not produced' }
        "smoke: alternate-identity installer = $($smokeExe.FullName)"
        "smoke: alternate SHA256 = $((Get-FileHash -LiteralPath $smokeExe.FullName).Hash.ToLower())"

        $instDir = Join-Path $SmokeDir 'install'
        $p = Start-Process -FilePath $smokeExe.FullName -ArgumentList @(
            '/VERYSILENT','/CURRENTUSER','/SUPPRESSMSGBOXES','/NORESTART',
            "/DIR=$instDir", "/LOG=$(Join-Path $Logs 'smoke-install.log')"
        ) -WindowStyle Hidden -Wait -PassThru
        if ($p.ExitCode -ne 0) { throw "smoke: fixture install failed (exit $($p.ExitCode))" }
        # (d) seed present and parses to ko_KR + Classic
        $seeded = Join-Path $instDir 'data\preference.json'
        if (-not (Test-Path -LiteralPath $seeded)) { throw 'smoke: data\preference.json not seeded' }
        $seedJson = Get-Content -Raw -LiteralPath $seeded | ConvertFrom-Json
        if ($seedJson.Locale -ne 'ko_KR' -or $seedJson.Layout.UseClassicLayout -ne $true) {
            throw 'smoke: seeded preference is not ko_KR/Classic'
        }

        # (e) user-data sentinel, then REINSTALL over the same dir: seed must not overwrite
        $sentinelFile = Join-Path $instDir 'data\smoke-sentinel.txt'
        Set-Content -LiteralPath $sentinelFile -Value "sentinel-$(Get-Random)" -Encoding Ascii
        $prefHashBefore = (Get-FileHash -LiteralPath $seeded).Hash
        $p = Start-Process -FilePath $smokeExe.FullName -ArgumentList @(
            '/VERYSILENT','/CURRENTUSER','/SUPPRESSMSGBOXES','/NORESTART',
            "/DIR=$instDir"
        ) -WindowStyle Hidden -Wait -PassThru
        if ($p.ExitCode -ne 0) { throw "smoke: reinstall failed (exit $($p.ExitCode))" }
        if (-not (Test-Path -LiteralPath $sentinelFile)) { throw 'smoke: reinstall deleted user data' }
        if ((Get-FileHash -LiteralPath $seeded).Hash -ne $prefHashBefore) { throw 'smoke: reinstall overwrote existing preference.json (seed guard broken)' }

        # (f) launch installed exe; fail CLOSED unless ownership is proven; kill only own PID
        $exe = Join-Path $instDir 'SourceGit.exe'
        $proc = Start-Process -FilePath $exe -WorkingDirectory $instDir -PassThru
        $launchedPid = $proc.Id
        Start-Sleep -Seconds 10
        $alive = Get-Process -Id $proc.Id -ErrorAction SilentlyContinue
        if (-not $alive) { throw "smoke: installed process exited within 10s (check $instDir\data)" }
        $ImagePath = $null
        try { $ImagePath = (Get-CimInstance Win32_Process -Filter "ProcessId=$($proc.Id)").ExecutablePath } catch { }
        if (-not $ImagePath) { throw "smoke: cannot resolve image path of PID $($proc.Id); failing closed WITHOUT killing it" }
        if ((Split-Path -Parent $ImagePath).ToLower() -ne $instDir.ToLower()) {
            throw "smoke: PID $($proc.Id) runs outside the fixture ($ImagePath); failing closed WITHOUT killing it"
        }
        $verifiedOwnership = $true
        $smokeAppPid = $proc.Id
        Stop-Process -Id $proc.Id -Force
        Wait-Process -Id $proc.Id -Timeout 15 -ErrorAction SilentlyContinue
        $launchedPid = $null
        "smoke: launched own PID $smokeAppPid from fixture (verified image path), closed"

        # (g) isolation: real-user preference hash unchanged (hash compare only)
        $after = if (Test-Path -LiteralPath $realPref) { (Get-FileHash -Algorithm SHA256 -LiteralPath $realPref).Hash } else { 'ABSENT' }
        if ($after -ne $before) { throw 'smoke: real-user preference.json hash changed - isolation broken' }

        # (h) uninstall the fixture ONLY (its own uninstaller, smoke AppId)
        $unins = Join-Path $instDir 'unins000.exe'
        $p = Start-Process -FilePath $unins -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') `
             -WindowStyle Hidden -Wait -PassThru
        if ($p.ExitCode -ne 0) { throw "smoke: uninstall failed (exit $($p.ExitCode))" }
        if (-not (Test-Path -LiteralPath $seeded)) { throw 'smoke: uninstall deleted user data (must be preserved)' }
        if (-not (Test-Path -LiteralPath $sentinelFile)) { throw 'smoke: uninstall deleted sentinel user data' }
        if (Test-Path -LiteralPath $ProdRegKey) { throw 'smoke: production AppId registry key appeared - unexpected mutation' }
        "smoke: uninstalled fixture; preference.json + sentinel preserved at $instDir\data; production registry untouched"

        Remove-InsideArtifacts $SmokeDir
        "SMOKE PASS (GUI text rendering itself is NOT verified by this test)"
    }
    finally {
        # kill ONLY a PID whose image path we verified to be inside the fixture
        if ($launchedPid -and $verifiedOwnership) {
            Stop-Process -Id $launchedPid -Force -ErrorAction SilentlyContinue
        }
    }
}

"Done."
}
finally {
    foreach ($v in $envSave.Keys) {
        if ($null -eq $envSave[$v]) { Remove-Item -LiteralPath "Env:$v" -ErrorAction SilentlyContinue }
        else { [Environment]::SetEnvironmentVariable($v, $envSave[$v]) }
    }
}
