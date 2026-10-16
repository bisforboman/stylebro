#!/usr/bin/env pwsh
# Checks the NuGet package the way a consumer uses it: packs StyleBro.Analyzers under a throwaway version, restores it
# from a local feed into a project outside the repository, and checks what only the package's build targets do:
#   1. the preset reaches the compiler (BRO1112, which the preset turns off, isn't reported; other rules are);
#   2. <StyleBroPreset>none</StyleBroPreset> turns it off;
#   3. 'stylebro-migrate init --diff' previews without writing; after 'init --write', 'dotnet format' fixes the built-in rules too (their severities come from
#      .editorconfig) and applies the preset's formatting options; a second run changes nothing;
#   4. a stylebro.baseline above the project hides its violations, and a new violation is still reported;
#   5. the multi-target guard: after 'init --modernize --write', 'dotnet format' applies the newer-API rules (tier C) in a
#      single-target project and not in a netstandard2.0;net10.0 one, which still builds;
#   6. BRO1145 (C# 12 syntax) isn't reported in a netstandard2.0;net10.0 project unless it sets LangVersion;
#   7. a file compiled from the package folder (a source package's contentFiles) isn't checked;
#   8. a rename the fix keeps on purpose (the name read by reflection in another file): 'stylebro-migrate format' lists it
#      with its reason, and its --verify-no-changes exits 2 while something is left to fix, then 0 with only it left;
#      with --files and --json, the JSON on stdout has it with its reason;
#   9. preset versions: 'init --write' pinned <StyleBroPreset>; unset, the newest version and 'latest' give the same
#      findings while the package has one layer; a version newer than the package warns (STYLEBRO002), an invalid one
#      fails the build (STYLEBRO001). Once a minor adds a layer: also check that its rule is off by default and on with it;
#  10. the Microsoft style: <StyleBroStyle>microsoft</StyleBroStyle> accepts '_'/'s_' fields, orders fields first and keeps
#      rules runtime has off off; an unknown style fails (STYLEBRO003), one with StyleBroPreset none warns (STYLEBRO004);
#      after 'init --write --style microsoft', 'dotnet format' removes 'this.' (IDE0003) and a second run changes nothing.
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
    # init follows the code's conventions (3 or more places): most empty strings are string.Empty, so BRO1106 stays on.
    Set-Content (Join-Path $app 'Empties.cs') ("namespace App;`n`ninternal class Empties`n{`n" + (-join (0..9 | ForEach-Object { "    public string A$_ = string.Empty;`n" })) + "}`n") -NoNewline
    Set-Content (Join-Path $app 'Style.cs')"namespace App;`n`nclass Style`n{`n    static public int Count;`n`n    public string Name = `"`";`n`n    public void M()`n    {`n        int a = 1; int b = 2;`n    }`n}`n" -NoNewline

    Write-Host '== 1. The preset reaches the compiler'
    $warnings = Build
    Check ($warnings -contains 'BRO1106 Style.cs') 'StyleBro rules are reported (BRO1106)'
    Check (-not ($warnings -match '^BRO1112 ')) 'BRO1112, which the preset turns off, is not reported'

    Write-Host '== 2. <StyleBroPreset>none</StyleBroPreset> turns it off'
    Check ((Build @('-p:StyleBroPreset=none')) -contains 'BRO1112 Regions.cs') 'BRO1112 is reported without the preset'

    Write-Host "== 3. init --diff previews, then init + dotnet format"
    $patch = Join-Path $work 'preview.patch'
    Migrate @('init', $repo, "--diff=$patch", '--project', 'src/App/App.csproj')
    $previewed = Get-Content $patch -Raw
    Check ($previewed -match '\+\+\+ b/\.editorconfig' -and $previewed -match '\+internal class Style') 'init --diff wrote the settings and the format fixes (BRO1404) to the patch'
    Check (-not (Test-Path (Join-Path $repo '.editorconfig'))) 'init --diff left the repository alone'
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
    Check ((Get-Content (Join-Path $repo 'Directory.Build.props') -Raw) -match '<StyleBroPreset>\d+\.\d+</StyleBroPreset>') 'init pinned the preset version in Directory.Build.props'

    Write-Host '== 4. Baseline'
    Set-Content (Join-Path $app 'Old.cs') "namespace App;`n`ninternal class Old`n{`n    public string Name = `"`";`n}`n" -NoNewline
    Migrate @('baseline', $repo, '--project', 'src/App/App.csproj')
    Check (Test-Path (Join-Path $repo 'stylebro.baseline')) 'stylebro-migrate baseline wrote stylebro.baseline'
    Check (-not ((Build) -contains 'BRO1106 Old.cs')) 'the baselined violation is not reported'
    # A relative <StyleBroBaseline> (docs/baseline.md's form, eng/ next to src/) is found above the project too (trial 25).
    $eng = Join-Path $repo 'eng'
    New-Item -ItemType Directory -Force $eng | Out-Null
    $baselineText = Get-Content (Join-Path $repo 'stylebro.baseline') -Raw
    Remove-Item (Join-Path $repo 'stylebro.baseline')
    Set-Content (Join-Path $eng 'stylebro.baseline') $baselineText.Replace("`tsrc/", "`t../src/") -NoNewline
    # In a props file, as the docs show (a global -p: property can't be changed by the targets).
    $props = Join-Path $repo 'Directory.Build.props'
    $propsText = Get-Content $props -Raw
    Set-Content $props $propsText.Replace('</Project>', "  <PropertyGroup><StyleBroBaseline>eng/stylebro.baseline</StyleBroBaseline></PropertyGroup>`n</Project>") -NoNewline
    Check (-not ((Build) -contains 'BRO1106 Old.cs')) 'a relative StyleBroBaseline is found in a folder above the project'
    Set-Content $props $propsText -NoNewline
    Remove-Item (Join-Path $eng 'stylebro.baseline')
    Set-Content (Join-Path $repo 'stylebro.baseline') $baselineText -NoNewline
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

    Write-Host '== 8. Findings kept on purpose'
    $keptRepo = Join-Path $work 'kept'
    New-Item -ItemType Directory -Force $keptRepo | Out-Null
    Copy-Item (Join-Path $repo 'nuget.config'), (Join-Path $repo '.editorconfig') $keptRepo
    Set-Content (Join-Path $keptRepo 'Kept.csproj') (Get-Content $project -Raw)
    Set-Content (Join-Path $keptRepo 'Counter.cs') "namespace App;`n`ninternal class Counter`n{`n    private int _count;`n    private int _total;`n`n    public int Next() => ++_count + _total++;`n}`n" -NoNewline
    Set-Content (Join-Path $keptRepo 'Reader.cs') "namespace App;`n`ninternal static class Reader`n{`n    public static object? Read(Counter counter) => typeof(Counter).GetField(`"_count`", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(counter);`n}`n" -NoNewline
    $migrate = @('run', '--project', (Join-Path $root 'src/StyleBro.Migrate'), '--no-build', '--', 'format', $keptRepo, '--severity', 'warn')
    dotnet @migrate --verify-no-changes | Out-Host
    Check ($LASTEXITCODE -eq 2) 'format --verify-no-changes exits 2 while _total can be renamed'
    $formatted = dotnet @migrate 2>&1 | Out-String
    $formatted | Out-Host
    Check ($LASTEXITCODE -eq 0 -and $formatted -match "Counter\.cs\(5,17\): BRO1303 Rename '_count' to 'count'\. Kept: the name is in a string") 'format lists _count as kept on purpose, with the reason'
    Check ((Get-Content (Join-Path $keptRepo 'Counter.cs') -Raw) -match 'private int _count;\s+private int total;') 'format renamed _total and kept _count'
    # The clean run baselined the kept finding: the build and verify skip it (trial 23, owner's choice).
    $baselineFile = Join-Path $keptRepo 'stylebro.baseline'
    Check ($formatted -match 'Baseline: stylebro\.baseline: added 1 kept finding' -and (Test-Path $baselineFile) -and (Get-Content $baselineFile -Raw) -match "BRO1303`tCounter\.cs`t") 'format baselines the kept finding'
    $verified = dotnet @migrate --verify-no-changes 2>&1 | Out-String
    $verified | Out-Host
    Check ($LASTEXITCODE -eq 0 -and $verified -match 'Clean') 'format --verify-no-changes exits 0 with the kept finding baselined'
    dotnet build $keptRepo -nologo -v q -p:TreatWarningsAsErrors=true -p:Nullable=enable 2>&1 | Out-Host
    Check ($LASTEXITCODE -eq 0) 'the build with warnings as errors passes with the kept finding baselined'

    # Without the baseline, verify lists the kept finding with its reason.
    Remove-Item $baselineFile
    $json = dotnet @migrate --verify-no-changes --files (Join-Path $keptRepo 'Counter.cs') --json 2>$null | Out-String | ConvertFrom-Json
    Check ($LASTEXITCODE -eq 0 -and $json.clean -and $json.keptFindings[0].rule -eq 'BRO1303' -and $json.keptFindings[0].reason -eq 'NameInString') 'format --files --json reports the kept finding with its reason'

    Write-Host '== 9. Preset versions'
    $latest = [regex]::Match((Get-Content (Join-Path $root 'src/StyleBro.Package/build/StyleBro.Analyzers.targets') -Raw), '<_StyleBroPresetLatest>([\d.]+)<').Groups[1].Value
    $default = Build @('-p:StyleBroPreset=')
    Check (-not ($default -match '^BRO1112 ') -and ((Build @('-p:StyleBroPreset=none')) -contains 'BRO1112 Regions.cs')) 'unset: the preset applies (BRO1112 off; on with none)'
    Check (((Build @("-p:StyleBroPreset=$latest")) -join ',') -eq ($default -join ',')) "StyleBroPreset=$latest gives the same findings"
    Check (((Build @('-p:StyleBroPreset=latest')) -join ',') -eq ($default -join ',')) 'StyleBroPreset=latest gives the same findings'
    $newer = dotnet build $project --nologo --no-incremental -p:TreatWarningsAsErrors=false -p:StyleBroPreset=99.0 2>&1 | Out-String
    Check ($LASTEXITCODE -eq 0 -and $newer -match 'warning STYLEBRO002') 'a version newer than the package warns (STYLEBRO002) and builds'
    $invalid = dotnet build $project --nologo --no-incremental -p:StyleBroPreset=bogus 2>&1 | Out-String
    Check ($LASTEXITCODE -ne 0 -and $invalid -match 'error STYLEBRO001') 'an invalid value fails the build (STYLEBRO001)'

    Write-Host '== 10. The Microsoft style'
    $styleRepo = Join-Path $work 'style'
    $styleProject = Join-Path $styleRepo 'src/StyleApp/StyleApp.csproj'
    New-Item -ItemType Directory -Force (Split-Path $styleProject) | Out-Null
    Copy-Item (Join-Path $repo 'nuget.config') $styleRepo
    Set-Content $styleProject (Get-Content $project -Raw) -NoNewline
    $holder = Join-Path (Split-Path $styleProject) 'Holder.cs'
    Set-Content $holder "namespace StyleApp;`n`ninternal sealed class Holder`n{`n    public int Count => _count + s_total;`n`n    private static int s_total;`n    private int _count;`n    public int Next()`n    {`n        return this._count + 1;`n    }`n}`n" -NoNewline
    $plain = Build @() $styleProject
    $styled = Build @('-p:StyleBroStyle=microsoft') $styleProject
    Check (($plain -contains 'BRO1303 Holder.cs') -and -not ($styled -match '^BRO130[367] ')) "the style's '_' and 's_' fields are accepted (BRO1303/BRO1306/BRO1307 without it)"
    Check (($styled -contains 'BRO1001 Holder.cs') -and ($plain -contains 'BRO1505 Holder.cs') -and -not ($styled -match '^BRO1505 ')) 'fields first (BRO1001), and rules runtime has off stay off (BRO1505)'
    $unknown = dotnet build $styleProject --nologo --no-incremental -p:StyleBroStyle=google 2>&1 | Out-String
    Check ($LASTEXITCODE -ne 0 -and $unknown -match 'error STYLEBRO003') 'an unknown style fails the build (STYLEBRO003)'
    $ignored = dotnet build $styleProject --nologo --no-incremental -p:TreatWarningsAsErrors=false -p:StyleBroStyle=microsoft -p:StyleBroPreset=none 2>&1 | Out-String
    Check ($LASTEXITCODE -eq 0 -and $ignored -match 'warning STYLEBRO004') 'the style with StyleBroPreset none warns (STYLEBRO004)'
    Migrate @('init', $styleRepo, '--write', '--style', 'microsoft')
    Check ((Get-Content (Join-Path $styleRepo 'Directory.Build.props') -Raw) -match '<StyleBroStyle>microsoft</StyleBroStyle>') 'init --style microsoft pins the style'
    dotnet format $styleProject | Out-Host
    $fixed = Get-Content $holder -Raw
    Check ($fixed -match 'return _count \+ 1;' -and $fixed -match '(?s)private int _count;.*public int Count') "format removes 'this.' (IDE0003) and puts the fields first"
    dotnet format $styleProject --verify-no-changes | Out-Host
    Check ($LASTEXITCODE -eq 0) 'a second format run changes nothing'
}
finally {
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}

if ($failures.Count -gt 0) {
    Write-Host "Package check FAILED: $($failures.Count) problem(s)."
    exit 1
}

Write-Host 'Package check: all good.'
# The last check's build failed on purpose; its exit code isn't the script's.
exit 0
