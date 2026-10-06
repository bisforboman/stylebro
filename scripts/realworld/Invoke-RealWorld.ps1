#!/usr/bin/env pwsh
# Runs every StyleBro rule on a public repository (scripts/realworld/repos.psd1) and checks what a user would see:
#   - no analyzer crashes (AD0001) and no new compile errors after 'dotnet format'
#   - no merge-conflict markers written into the source (multi-targeted projects)
#   - the fixes converge in one run: the second 'dotnet format' run changes no file (not '--verify-no-changes': it also
#     fails on warnings whose fix deliberately changes nothing, like the renames the string guards keep back); a repo
#     with a documented exception sets MaxRuns in repos.psd1
#   - with -Tests: no test that passed on the untouched code fails after the fixes
#
#   ./scripts/realworld/Invoke-RealWorld.ps1 -Repo Serilog [-Tests] [-Work <dir>] [-Enable BRO1313,BRO1314]
# -Enable turns on rules that are off by default (they don't run otherwise).
param(
    [Parameter(Mandatory)][string]$Repo,
    [string]$Work = (Join-Path ([IO.Path]::GetTempPath()) 'stylebro-realworld'),
    [switch]$Tests,
    [string[]]$Enable = @(),
    [string]$Configuration = 'Release'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$repos = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'repos.psd1')
if (-not $repos.ContainsKey($Repo)) { throw "Unknown repo '$Repo'. Known: $($repos.Keys -join ', ')" }
$r = $repos[$Repo]

# Every rule in the release tracking files.
$ids = @(Select-String -Path (Join-Path $root 'src/StyleBro.Analyzers/AnalyzerReleases.*.md') -Pattern '^(BRO\d{4})\s*\|' |
    ForEach-Object { $_.Matches[0].Groups[1].Value } | Sort-Object -Unique)

# StyleBro's analyzers, copied so the repository's builds don't lock this repository's build output.
$env:StyleBroSelf = 'none'
dotnet build (Join-Path $root 'src/StyleBro.CodeFixes') -c $Configuration -nologo -v q | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Building StyleBro failed.' }
$bin = Join-Path $Work 'stylebro-bin'
New-Item -ItemType Directory -Force $bin | Out-Null
Copy-Item (Join-Path $root "src/StyleBro.CodeFixes/bin/$Configuration/netstandard2.0/StyleBro.*.dll") $bin -Force
$hook = Join-Path $Work 'stylebro-hook.targets'

# -Enable: rules that are off by default, turned on through a global config (EditorConfigFiles, not
# GlobalAnalyzerConfigFiles: the SDK has already converted those when this hook is imported).
$enableItem = ''
# 'pwsh Invoke-RealWorld.ps1 -Enable A,B' (from another shell, or CI) passes one string "A,B".
$Enable = @($Enable | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
if ($Enable) {
    $enableConfig = Join-Path $Work 'stylebro-enable.globalconfig'
    Set-Content $enableConfig (@('is_global = true') + @($Enable | ForEach-Object { "dotnet_diagnostic.$_.severity = warning" }))
    $enableItem = "<EditorConfigFiles Include=`"$enableConfig`" />"
}

Set-Content $hook @"
<Project>
  <ItemGroup>
    <Analyzer Include="$(Join-Path $bin 'StyleBro.Analyzers.dll')" />
    <Analyzer Include="$(Join-Path $bin 'StyleBro.CodeFixes.dll')" />
    $enableItem
  </ItemGroup>
  <!-- What the package's build targets pass to the analyzers (multi-target guards: MultiTargetSuppressor, BRO1145, BRO1147). -->
  <PropertyGroup>
    <StyleBroTargetFrameworks Condition="'`$(StyleBroTargetFrameworks)' == '' and '`$(TargetFrameworks)' != ''">`$(TargetFrameworks.Replace(';', ','))</StyleBroTargetFrameworks>
    <StyleBroTargetFrameworks Condition="'`$(StyleBroTargetFrameworks)' == ''">`$(TargetFramework)</StyleBroTargetFrameworks>
  </PropertyGroup>
  <ItemGroup>
    <CompilerVisibleProperty Include="StyleBroTargetFrameworks" />
    <CompilerVisibleProperty Include="LangVersion" />
    <CompilerVisibleProperty Include="MaxSupportedLangVersion" />
    <CompilerVisibleProperty Include="GenerateDocumentationFile" />
  </ItemGroup>
</Project>
"@

# The repository at its pinned commit.
$path = Join-Path $Work $Repo
if (-not (Test-Path (Join-Path $path '.git'))) {
    git init -q $path
    git -C $path remote add origin $r.Url
}
git -C $path fetch -q --depth 1 origin $r.Commit
git -C $path checkout -q --force FETCH_HEAD
git -C $path clean -fdxq
$sln = Join-Path $path $r.Solution

# Every result below comes from 'git diff'; a broken clone must fail the run, not look like 'nothing changed'.
function Get-Diff([string[]]$options = @()) {
    $out = git -C $path diff @options
    if ($LASTEXITCODE -ne 0) { throw "git diff failed in $path (exit $LASTEXITCODE): the clone is broken." }
    $out
}

function Invoke-Build {
    $env:CustomAfterMicrosoftCommonTargets = $hook
    try { $out = dotnet build $sln -nologo --no-incremental -p:TreatWarningsAsErrors=false -p:CodeAnalysisTreatWarningsAsErrors=false 2>&1 }
    finally { Remove-Item Env:\CustomAfterMicrosoftCommonTargets -ErrorAction SilentlyContinue }
    @{
        Errors = @($out | Where-Object { "$_" -match ': error (?!BRO|AD0001)' } | ForEach-Object { ("$_" -replace '\s*\[.*$', '').Replace($path, '') } | Sort-Object -Unique)
        Crashes = @($out | Where-Object { "$_" -match 'AD0001' } | ForEach-Object { ("$_" -replace '^.*AD0001: ', '') -replace '\s*\[.*$', '' } | Sort-Object -Unique)
        Findings = @($out | Where-Object { "$_" -match 'warning BRO\d{4}' } | ForEach-Object { ("$_" -replace '\s*\[.*$', '') } | Sort-Object -Unique).Count
    }
}

function Invoke-Format([switch]$Verify) {
    $env:CustomAfterMicrosoftCommonTargets = $hook
    try {
        $formatArgs = @('format', 'analyzers', $sln, '--diagnostics') + $ids + @('--severity', 'warn', '--no-restore')
        if ($Verify) { $formatArgs += '--verify-no-changes' }
        dotnet @formatArgs 2>&1 | Out-Null
        return $LASTEXITCODE
    }
    finally { Remove-Item Env:\CustomAfterMicrosoftCommonTargets -ErrorAction SilentlyContinue }
}

function Get-FailedTests {
    # Names of the failing tests. Not StyleBro's analyzers here: the tests run on the code as it is.
    $failed = [Collections.Generic.HashSet[string]]::new()
    if ($r.Tests -eq 'mtp') {
        dotnet build $sln -c Release -nologo -v q -p:TreatWarningsAsErrors=false 2>&1 | Out-Null
        $exes = Get-ChildItem (Join-Path $path 'artifacts/bin') -Recurse -Include '*Tests.exe', '*Specs.exe', '*Tests', '*Specs' -File |
            Where-Object { $_.BaseName -match '(Tests|Specs)$' -and $_.Directory.FullName -match '[\\/]release' -and ($_.Extension -in '.exe', '') }
        foreach ($exe in $exes) {
            & $exe.FullName 2>&1 | Select-String '^\s*failed (\S+)' | ForEach-Object { [void]$failed.Add("$($exe.Directory.Name)/$($_.Matches[0].Groups[1].Value)") }
        }
    }
    else {
        # TestFilter (repos.psd1): leaves out tests that depend on something other than the code, e.g. the network.
        $filter = if ($r.TestFilter) { @('--filter', $r.TestFilter) } else { @() }
        dotnet test $sln -nologo -p:TreatWarningsAsErrors=false @filter 2>&1 | Select-String '^\s+Failed (\S+)' |
            ForEach-Object { [void]$failed.Add($_.Matches[0].Groups[1].Value) }
    }
    # The comma keeps an empty set a set (PowerShell would unroll it to $null).
    return , $failed
}

$problems = [Collections.Generic.List[string]]::new()
$report = [Collections.Generic.List[string]]::new()
$report.Add("### $Repo")

$failedBefore = $null
if ($Tests -and $r.Tests) {
    $failedBefore = Get-FailedTests
    $report.Add("- tests failing on the untouched code: $($failedBefore.Count)")
    git -C $path clean -fdxq
}

$before = Invoke-Build
$report.Add("- findings: $($before.Findings); compile errors before: $($before.Errors.Count)")
$before.Crashes | ForEach-Object { $problems.Add("analyzer crash: $_") }

$runs = 0
$converged = $false
$state = (Get-Diff | Out-String)
while ($runs -lt 3) {
    $runs++
    [void](Invoke-Format)
    $next = (Get-Diff | Out-String)
    if ($next -eq $state) { $converged = $true; break }
    $state = $next
}
$changed = @(Get-Diff '--name-only').Count
$allowed = if ($r.MaxRuns) { $r.MaxRuns } else { 2 }
$report.Add("- files changed: $changed; runs until no changes: $(if ($converged) { $runs } else { "more than $runs" }) (allowed: $allowed)")
if (-not $converged) { $problems.Add('the fixes did not converge in 3 runs') }
elseif ($runs -gt $allowed) { $problems.Add("$runs runs until no changes, allowed $allowed (one that changes, one clean; MaxRuns in repos.psd1 for a documented exception)") }

$markers = @(Get-Diff | Select-String '^\+(<<<<<<<|>>>>>>>) ')
if ($markers.Count) { $problems.Add("conflict markers written: $($markers.Count) lines") }

$after = Invoke-Build
$newErrors = @($after.Errors | Where-Object { $_ -notin $before.Errors })
$newErrors | ForEach-Object { $problems.Add("new compile error: $_") }
$after.Crashes | Where-Object { $_ -notin $before.Crashes } | ForEach-Object { $problems.Add("analyzer crash after fixing: $_") }
$report.Add("- warnings left (kept by guards): $($after.Findings); new compile errors: $($newErrors.Count)")

if ($null -ne $failedBefore) {
    $failedAfter = Get-FailedTests
    # Known failures: documented limits (repos.psd1), e.g. a class a test serializes by reflection without attributes.
    $known = @($r.KnownFailures)
    $newFailures = @($failedAfter | Where-Object { -not $failedBefore.Contains($_) -and $_ -notin $known })
    $report.Add("- tests failing after the fixes: $($failedAfter.Count) (new: $($newFailures.Count))")
    $newFailures | ForEach-Object { $problems.Add("test fails after the fixes: $_") }
}

$problems | ForEach-Object { $report.Add("- **PROBLEM**: $_") }
$report | Out-Host
if ($env:GITHUB_STEP_SUMMARY) { $report | Add-Content $env:GITHUB_STEP_SUMMARY }
if ($problems.Count) { throw "$Repo`: $($problems.Count) problem(s)." }
Write-Host "$Repo`: all good." -ForegroundColor Green
exit 0
