#Requires -Version 5
<#
.SYNOPSIS
  Runs a command with git pointed away from this machine's own configuration files.

.DESCRIPTION
  Reading or writing the user's or the machine's git configuration is out of bounds for this project's
  automated work - a merged `git config -l` once put real user values into a log. This wrapper installs
  two empty fixture files that live outside every repository, switches the system file off as well, and
  then runs the command, so everything it starts - dotnet, MSBuild, and the `git describe` MSBuild runs
  for the version stamp - inherits the same isolation. The wrapper never reads a configuration file to
  check anything; it only creates the two empty files.

  Use it for builds, tests and development launches. `TestBootstrap.Prepare()` enforces the same three
  variables inside the test process, so a test run is covered from both ends.

.EXAMPLE
  tools/Invoke-IsolatedGitEnv.ps1 'dotnet test tests/SourceGit.Tests/SourceGit.Tests.csproj -c Debug'

.EXAMPLE
  tools/Invoke-IsolatedGitEnv.ps1 'dotnet build src/SourceGit.csproj -c Release'
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Command
)

# A string rather than a script block, so a `-File` invocation works too; it becomes a block again
# below and runs in this scope.

$ErrorActionPreference = 'Stop'
$root = Join-Path $env:TEMP 'sourcegit-tests\git-env'
New-Item -ItemType Directory -Path $root -Force | Out-Null

foreach ($name in 'global.gitconfig', 'system.gitconfig') {
    $path = Join-Path $root $name
    if (!(Test-Path -LiteralPath $path)) { Set-Content -LiteralPath $path -Value '' -NoNewline }
}

$keepGlobal = $env:GIT_CONFIG_GLOBAL
$keepSystem = $env:GIT_CONFIG_SYSTEM
$keepNoSystem = $env:GIT_CONFIG_NOSYSTEM

try {
    $env:GIT_CONFIG_GLOBAL = Join-Path $root 'global.gitconfig'
    $env:GIT_CONFIG_SYSTEM = Join-Path $root 'system.gitconfig'
    $env:GIT_CONFIG_NOSYSTEM = '1'
    . ([scriptblock]::Create($Command))
    exit $LASTEXITCODE
}
finally {
    $env:GIT_CONFIG_GLOBAL = $keepGlobal
    $env:GIT_CONFIG_SYSTEM = $keepSystem
    $env:GIT_CONFIG_NOSYSTEM = $keepNoSystem
}
