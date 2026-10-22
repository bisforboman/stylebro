<#
.SYNOPSIS
    Compares the analyzers of released StyleBro versions (and StyleCop.Analyzers) with a local build, in the benchmark's
    compare mode. Used for the performance audit (docs/proposals/performance-audit-2026-10.md).

.DESCRIPTION
    Downloads each version's StyleBro.Analyzers package from nuget.org into -Work (once), builds src/StyleBro.Analyzers
    in Release (unless -Head names a DLL), and runs `scripts/benchmark compare <release> <head> <sources> <runs>` for
    every version, with and without --all-rules, -Repeat times each. `stylecop` as a version compares
    StyleCop.Analyzers 1.2.0-beta.556 (the StyleCop.Analyzers.Unstable 1.2.0.556 package) with the default rules only,
    as docs/stylebro-vs-stylecop.md did. Each run's output goes to -Work/logs/<version>-<rules>-<n>.txt; the summary
    lines (totals) are printed at the end.

.EXAMPLE
    pwsh scripts/benchmark/Compare-Releases.ps1 -Sources ~/perf/nj/Src/Newtonsoft.Json -Versions 0.7.0-alpha.1,stylecop
#>
param(
    [Parameter(Mandatory)] [string] $Sources,
    [string[]] $Versions = @('0.2.0-alpha.1', '0.7.0-alpha.1', '0.8.0-alpha.1', 'stylecop'),
    [string] $Work = (Join-Path ([IO.Path]::GetTempPath()) 'stylebro-releases'),
    [string] $Head,
    [int] $Runs = 10,
    [int] $Repeat = 2
)

$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '../..')
New-Item -ItemType Directory -Force (Join-Path $Work 'logs') | Out-Null

function Get-Dll([string] $version) {
    $id, $packageVersion, $dll = if ($version -eq 'stylecop') {
        'stylecop.analyzers.unstable', '1.2.0.556', 'StyleCop.Analyzers.dll'
    } else {
        'stylebro.analyzers', $version, 'StyleBro.Analyzers.dll'
    }

    $folder = Join-Path $Work "$id.$packageVersion"
    $path = Join-Path $folder "analyzers/dotnet/cs/$dll"
    if (-not (Test-Path $path)) {
        New-Item -ItemType Directory -Force $folder | Out-Null
        $nupkg = Join-Path $folder 'package.nupkg'
        Invoke-WebRequest "https://api.nuget.org/v3-flatcontainer/$id/$packageVersion/$id.$packageVersion.nupkg" -OutFile $nupkg
        Expand-Archive $nupkg -DestinationPath $folder -Force
    }

    return $path
}

if (-not $Head) {
    $env:StyleBroSelf = 'none'
    $Head = Join-Path $Work 'head/StyleBro.Analyzers.dll'
    dotnet build (Join-Path $repo 'src/StyleBro.Analyzers') -c Release -nologo -v q -o (Join-Path $Work 'head') | Out-Host
}

dotnet build (Join-Path $repo 'scripts/benchmark') -c Release -nologo -v q | Out-Host
$benchmark = Join-Path $repo 'scripts/benchmark/bin/Release/net10.0/Benchmark.dll'

$summary = @()
foreach ($version in $Versions) {
    $dll = Get-Dll $version
    $modes = if ($version -eq 'stylecop') { @('default') } else { @('default', 'all') }
    foreach ($rules in $modes) {
        for ($n = 1; $n -le $Repeat; $n++) {
            $log = Join-Path $Work "logs/$version-$rules-$n.txt"
            $arguments = @('compare', $dll, $Head, $Sources, $Runs) + $(if ($rules -eq 'all') { '--all-rules' } else { @() })
            $watch = [Diagnostics.Stopwatch]::StartNew()
            dotnet $benchmark @arguments *> $log
            $total = Select-String -Path $log -Pattern '\*\*total\*\*' | Select-Object -Last 1
            $summary += "$version $rules #$n ($([int]$watch.Elapsed.TotalSeconds) s): $($total.Line)"
            Write-Host $summary[-1]
        }
    }
}

Write-Host ''
$summary | ForEach-Object { Write-Host $_ }
