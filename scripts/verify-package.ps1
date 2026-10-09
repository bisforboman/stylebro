#!/usr/bin/env pwsh
# Checks the NuGet package the way a consumer uses it: packs StyleBro.Analyzers under a throwaway version, restores it
# from a local feed into a project outside the repository, and checks what only the package's build targets do:
#   1. the preset reaches the compiler (BRO1112, which the preset turns off, isn't reported; other rules are);
#   2. <StyleBroPreset>none</StyleBroPreset> turns it off;
#   3. after 'stylebro-migrate init --write', 'dotnet format' fixes the built-in rules too (their severities come from
#      .editorconfig) and applies the preset's formatting options; a second run changes nothing;
#   4. a stylebro.baseline above the project hides its violations, and a new violation is still reported;
#   5. the multi-target guard: after 'init --modernize --write', 'dotnet format' applies the newer-API rules (tier C) in a
#      single-target project and not in a netstandard2.0;net10.0 one, which still builds;
#   6. BRO1145 (C# 12 syntax) isn't reported in a netstandard2.0;net10.0 project unless it sets LangVersion;
#   7. a file compiled from the package folder (a source package's contentFiles) isn't checked.
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

function Build([string[]]$extra = @(), [string]$path = $project) {
    $out = dotnet build $path --nologo --no-incremental -p:TreatWarningsAsErrors=false @extra 2>&1
    if ($LASTEXITCODE -ne 0) { $out | Write-Host; throw 'The consumer project does not build.' }
    return @($out | ForEach-Object { if ("$_" -match '(\w+\.cs)\(\d+,\d+\): warning ((BRO|IDE|CA)\d{4})') { "$($Matches[2]) $($Matches[1])" } } | Sort-Object -Unique)
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
  <packageSources><clear /><add key="local" value="$feed" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources>
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

    Write-Host '== 5. Multi-target guard'
    $multi = Join-Path $repo 'src/Multi'
    New-Item -ItemType Directory -Force $multi | Out-Null
    $multiProject = Join-Path $multi 'Multi.csproj'
    Set-Content $multiProject ((Get-Content $project -Raw).Replace('<TargetFramework>net10.0</TargetFramework>', '<TargetFrameworks>netstandard2.0;net10.0</TargetFrameworks><EnableNETAnalyzers>true</EnableNETAnalyzers><LangVersion>latest</LangVersion>'))
    $modern = "namespace App;`n`ninternal static class Modern`n{`n    public static string Inner(string text)`n    {`n        if (text == null)`n        {`n            throw new System.ArgumentNullException(nameof(text));`n        }`n`n        return text.Substring(1, text.Length - 2);`n    }`n}`n"
    Set-Content (Join-Path $app 'Modern.cs') $modern -NoNewline
    Set-Content (Join-Path $multi 'Modern.cs') $modern -NoNewline
    Migrate @('init', $repo, '--write', '--modernize')
    $without = Build @('-p:StyleBroTargetFrameworks=net10.0', '-p:EnforceCodeStyleInBuild=true') $multiProject
    Check ($without -contains 'CA1510 Modern.cs' -and $without -contains 'IDE0057 Modern.cs') 'without the framework list, CA1510 and IDE0057 are reported in the multi-targeted project'
    Check (-not ((Build @('-p:EnforceCodeStyleInBuild=true') $multiProject) -match '^(CA1510|IDE0057) ')) 'with it, CA1510 and IDE0057 are hidden there'
    dotnet format $project --severity warn | Out-Host
    dotnet format $multiProject --severity warn | Out-Host
    $single = Get-Content (Join-Path $app 'Modern.cs') -Raw
    Check ($single -match 'ArgumentNullException\.ThrowIfNull\(text\)' -and $single -match 'text\[1\.\.\^1\]') 'the single-target project got ThrowIfNull and the range (CA1510, IDE0057)'
    $kept = Get-Content (Join-Path $multi 'Modern.cs') -Raw
    Check ($kept -match 'throw new System\.ArgumentNullException' -and $kept -match 'Substring\(1') 'the multi-targeted project kept its code (netstandard2.0 has neither API)'
    Build @() $multiProject | Out-Null

    Write-Host '== 6. BRO1145 (C# 12) in a multi-targeted project'
    $guard = Join-Path $repo 'src/Guard'
    New-Item -ItemType Directory -Force $guard | Out-Null
    $guardProject = Join-Path $guard 'Guard.csproj'
    Set-Content $guardProject ((Get-Content $project -Raw).Replace('<TargetFramework>net10.0</TargetFramework>', '<TargetFrameworks>netstandard2.0;net10.0</TargetFrameworks>'))
    Set-Content (Join-Path $guard '.editorconfig') "[*.cs]`ndotnet_diagnostic.BRO1145.severity = warning`n"
    Set-Content (Join-Path $guard 'Marker.cs') "namespace App`n{`n    public class Marker`n    {`n    }`n}`n" -NoNewline
    Check (-not ((Build @() $guardProject) -contains 'BRO1145 Marker.cs')) 'without LangVersion, netstandard2.0;net10.0 gets no BRO1145'
    Check ((Build @('-p:LangVersion=12') $guardProject) -contains 'BRO1145 Marker.cs') 'with LangVersion set, it is reported'
    Check ((Build @('-p:StyleBroTargetFrameworks=net10.0') $guardProject) -contains 'BRO1145 Marker.cs') 'with one framework, it is reported'

    Write-Host '== 7. Files from the package folder (a source package) are not checked'
    $vendored = Join-Path $repo 'src/Vendored'
    $source = Join-Path $work 'packages/fake.sources/1.0.0/contentFiles/cs/any'
    New-Item -ItemType Directory -Force $vendored, $source | Out-Null
    Set-Content (Join-Path $source 'Vendored.cs') "namespace App;`n`npublic class Vendored`n{`n    public string Name = `"`";`n}`n" -NoNewline
    $vendoredProject = Join-Path $vendored 'Vendored.csproj'
    Set-Content $vendoredProject ((Get-Content $project -Raw).Replace('</Project>', "  <ItemGroup><Compile Include=`"$(Join-Path $source 'Vendored.cs')`" /></ItemGroup>`n</Project>"))
    Check (-not ((Build @() $vendoredProject) -contains 'BRO1106 Vendored.cs')) 'BRO1106 is not reported in a file under the package folder'
    Check ((Build @('-p:StyleBroPackageFolders=C:/elsewhere') $vendoredProject) -contains 'BRO1106 Vendored.cs') 'with other package folders, it is reported'
}
finally {
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}

if ($failures.Count -gt 0) {
    Write-Host "Package check FAILED: $($failures.Count) problem(s)."
    exit 1
}

Write-Host 'Package check: all good.'
