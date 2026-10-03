#!/usr/bin/env pwsh
# Checks the NuGet package the way a consumer uses it: packs StyleBro.Analyzers under a throwaway version, restores it
# from a local feed into a project outside the repository, and checks what only the package's build targets do:
#   1. the preset reaches the compiler (BRO1112, which the preset turns off, isn't reported; other rules are);
#   2. <StyleBroPreset>none</StyleBroPreset> turns it off;
#   3. after 'stylebro-migrate init --write', 'dotnet format' fixes the built-in rules too (their severities come from
#      .editorconfig) and applies the preset's formatting options; a second run changes nothing;
#   4. a stylebro.baseline above the project hides its violations, and a new violation is still reported.
# Every earlier check loaded the analyzers another way, which is how the preset went missing from alpha.1 to alpha.8.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$work = Join-Path ([IO.Path]::GetTempPath()) "stylebro-package-$([Guid]::NewGuid().ToString('N').Substring(0, 8))"
$feed = Join-Path $work 'feed'
$repo = Join-Path $work 'repo'
$project = Join-Path $repo 'src/App/App.csproj'
$version = '0.0.0-verify'
$failures = [Collections.Generic.List[string]]::new()

function Check([bool]$ok, [string]$what) {
    if ($ok) { Write-Host "  ok: $what" } else { Write-Host "  FAILED: $what"; $failures.Add($what) }
}

function Build([string[]]$extra = @()) {
    $out = dotnet build $project --nologo --no-incremental -p:TreatWarningsAsErrors=false @extra 2>&1
    if ($LASTEXITCODE -ne 0) { $out | Write-Host; throw 'The consumer project does not build.' }
    return @($out | ForEach-Object { if ("$_" -match '(\w+\.cs)\(\d+,\d+\): warning ((BRO|IDE)\d{4})') { "$($Matches[2]) $($Matches[1])" } } | Sort-Object -Unique)
}

function Migrate([string[]]$arguments) {
    dotnet run --project (Join-Path $root 'src/StyleBro.Migrate') --no-build -- @arguments | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "stylebro-migrate $($arguments -join ' ') failed." }
}

try {
    New-Item -ItemType Directory -Force $feed, (Join-Path $repo 'src/App') | Out-Null
    dotnet pack (Join-Path $root 'src/StyleBro.Package') -c Release -o $feed -p:Version=$version --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'Packing failed.' }
    dotnet build (Join-Path $root 'src/StyleBro.Migrate') --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'Building stylebro-migrate failed.' }

    Set-Content (Join-Path $repo 'nuget.config') @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources><clear /><add key="local" value="$feed" /></packageSources>
</configuration>
"@
    Set-Content $project @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RestorePackagesPath>$(Join-Path $work 'packages')</RestorePackagesPath>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="StyleBro.Analyzers" Version="$version" PrivateAssets="all" />
  </ItemGroup>
</Project>
"@
    $app = Join-Path $repo 'src/App'
    Set-Content (Join-Path $app 'Regions.cs') "namespace App;`n`npublic class Regions`n{`n    #region Values`n`n    public int Value;`n`n    #endregion`n}`n" -NoNewline
    Set-Content (Join-Path $app 'Style.cs') "namespace App;`n`nclass Style`n{`n    static public int Count;`n`n    public string Name = `"`";`n`n    public void M()`n    {`n        int a = 1; int b = 2;`n    }`n}`n" -NoNewline

    Write-Host '== 1. The preset reaches the compiler'
    $warnings = Build
    Check ($warnings -contains 'BRO1106 Style.cs') 'StyleBro rules are reported (BRO1106)'
    Check (-not ($warnings -match '^BRO1112 ')) 'BRO1112, which the preset turns off, is not reported'

    Write-Host '== 2. <StyleBroPreset>none</StyleBroPreset> turns it off'
    Check ((Build @('-p:StyleBroPreset=none')) -contains 'BRO1112 Regions.cs') 'BRO1112 is reported without the preset'

    Write-Host "== 3. init + dotnet format"
    Migrate @('init', $repo, '--write')
    dotnet format $project --severity warn | Out-Host
    $style = Get-Content (Join-Path $app 'Style.cs') -Raw
    Check ($style -match 'internal class Style') 'BRO1404 (preset) added the access modifier'
    Check ($style -match 'public static int Count') 'IDE0036 (from init) fixed the modifier order'
    Check ($style -match 'Name = string\.Empty') 'BRO1106 was fixed'
    Check ($style -match 'int a = 1;\r?\n\s+int b = 2;') "the preset's csharp_preserve_single_line_statements = false applied"
    Check ((Get-Content (Join-Path $app 'Regions.cs') -Raw) -match '#region') 'regions were left alone (BRO1112 is off)'
    dotnet format $project --severity warn --verify-no-changes | Out-Host
    Check ($LASTEXITCODE -eq 0) 'a second dotnet format run changes nothing'

    Write-Host '== 4. Baseline'
    Set-Content (Join-Path $app 'Old.cs') "namespace App;`n`ninternal class Old`n{`n    public string Name = `"`";`n}`n" -NoNewline
    Migrate @('baseline', $repo, '--project', 'src/App/App.csproj')
    Check (Test-Path (Join-Path $repo 'stylebro.baseline')) 'stylebro-migrate baseline wrote stylebro.baseline'
    Check (-not ((Build) -contains 'BRO1106 Old.cs')) 'the baselined violation is not reported'
    Add-Content (Join-Path $app 'Old.cs') "`ninternal class Newer`n{`n    public string Name = `"`";`n}`n"
    Check ((Build) -contains 'BRO1106 Old.cs') 'a new violation in the same file is reported'
}
finally {
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}

if ($failures.Count -gt 0) {
    Write-Host "Package check FAILED: $($failures.Count) problem(s)."
    exit 1
}

Write-Host 'Package check: all good.'
