#!/usr/bin/env pwsh
# Mutation testing for StyleBro's guards (scripts/mutation/mutations.psd1): each mutation breaks one guard, and the
# listed tests must then fail. Fails when a mutation survives (no test covers that guard) or no longer applies (the code
# changed: update its Find text).
#
#   ./scripts/mutation/Invoke-Mutations.ps1 [-Root <checkout>] [-Shard <i> -Shards <n>]
# -Shard/-Shards run only the entries whose index modulo n is i (0-based; CI runs the shards in parallel jobs). The stale
# check (Find must occur exactly once) is text only and always covers EVERY entry, so each shard fails on any stale one.
# The checkout must have no uncommitted changes to the mutated files: each mutation is undone with 'git checkout'.
# Locally, run it in a separate worktree (git worktree add ../stylebro-mut HEAD) so builds don't touch your working copy.
param([string]$Root = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent), [int]$Shard = 0, [int]$Shards = 1)
$ErrorActionPreference = 'Stop'
$env:StyleBroSelf = 'none'
$config = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'mutations.psd1')
$tests = Join-Path $Root 'tests/StyleBro.Tests'
if ($Shards -lt 1 -or $Shard -lt 0 -or $Shard -ge $Shards) { throw "Invalid shard $Shard of $Shards." }

function Get-Count($m) {
    ([regex]::Matches([IO.File]::ReadAllText((Join-Path $Root $m.File)), [regex]::Escape($m.Find))).Count
}

$all = $config.Mutations
$stale = @(foreach ($m in $all) {
    $count = Get-Count $m
    if ($count -ne 1) { [pscustomobject]@{ Result = 'STALE'; File = $m.File; Find = $m.Find; Note = "found $count times" } }
})
$mine = @(for ($i = $Shard; $i -lt $all.Count; $i += $Shards) { if ((Get-Count $all[$i]) -eq 1) { $all[$i] } })
Write-Host "Shard $Shard of ${Shards}: $($mine.Count) mutations of $($all.Count) ($($stale.Count) stale in total)."

dotnet build $tests -nologo -v q | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'The unmutated build fails.' }

$results = $stale + @(foreach ($m in $mine) {
    $file = Join-Path $Root $m.File
    $text = [IO.File]::ReadAllText($file)
    [IO.File]::WriteAllText($file, $text.Replace($m.Find, $m.Replace))
    try {
        $out = dotnet test $tests -nologo -v q --filter "FullyQualifiedName~$($m.Tests)" 2>&1
        $result = if ($LASTEXITCODE -ne 0) { if ($out -match ' error CS') { 'KILLED (build)' } else { 'KILLED' } } else { 'SURVIVED' }
    }
    finally {
        git -C $Root checkout -q -- $m.File
    }

    [pscustomobject]@{ Result = $result; File = $m.File; Find = $m.Find; Note = $m.Tests }
})

$results | Format-Table -AutoSize -Wrap | Out-Host
if ($env:GITHUB_STEP_SUMMARY) {
    $title = if ($Shards -gt 1) { "### Mutation testing (shard $Shard of $Shards)" } else { '### Mutation testing' }
    $title, '', '| Result | File | Guard |', '|---|---|---|' | Add-Content $env:GITHUB_STEP_SUMMARY
    $results | ForEach-Object { "| $($_.Result) | $($_.File) | ``$($_.Find)`` |" } | Add-Content $env:GITHUB_STEP_SUMMARY
}

$bad = @($results | Where-Object { $_.Result -in 'SURVIVED', 'STALE' })
if ($bad.Count) { throw "$($bad.Count) mutation(s) survived or no longer apply." }
Write-Host "All $($results.Count) mutations were caught." -ForegroundColor Green
