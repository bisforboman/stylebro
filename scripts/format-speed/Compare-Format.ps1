#!/usr/bin/env pwsh
# The parity harness of docs/proposals/format-speed.md: runs 'bro check', 'bro format' and 'bro check' again with the
# default engine ('dotnet format' processes) and with --engine host on two fresh clones of the same commit, and compares
# what users see: the exit codes, the output (the clone's path written as <root>, the --timings block left out, finding
# lines, kept-finding lines and "Checked" progress lines compared as sorted sets: both engines analyze and fix
# concurrently, so their order isn't stable between two runs of the same engine either), the working trees after 'format' (stylebro.baseline included)
# and the --json reports. Any difference fails. Prints both engines' times.
#
#   pwsh scripts/format-speed/Compare-Format.ps1 -Repository <clone at the commit to compare> [-Work <folder>] [-Bro <command>]
#   [-Only check] [-Option <extra bro option> ...]
#
# The repository is cloned twice from -Repository (its committed state: run 'bro init --write' and add the package first,
# as a trial's step 4 does). -Bro defaults to the StyleBro.Migrate build in this repository (dotnet <dll>).
param(
    [Parameter(Mandatory)] [string] $Repository,
    [string] $Work = (Join-Path ([IO.Path]::GetTempPath()) "stylebro-parity-$([Guid]::NewGuid().ToString('N').Substring(0, 8))"),
    [string] $Bro = "dotnet $(Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'src/StyleBro.Migrate/bin/Debug/net8.0/StyleBro.Migrate.dll')",
    [ValidateSet('check', 'all')] [string] $Only = 'all',
    [string[]] $Option = @(),
    # Compares the logs and trees an earlier run left in -Work again, without running anything.
    [switch] $Reuse
)
$ErrorActionPreference = 'Stop'
$Repository = (Resolve-Path $Repository).Path
$Work = [IO.Path]::GetFullPath($Work)
$name = Split-Path $Repository -Leaf
New-Item -ItemType Directory -Force $Work | Out-Null
$failures = [Collections.Generic.List[string]]::new()
$times = [ordered]@{}

# What the comparison reads of a command's output: <root> for the clone, no timings; the finding lines and the "Checked"
# progress lines as sorted sets after the rest (both engines analyze at once, and when a line arrives between other output
# varies between two runs of the same engine), every other line in its order.
function Normalize([string[]] $lines, [string] $root) {
    $ordered = [Collections.Generic.List[string]]::new()
    $unordered = [Collections.Generic.List[string]]::new()
    $timings = $false
    foreach ($raw in $lines) {
        $line = $raw.Replace($root, '<root>').TrimEnd()
        if ($line.StartsWith('Timings (')) { $timings = $true; continue }
        if ($timings -and $line.StartsWith('  ')) { continue }
        $timings = $false
        # Also the findings a build of another configuration lists ('  path(l,c): BRO1108: ...', trial 48, Tomlyn's Release
        # build): in MSBuild's output order, which a parallel build changes between two runs of the same engine.
        if ($line -match '^\s*.+?\(\d+,\d+\): (?:error|warning|kept) \w+:' -or $line -match '^\s+.+?\(\d+,\d+\): [A-Z]+\d+: ' -or $line -match '^Checked .+ \(run \d+ of \d+\)$') {
            $unordered.Add(($line -replace ' \(run \d+ of \d+\)$', ''))
        }
        else {
            $ordered.Add($line)
        }
    }

    return @($ordered) + @('--- unordered ---') + @($unordered | Sort-Object -CaseSensitive)
}

function Run([string] $engine, [string] $clone, [string] $step, [string[]] $arguments) {
    $log = Join-Path $Work "$name-$engine-$step.txt"
    $json = Join-Path $Work "$name-$engine-$step.json"
    $engineOption = if ($engine -eq 'host') { @('--engine', 'host') } else { @() }
    $command = "$Bro $($arguments -join ' ') $clone --timings $($engineOption -join ' ') $($Option -join ' ') --json"
    if ($Reuse -and (Test-Path $log)) {
        $code = if (Test-Path "$log.exit") { [int](Get-Content "$log.exit") } else { $null }
        return [pscustomobject]@{ Code = $code; Log = $log; Json = $json }
    }

    $watch = [Diagnostics.Stopwatch]::StartNew()
    # --json: the report on stdout, the text on stderr.
    & bash -c "$command > '$json' 2> '$log'"
    $code = $LASTEXITCODE
    Set-Content "$log.exit" $code
    $times["$engine $step"] = '{0:0.0} s' -f $watch.Elapsed.TotalSeconds
    return [pscustomobject]@{ Code = $code; Log = $log; Json = $json }
}

# A --json report as text with sorted keys and without its timings, for comparing.
function Canonical([string] $json) {
    $json | python3 -c "import json, sys; r = json.load(sys.stdin); [r.pop(k, None) for k in ('timings', 'output')]; print(json.dumps(r, sort_keys=True, indent=1))"
}

function Compare-Step([string] $step, $old, $new, [string] $oldClone, [string] $newClone) {
    if ($old.Code -ne $new.Code) { $failures.Add("$step exit code: dotnet-format $($old.Code), host $($new.Code)") }
    $a = Normalize (Get-Content $old.Log) $oldClone
    $b = Normalize (Get-Content $new.Log) $newClone
    $diff = Compare-Object $a $b -SyncWindow 0 -CaseSensitive
    if ($diff) {
        $failures.Add("$step output differs ($($old.Log) vs $($new.Log)):`n" + (($diff | Select-Object -First 20 | ForEach-Object { "  $($_.SideIndicator) $($_.InputObject)" }) -join "`n"))
    }

    if ((Test-Path $old.Json) -and (Test-Path $new.Json)) {
        $ja = (Get-Content -Raw $old.Json) + ''
        $jb = (Get-Content -Raw $new.Json) + ''
        if ($ja.Trim() -and $jb.Trim()) {
            $ca = (Canonical $ja.Replace($oldClone, '<root>')) -join "`n"
            $cb = (Canonical $jb.Replace($newClone, '<root>')) -join "`n"
            if ($ca -ne $cb) { $failures.Add("$step --json report differs ($($old.Json) vs $($new.Json))") }
        }
    }
}

# The trees as git sees them: every changed or new file's content.
function TreeState([string] $clone) {
    git -C $clone add -A 2>$null | Out-Null
    $state = git -C $clone diff --cached --binary HEAD
    git -C $clone reset -q
    return ($state -join "`n")
}

$clones = @{}
foreach ($engine in 'dotnet-format', 'host') {
    $clone = Join-Path $Work "$name-$engine"
    if (-not $Reuse) {
        if (Test-Path $clone) { Remove-Item -Recurse -Force $clone }
        git clone -q --no-hardlinks $Repository $clone
    }

    $clones[$engine] = $clone
}

$steps = if ($Only -eq 'check') { @('check') } else { @('check', 'format', 'recheck') }
foreach ($step in $steps) {
    $command = if ($step -eq 'format') { @('format') } else { @('check') }
    $results = @{}
    foreach ($engine in 'dotnet-format', 'host') {
        Write-Host "== ${name}: $engine $step"
        $results[$engine] = Run $engine $clones[$engine] $step $command
    }

    Compare-Step $step $results['dotnet-format'] $results['host'] $clones['dotnet-format'] $clones['host']
    if ($step -eq 'format') {
        $ta = (TreeState $clones['dotnet-format']).Replace($clones['dotnet-format'], '<root>')
        $tb = (TreeState $clones['host']).Replace($clones['host'], '<root>')
        if ($ta -ne $tb) { $failures.Add("the working trees differ after 'format' (git diff in $($clones['dotnet-format']) and $($clones['host']))") }
    }
}

Write-Host "Times ($name):"
$times.GetEnumerator() | ForEach-Object { Write-Host ("  {0,-24} {1,8}" -f $_.Key, $_.Value) }
if ($failures.Count -gt 0) {
    Write-Host "DIFFERENT ($name):"
    $failures | ForEach-Object { Write-Host "  $_" }
    exit 1
}

Write-Host "Same ($name): exit codes, output, trees and reports."
