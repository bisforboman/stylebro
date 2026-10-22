<#
.SYNOPSIS
    The nightly drift check (.github/workflows/drift.yml): compares main's analyzers with the last released ones, with
    the benchmark's compare mode and the PR gate's thresholds.

.DESCRIPTION
    CI's `performance` job compares a pull request with its base only, so many small slowdowns can add up unnoticed (the
    performance audit of 2026-10-22, docs/proposals/performance-audit-2026-10.md: +4-7 % since 0.7.0-alpha.1, every PR
    under the 10 % gate). This compares the working tree's src/StyleBro.Analyzers (built in Release) with the release
    the self-check uses (`StyleBroSelfVersion` in Directory.Build.props): restoring StyleBro.Analyzers downloads that
    package into the NuGet packages folder (Directory.Build.props' PackageDownload), and its DLL is taken from there.
    `scripts/benchmark compare <release> <head> <sources> <runs> --all-rules`, exit 1 when the head is clearly slower
    (the same thresholds as the PR gate: scripts/benchmark/README.md). Without -Sources, Newtonsoft.Json at the
    real-world pin (scripts/realworld/repos.psd1) is cloned into -Work, as the `performance` job does. The report goes
    to the GitHub Actions job summary when there is one.

.EXAMPLE
    pwsh scripts/benchmark/Test-Drift.ps1
    pwsh scripts/benchmark/Test-Drift.ps1 -Sources ~/perf/nj/Src/Newtonsoft.Json -Runs 5
#>
param(
    [string] $Sources,
    [string] $Work = (Join-Path ([IO.Path]::GetTempPath()) 'stylebro-drift'),
    [int] $Runs = 10
)

$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '../..')
$analyzers = Join-Path $repo 'src/StyleBro.Analyzers'
New-Item -ItemType Directory -Force $Work | Out-Null

# The released DLL: the self-check's own download (StyleBroSelf=download for StyleBro.Analyzers).
dotnet restore $analyzers -nologo -v q | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Restoring StyleBro.Analyzers (and the released package) failed.' }
$properties = (dotnet msbuild $analyzers -getProperty:StyleBroSelfVersion -getProperty:NuGetPackageRoot | ConvertFrom-Json).Properties
$version = $properties.StyleBroSelfVersion
$release = Join-Path $properties.NuGetPackageRoot "stylebro.analyzers/$version/analyzers/dotnet/cs/StyleBro.Analyzers.dll"
if (-not (Test-Path $release)) { throw "The released StyleBro.Analyzers $version isn't at $release after the restore." }

# main's analyzers and the benchmark, without the self-check (as the performance job builds them).
$env:StyleBroSelf = 'none'
dotnet build $analyzers -c Release -nologo -v q -o (Join-Path $Work 'head') | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Building StyleBro.Analyzers failed.' }
$head = Join-Path $Work 'head/StyleBro.Analyzers.dll'

if (-not $Sources) {
    $nj = (Import-PowerShellDataFile (Join-Path $repo 'scripts/realworld/repos.psd1')).Newtonsoft
    $clone = Join-Path $Work 'nj'
    if (-not (Test-Path (Join-Path $clone '.git'))) { git init -q $clone }
    git -C $clone cat-file -e "$($nj.Commit)^{commit}" 2>$null
    if ($LASTEXITCODE -ne 0) { git -C $clone fetch -q --depth 1 $nj.Url $nj.Commit }
    git -C $clone checkout -q --force $nj.Commit
    if ($LASTEXITCODE -ne 0) { throw "Checking out Newtonsoft.Json $($nj.Commit) failed." }
    $Sources = Join-Path $clone 'Src/Newtonsoft.Json'
}

Write-Host "StyleBro.Analyzers $version (released) against this tree, $Sources, $Runs runs, --all-rules"
$log = Join-Path $Work 'compare.txt'
dotnet run -c Release --project (Join-Path $repo 'scripts/benchmark') -- compare $release $head $Sources $Runs --all-rules 2>&1 |
    Tee-Object -FilePath $log | Out-Host
$code = $LASTEXITCODE

if ($env:GITHUB_STEP_SUMMARY) {
    $verdict = if ($code -eq 0) { 'within the thresholds' } else { '**slower than the thresholds allow**' }
    @("### Drift: $version (released) against main", '', "Newtonsoft.Json, $Runs runs, --all-rules: $verdict.", '') +
        @(Get-Content $log | Where-Object { $_ -like '|*' }) | Add-Content $env:GITHUB_STEP_SUMMARY
}
exit $code
