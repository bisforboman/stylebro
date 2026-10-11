#!/usr/bin/env pwsh
# Writes a new decision to docs/decisions/<yyyy-mm-dd>-<slug>.md (one file per decision, so parallel pull requests never
# edit the same file) with the Question, Choices and Decision headings, and prints its path.
#
#   ./scripts/New-Decision.ps1 -Title <text> [-Date yyyy-mm-dd]
param(
    [Parameter(Mandatory)][string]$Title,
    [string]$Date = (Get-Date -Format 'yyyy-MM-dd'))
$ErrorActionPreference = 'Stop'

$slug = ($Title.ToLowerInvariant() -replace '[^\w\- ]', '').Trim() -replace ' ', '-' -replace '-+', '-'
if ($slug.Length -gt 60) { $slug = $slug.Substring(0, 60) -replace '-[^-]*$', '' }
$path = Join-Path (Join-Path $PSScriptRoot '../docs/decisions') "$Date-$slug.md"
if (Test-Path $path) { throw "$path already exists." }
$text = "# $Title ($Date)`n`n## Question`n`n## Choices`n`n## Decision`n"
[IO.File]::WriteAllText($path, $text)
[IO.Path]::GetFullPath($path)
