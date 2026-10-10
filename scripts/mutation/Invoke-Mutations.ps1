#!/usr/bin/env pwsh
# Mutation testing for StyleBro's guards: each mutation breaks one guard, and the listed tests must then fail. Fails when a
# mutation survives (no test covers that guard) or no longer applies (the code changed: update its Find text).
# The entries live in scripts/mutation/mutations/<Area>.psd1, one file per source folder: src/StyleBro.Analyzers/<Area>/
# and src/StyleBro.CodeFixes/<Area>/ -> <Area>.psd1, src/StyleBro.Migrate -> Migrate.psd1, anything else -> Common.psd1.
# Each entry: @{ File = '<path>'; Find = '<text>'; Replace = '<text>'; Tests = '<test filter>' }. Find must occur exactly
# once in File; Replace takes its place, and at least one test matching Tests (a test class, optionally .Method) must then
# fail. A mutation that survives is a guard no test covers. Add one for every new guard.
#
#   ./scripts/mutation/Invoke-Mutations.ps1 [-Root <checkout>] [-Shard <i> -Shards <n>] [-OnlyFiles <paths>]
# -OnlyFiles (pull requests: the changed files; an array or a comma-separated list) runs only the entries whose File is
# listed or whose test class is a listed tests/ file; an empty list runs none. Guards can also break through other files (shared helpers): the full run on main
# catches those.
# -Shard/-Shards run only the entries whose index modulo n is i (0-based; CI runs the shards in parallel jobs). The stale
# check (Find must occur exactly once) is text only and always covers EVERY entry, so each shard fails on any stale one.
# The checkout must have no uncommitted changes to the mutated files: each mutation is undone with 'git checkout'.
# Locally, run it in a separate worktree (git worktree add ../stylebro-mut HEAD) so builds don't touch your working copy.
param([string]$Root = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent), [int]$Shard = 0, [int]$Shards = 1,
    [AllowEmptyCollection()][string[]]$OnlyFiles)
$ErrorActionPreference = 'Stop'
$env:StyleBroSelf = 'none'
# Sorted by name, so the entries' order, and with it each shard's share, is the same on every machine.
# Keep each file well under a few hundred entries: Import-PowerShellDataFile rejects large files ('dynamic expressions').
$files = @(Get-ChildItem (Join-Path $PSScriptRoot 'mutations') -Filter *.psd1 | Sort-Object Name -Culture ([cultureinfo]::InvariantCulture))
if (-not $files.Count) { throw "No mutation files in $(Join-Path $PSScriptRoot 'mutations')." }
$all = @(foreach ($f in $files) {
    try { $entries = (Import-PowerShellDataFile $f.FullName).Mutations }
    catch { throw "Cannot load $($f.Name): $($_.Exception.Message)" }
    foreach ($m in $entries) { $m.Source = $f.BaseName; $m }
})

function Get-Area($path) {
    if ($path -match '^src/StyleBro\.(Analyzers|CodeFixes)/([^/]+)/') { return $Matches[2] }
    if ($path -like 'src/StyleBro.Migrate/*') { return 'Migrate' }
    'Common'
}
$tests = Join-Path $Root 'tests/StyleBro.Tests'
if ($Shards -lt 1 -or $Shard -lt 0 -or $Shard -ge $Shards) { throw "Invalid shard $Shard of $Shards." }

function Get-Count($m) {
    ([regex]::Matches([IO.File]::ReadAllText((Join-Path $Root $m.File)), [regex]::Escape($m.Find))).Count
}

$stale = @(foreach ($m in $all) {
    $count = Get-Count $m
    if ($count -ne 1) { [pscustomobject]@{ Result = 'STALE'; File = $m.File; Find = $m.Find; Note = "found $count times" } }
    elseif ($m.Source -ne ($area = Get-Area $m.File)) {
        [pscustomobject]@{ Result = 'STALE'; File = $m.File; Find = $m.Find; Note = "in $($m.Source).psd1, belongs in $area.psd1" }
    }
})
$run = $all
if ($PSBoundParameters.ContainsKey('OnlyFiles')) {
    $OnlyFiles = @($OnlyFiles | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim().Replace('\', '/') } | Where-Object { $_ })
    $classes = @($OnlyFiles | Where-Object { $_ -like 'tests/*.cs' } | ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_) })
    $run = @($all | Where-Object { $_.File -in $OnlyFiles -or $_.Tests.Split('.')[0] -in $classes })
}

$mine = @(for ($i = $Shard; $i -lt $run.Count; $i += $Shards) { if ((Get-Count $run[$i]) -eq 1) { $run[$i] } })
Write-Host "Shard $Shard of ${Shards}: $($mine.Count) mutations of $($run.Count) selected, $($all.Count) in total ($($stale.Count) stale)."

if ($mine.Count) {
    dotnet build $tests -nologo -v q | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'The unmutated build fails.' }
}

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
