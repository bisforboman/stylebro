#!/usr/bin/env pwsh
# Checks that StyleBro's fixes for Sonar's own ids (S1066, S2971, S3878; registered by BRO1149-BRO1151's fix providers)
# work with the real SonarAnalyzer.CSharp package under 'dotnet format'. The unit tests (SonarIdsTests) use a stub that
# reports the ids where Sonar 10.35.0.4138 does; this script catches Sonar moving them. A project OUTSIDE the repository
# references the pinned Sonar package (never a dependency of StyleBro's packages) and the built StyleBro DLLs; then:
#   1. 'dotnet format analyzers --diagnostics S1066 S2971 S3878' gives exactly the expected text;
#   2. the build reports Sonar's warnings only at the places StyleBro skips;
#   3. a second run changes nothing.
# Usage: pwsh scripts/sonar-interop.ps1 [-SonarVersion 10.35.0.4138]
param([string]$SonarVersion = '10.35.0.4138')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$work = Join-Path ([IO.Path]::GetTempPath()) "stylebro-sonar-$([Guid]::NewGuid().ToString('N').Substring(0, 8))"
$failures = [Collections.Generic.List[string]]::new()

function Check([bool]$ok, [string]$what) {
    if ($ok) { Write-Host "  ok: $what" } else { Write-Host "  FAILED: $what"; $failures.Add($what) }
}

$source = @'
using System.Collections.Generic;
using System.Linq;

public class C
{
    public static int Sum(int first, params int[] rest) => first + rest.Length;

    public static int Count(params object[] values) => values.Length;

    public int M(List<int> items, bool a, bool b)
    {
        if (a)
        {
            if (b)
            {
                return Sum(1, new[] { 2, 3 }) + Count(new string[] { "x" });
            }
        }

        if (a)
        {
            // Kept: a comment in the removed text.
            if (b)
            {
                return 1;
            }
        }

        return items.Where(i => i > 0).Count() + items.Count();
    }
}
'@
$expected = @'
using System.Collections.Generic;
using System.Linq;

public class C
{
    public static int Sum(int first, params int[] rest) => first + rest.Length;

    public static int Count(params object[] values) => values.Length;

    public int M(List<int> items, bool a, bool b)
    {
        if (a && b)
        {
            return Sum(1, 2, 3) + Count(new string[] { "x" });
        }

        if (a)
        {
            // Kept: a comment in the removed text.
            if (b)
            {
                return 1;
            }
        }

        return items.Count(i => i > 0) + items.Count();
    }
}
'@
# Sonar's warnings that stay: the commented 'if', 'Count()' on a list (a property exists), the covariant array.
$kept = @('S1066 (20,13)', 'S2971 (26,48)', 'S3878 (14,41)')

try {
    dotnet build (Join-Path $root 'src/StyleBro.CodeFixes') -c Release -p:StyleBroSelf=none --nologo -v q | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'StyleBro.CodeFixes does not build.' }
    $bin = Join-Path $root 'src/StyleBro.CodeFixes/bin/Release/netstandard2.0'
    New-Item -ItemType Directory -Force (Join-Path $work 'dll'), (Join-Path $work 'App') | Out-Null
    Copy-Item (Join-Path $bin 'StyleBro.Analyzers.dll'), (Join-Path $bin 'StyleBro.CodeFixes.dll') (Join-Path $work 'dll')
    Set-Content (Join-Path $work 'App/App.csproj') @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="SonarAnalyzer.CSharp" Version="$SonarVersion" PrivateAssets="all" />
    <Analyzer Include="../dll/StyleBro.Analyzers.dll" />
    <Analyzer Include="../dll/StyleBro.CodeFixes.dll" />
  </ItemGroup>
</Project>
"@
    $file = Join-Path $work 'App/C.cs'
    [IO.File]::WriteAllText($file, $source.Replace("`r`n", "`n") + "`n")
    $project = Join-Path $work 'App/App.csproj'

    Write-Host "SonarAnalyzer.CSharp $SonarVersion"
    foreach ($run in 1, 2) {
        dotnet format analyzers $project --diagnostics S1066 S2971 S3878 --severity info | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "dotnet format (run $run) failed." }
        $text = [IO.File]::ReadAllText($file).Replace("`r`n", "`n")
        Check ($text -eq $expected.Replace("`r`n", "`n") + "`n") "run $run gives the expected text"
        if ($text -ne $expected.Replace("`r`n", "`n") + "`n") { Write-Host $text }
    }

    $out = dotnet build $project --nologo --no-incremental 2>&1
    if ($LASTEXITCODE -ne 0) { $out | Write-Host; throw 'The fixed project does not build.' }
    $left = @($out | ForEach-Object { if ("$_" -match 'C\.cs(\(\d+,\d+\)): warning (S1066|S2971|S3878)') { "$($Matches[2]) $($Matches[1])" } } | Sort-Object -Unique)
    Check (($left -join ', ') -eq ($kept -join ', ')) "Sonar's warnings left only where StyleBro skips: $($left -join ', ')"
}
finally {
    Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}

if ($failures.Count -gt 0) { throw "$($failures.Count) check(s) failed." }
Write-Host 'Sonar interop OK.'
