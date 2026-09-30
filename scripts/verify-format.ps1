#!/usr/bin/env pwsh
# End-to-end check that 'dotnet format' applies StyleBro fixes, gives the expected output,
# and that a second run changes nothing (the fixes converge in one pass).
$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$sample = Join-Path $root 'samples/Messy'
$project = Join-Path $sample 'Messy.csproj'
$generated = Join-Path $sample 'Generated'
$expected = Join-Path $sample 'Expected'

if (Test-Path $generated) { Remove-Item $generated -Recurse -Force }
New-Item $generated -ItemType Directory | Out-Null
Copy-Item (Join-Path $sample 'Input/*.cs') $generated

# The analyzer DLLs must exist before 'dotnet format' loads the project.
dotnet build $project --nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

Write-Host 'Pass 1: dotnet format applies fixes'
dotnet format analyzers $project --diagnostics BRO1001 BRO1101 BRO1102 --severity warn --no-restore
if ($LASTEXITCODE -ne 0) { throw "dotnet format failed ($LASTEXITCODE)." }

Write-Host 'Pass 2: a second run must change nothing'
dotnet format analyzers $project --diagnostics BRO1001 BRO1101 BRO1102 --severity warn --no-restore --verify-no-changes
if ($LASTEXITCODE -ne 0) { throw 'Fixes did not converge: second dotnet format run still wanted changes.' }

$failed = $false
foreach ($file in Get-ChildItem $expected -Filter *.cs) {
    $actualPath = Join-Path $generated $file.Name
    $want = (Get-Content $file.FullName -Raw) -replace "`r`n", "`n"
    $got = (Get-Content $actualPath -Raw) -replace "`r`n", "`n"
    if ($want -ne $got) {
        $failed = $true
        Write-Host "MISMATCH: $($file.Name)" -ForegroundColor Red
        Compare-Object ($want -split "`n") ($got -split "`n") | Format-Table -AutoSize | Out-String | Write-Host
    }
    else {
        Write-Host "OK: $($file.Name)" -ForegroundColor Green
    }
}

if ($failed) { throw 'dotnet format output does not match samples/Messy/Expected.' }
Write-Host 'dotnet format integration: all good.' -ForegroundColor Green
