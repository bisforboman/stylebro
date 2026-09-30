#!/usr/bin/env pwsh
# End-to-end check that 'dotnet format' applies StyleBro fixes, gives the expected output, and that a second run
# changes nothing (the fixes converge in one pass). Runs every sample: samples/<name>/Input/*.cs is copied to
# Generated/ (gitignored), formatted, and compared with Expected/.
#   Messy        single target framework, one file per rule area
#   MultiTarget  two target frameworks with '#if' code, so each framework's copy of a file needs different fixes
$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent

# Every rule in the release tracking files (shipped and unshipped), so a new rule can't be left out of the check.
$ids = @(Select-String -Path (Join-Path $root 'src/StyleBro.Analyzers/AnalyzerReleases.*.md') -Pattern '^(BRO\d{4})\s*\|' |
    ForEach-Object { $_.Matches[0].Groups[1].Value } | Sort-Object -Unique)
Write-Host "Rules: $($ids -join ' ')"

$failed = $false
foreach ($name in 'Messy', 'MultiTarget') {
    $sample = Join-Path $root "samples/$name"
    $project = Join-Path $sample "$name.csproj"
    $generated = Join-Path $sample 'Generated'
    $expected = Join-Path $sample 'Expected'
    Write-Host "== $name"

    if (Test-Path $generated) { Remove-Item $generated -Recurse -Force }
    New-Item $generated -ItemType Directory | Out-Null
    Copy-Item (Join-Path $sample 'Input/*.cs') $generated

    # The analyzer DLLs must exist before 'dotnet format' loads the project.
    dotnet build $project --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "Build of $name failed." }

    Write-Host 'Pass 1: dotnet format applies fixes'
    dotnet format analyzers $project --diagnostics @ids --severity warn --no-restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet format failed on $name ($LASTEXITCODE)." }

    Write-Host 'Pass 2: a second run must change nothing'
    dotnet format analyzers $project --diagnostics @ids --severity warn --no-restore --verify-no-changes
    if ($LASTEXITCODE -ne 0) { throw "Fixes did not converge on ${name}: a second dotnet format run still wanted changes." }

    # The fixed code must still compile (for every target framework).
    dotnet build $project --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "The fixed $name sample doesn't compile." }

    foreach ($file in Get-ChildItem $expected -Filter *.cs) {
        $actualPath = Join-Path $generated $file.Name
        $want = (Get-Content $file.FullName -Raw) -replace "`r`n", "`n"
        $got = (Get-Content $actualPath -Raw) -replace "`r`n", "`n"
        if ($want -ne $got) {
            $failed = $true
            Write-Host "MISMATCH: $name/$($file.Name)" -ForegroundColor Red
            Compare-Object ($want -split "`n") ($got -split "`n") | Format-Table -AutoSize | Out-String | Write-Host
        }
        else {
            Write-Host "OK: $name/$($file.Name)" -ForegroundColor Green
        }
    }
}

if ($failed) { throw 'dotnet format output does not match the Expected folders.' }
Write-Host 'dotnet format integration: all good.' -ForegroundColor Green
