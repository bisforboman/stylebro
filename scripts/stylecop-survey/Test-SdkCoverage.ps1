#!/usr/bin/env pwsh
# For each case in sdk-check-cases.ps1: does 'dotnet format' with only the SDK's own formatters and code-style fixes
# (not StyleCop's fixes) make the StyleCop diagnostic go away, given the settings in sdk-check.editorconfig?
# Writes data/sdk-check.csv with Before/After diagnostic counts and a verdict per rule.
# -StyleCopVersion: the StyleCop.Analyzers version (its data/inventory-<version>.csv lists the rules turned on).
param([string]$WorkDir = (Join-Path ([IO.Path]::GetTempPath()) 'stylebro-sdk-check'), [string]$StyleCopVersion = '1.3.177-beta')
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'sdk-check-cases.ps1')
$ids = (Import-Csv (Join-Path $PSScriptRoot "data/inventory-$StyleCopVersion.csv")).Id

if (Test-Path $WorkDir) { Remove-Item $WorkDir -Recurse -Force }
New-Item $WorkDir -ItemType Directory | Out-Null
Set-Content (Join-Path $WorkDir 'p.csproj') @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <NoWarn>CS0169;CS0414;CS0219;CS1591;CS0649;CS8321;CS0660;CS0661;CS0642</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="StyleCop.Analyzers" Version="$StyleCopVersion" PrivateAssets="all" />
    <AdditionalFiles Include="stylecop.json" />
  </ItemGroup>
</Project>
"@
Set-Content (Join-Path $WorkDir 'stylecop.json') '{ "settings": { "documentationRules": { "xmlHeader": false, "companyName": "Test", "copyrightText": "Copyright (c) Test." } } }'
Set-Content (Join-Path $WorkDir '.editorconfig') ((Get-Content (Join-Path $PSScriptRoot 'sdk-check.editorconfig') -Raw) +
    "`n# Every StyleCop rule on, so default-off rules are checked too`n" + (($ids | ForEach-Object { "dotnet_diagnostic.$_.severity = warning" }) -join "`n") + "`n" +
    "`n# SX1412 wants UTF-8 without a byte order mark: its case is written with one`n[SX1412.cs]`ncharset = utf-8`n")

foreach ($c in $cases.GetEnumerator()) {
    $code = $c.Value
    if ($code -notmatch '^\s*(using|namespace)') {
        $lead = [regex]::Match($code, '^\n*').Value  # keep leading blank lines (SA1517) in front of the namespace
        $code = $lead + "namespace Cases.$($c.Key);`n`n" + $code.Substring($lead.Length)
    }
    if (-not $code.EndsWith("`n")) { $code += "`n" }
    [IO.File]::WriteAllText((Join-Path $WorkDir "$($c.Key).cs"), $code, [Text.UTF8Encoding]::new($c.Key -eq 'SX1412'))
}

$project = Join-Path $WorkDir 'p.csproj'
function Get-Hits {
    $out = dotnet build $project -nologo --no-incremental 2>&1
    $hits = @{}
    foreach ($line in $out) {
        # Only count a case's own rule in its own file.
        if ("$line" -match '[\\/](?<case>S[AX]\d+)\.cs\(\d+,\d+\): (?:warning|error) (?<id>S[AX]\d+\w*):' -and $Matches.case -eq $Matches.id) {
            $hits[$Matches.id] = 1 + [int]$hits[$Matches.id]
        }
    }
    $errors = @($out | Where-Object { "$_" -match ': error CS' } | Sort-Object -Unique)
    if ($errors) { throw "Cases don't compile:`n$($errors | Select-Object -First 10 | Out-String)" }
    return $hits
}

dotnet restore $project -v q | Out-Null
$before = Get-Hits
dotnet format style $project --severity warn --no-restore | Out-Null
dotnet format whitespace $project --no-restore | Out-Null
$after = Get-Hits

$results = foreach ($id in $cases.Keys) {
    $b = [int]$before[$id]; $a = [int]$after[$id]
    $verdict = if ($b -eq 0) { 'no-repro' } elseif ($a -eq 0) { 'fixed' } elseif ($a -lt $b) { 'partly' } else { 'not-fixed' }
    [pscustomobject]@{ Id = $id; Before = $b; After = $a; Verdict = $verdict }
}
$results | Export-Csv (Join-Path $PSScriptRoot 'data/sdk-check.csv') -NoTypeInformation
$results | Group-Object Verdict | ForEach-Object { '{0,-10} {1,3}: {2}' -f $_.Name, $_.Count, (($_.Group.Id) -join ' ') }
