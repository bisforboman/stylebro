#!/usr/bin/env pwsh
# Surveys one repo that uses StyleCop and writes two files to data/:
#   <Name>-configs.csv  every explicit StyleCop severity in its rulesets, global configs and .editorconfig files
#   <Name>-counts.csv   unique diagnostics per rule when the repo is built with EVERY StyleCop rule on as a warning
#
# The count temporarily appends a section to each .editorconfig in the repo (and restores them with git afterwards),
# so the repo must be a git checkout without uncommitted changes. A fresh clone is safest. Register the repo in
# repos.psd1 so New-Mapping.ps1 includes it.
param(
    [Parameter(Mandatory)][string]$Path,      # the repo checkout
    [Parameter(Mandatory)][string]$Name,      # short name used for the data files, e.g. 'polly'
    [Parameter(Mandatory)][string]$Solution,  # solution or project to build, relative to $Path
    [string]$Inventory = (Join-Path $PSScriptRoot 'data/inventory-1.2.0-beta.556.csv')
)
$ErrorActionPreference = 'Stop'
$Path = (Resolve-Path $Path).Path
$data = Join-Path $PSScriptRoot 'data'

git -C $Path rev-parse --is-inside-work-tree *> $null
if ($LASTEXITCODE -ne 0) { throw "$Path is not a git checkout." }
if (git -C $Path status --porcelain --untracked-files=no) { throw "$Path has uncommitted changes. Use a clean clone." }

function Get-ConfigFiles([string[]]$Include) {
    Get-ChildItem $Path -Recurse -Include $Include -File -Force | Where-Object FullName -notmatch '[\\/](node_modules|bin|obj|\.git)[\\/]'
}

# ---- 1. Explicit StyleCop settings ----------------------------------------------------------------
$settings = foreach ($f in Get-ConfigFiles @('.editorconfig', '*.globalconfig', '*.ruleset')) {
    $rel = [IO.Path]::GetRelativePath($Path, $f.FullName)
    if ($f.Extension -eq '.ruleset') {
        foreach ($rule in ([xml](Get-Content $f.FullName -Raw)).SelectNodes('//Rule')) {
            if ($rule.Id -match '^S[AX]\d') { [pscustomobject]@{ File = $rel; Id = $rule.Id; Setting = $rule.Action.ToLowerInvariant() } }
        }
    }
    else {
        foreach ($line in Get-Content $f.FullName) {
            if ($line -match '^\s*dotnet_diagnostic\.(S[AX]\d+\w*)\.severity\s*=\s*([a-z]+)') {
                [pscustomobject]@{ File = $rel; Id = $Matches[1]; Setting = $Matches[2] }
            }
        }
    }
}
$settings | Export-Csv (Join-Path $data "$Name-configs.csv") -NoTypeInformation
"$Name`: $(@($settings).Count) explicit StyleCop settings"

# ---- 2. Count with every rule on ------------------------------------------------------------------
# Later .editorconfig sections win, and .editorconfig beats rulesets and global configs.
$ids = (Import-Csv $Inventory).Id
$section = "`n`n# --- StyleBro survey: every StyleCop rule on (temporary) ---`n[*.cs]`n" +
    (($ids | ForEach-Object { "dotnet_diagnostic.$_.severity = warning" }) -join "`n") + "`n"
$editorconfigs = @(Get-ConfigFiles '.editorconfig')
$rootConfig = Join-Path $Path '.editorconfig'
$createdRoot = -not (Test-Path $rootConfig)
if ($createdRoot) { Set-Content $rootConfig 'root = false'; $editorconfigs += Get-Item $rootConfig }

$log = Join-Path ([IO.Path]::GetTempPath()) "stylebro-survey-$Name.log"
try {
    foreach ($c in $editorconfigs) { Add-Content $c.FullName $section }
    $started = Get-Date
    dotnet build (Join-Path $Path $Solution) -nologo --no-incremental `
        -p:TreatWarningsAsErrors=false -p:CodeAnalysisTreatWarningsAsErrors=false -p:WarningsAsErrors= `
        -p:MSBuildTreatWarningsAsErrors=false -p:GenerateDocumentationFile=true -p:CodeAnalysisRuleSet= *> $log
}
finally {
    if ($createdRoot) { Remove-Item $rootConfig -Force }
    $tracked = $editorconfigs | Where-Object { Test-Path $_.FullName } | ForEach-Object { [IO.Path]::GetRelativePath($Path, $_.FullName) }
    if ($tracked) { git -C $Path checkout -- @tracked }
}

$pattern = '^(?<file>[^(]+\.cs)\((?<line>\d+),(?<col>\d+)\): (?:warning|error) (?<id>S[AX]\d+\w*):'
$unique = @{}
foreach ($line in [IO.File]::ReadLines($log)) {
    if ($line -match $pattern) { $unique["$($Matches.id)|$($Matches.file)|$($Matches.line)|$($Matches.col)"] = @($Matches.id, $Matches.file) }
}
$rows = $unique.Values | Group-Object { $_[0] } | ForEach-Object {
    [pscustomobject]@{ Id = $_.Name; Diagnostics = $_.Count; Files = @($_.Group | ForEach-Object { $_[1] } | Sort-Object -Unique).Count }
} | Sort-Object Diagnostics -Descending
$rows | Export-Csv (Join-Path $data "$Name-counts.csv") -NoTypeInformation

$failed = @(Select-String -Path $log -Pattern ': error (?!S[AX]\d)' | ForEach-Object {
    if ($_.Line -match '\[(?<p>[^\]]+\.csproj)') { Split-Path $Matches.p -Leaf } } | Sort-Object -Unique)
"{0}: {1:n0} StyleCop diagnostics across {2} rules in {3:n1} min (build log: {4})" -f `
    $Name, $unique.Count, @($rows).Count, ((Get-Date) - $started).TotalMinutes, $log
if ($failed) { "  {0} project(s) failed to compile, so their diagnostics are missing: {1}" -f $failed.Count, ($failed -join ', ') }
