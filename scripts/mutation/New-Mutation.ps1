#!/usr/bin/env pwsh
# Writes one mutation entry (see Invoke-Mutations.ps1) to scripts/mutation/entries/<Area>/<file>-<hash>.psd1 and prints
# its path. <Area> comes from File (Get-MutationArea), <file> is the guarded file's name without extension and <hash> the
# first 8 hex digits of the SHA-256 of File, Find, Replace and Tests: two pull requests adding entries never pick the same name
# unless they add the same mutation. The name is fixed once written; updating a stale Find later doesn't rename the file.
#
#   ./scripts/mutation/New-Mutation.ps1 -File <path> -Find <text> -Replace <text> -Tests <filter> [-Comment <text>]
param(
    [Parameter(Mandatory)][string]$File,
    [Parameter(Mandatory)][string]$Find,
    [Parameter(Mandatory)][AllowEmptyString()][string]$Replace,
    [Parameter(Mandatory)][string]$Tests,
    [string]$Comment)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Mutations.Common.ps1')

$File = $File.Replace('\', '/')
# Invoke-Mutations.ps1 puts 'FullyQualifiedName~' in front itself; doubled, the filter matches no test.
$Tests = $Tests -replace '^FullyQualifiedName~', ''
$sha = [Security.Cryptography.SHA256]::Create()
$bytes = $sha.ComputeHash([Text.Encoding]::UTF8.GetBytes("$File`n$Find`n$Replace`n$Tests"))
$hash = -join ($bytes[0..3] | ForEach-Object { $_.ToString('x2') })
$dir = Join-Path (Join-Path $PSScriptRoot 'entries') (Get-MutationArea $File)
$path = Join-Path $dir "$([IO.Path]::GetFileNameWithoutExtension($File))-$hash.psd1"
if (Test-Path $path) { throw "$path already exists (the same mutation)." }

function Quote([string]$text) { "'" + $text.Replace("'", "''") + "'" }
$lines = @(
    if ($Comment) { $Comment -split "`r?`n" | ForEach-Object { "# $_".TrimEnd() } }
    '@{'
    "    File = $(Quote $File)"
    "    Find = $(Quote $Find)"
    "    Replace = $(Quote $Replace)"
    "    Tests = $(Quote $Tests)"
    '}')
$null = New-Item -ItemType Directory -Force $dir
[IO.File]::WriteAllText($path, ($lines -join "`n") + "`n")
$path
