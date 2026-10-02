#!/usr/bin/env pwsh
# Checks StyleBro rules against the StyleCop rules they replace. For each set in parity/parity.psd1, both tools run on
# the same case files: the reported positions are compared, then each tool fixes its own copy and the fixed files
# are compared line by line (and must still compile). StyleBro's fixed files must also be clean: a fix that leaves
# a diagnostic behind would need a second 'dotnet format' run. Every difference must be listed in the set's
# 'Expected' entries, which document StyleBro's deliberate deviations; anything else fails. A set with
# CompareOutput = $false skips the output comparison (for StyleCop rules without a working fix). Optional per set:
# StyleCopJson (stylecop.json for both projects) and EditorConfig (extra .editorconfig lines, e.g. StyleBro settings).
# Several StyleCop rules may map to one StyleBro rule; StyleCop's reports at the same position then count once.
param([string[]]$Set)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$config = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'parity/parity.psd1')
$work = Join-Path ([IO.Path]::GetTempPath()) 'stylebro-parity'
$unknown = @($Set | Where-Object { $_ -notin $config.Sets.Name })
if ($unknown) { throw "Unknown set(s): $($unknown -join ', '). Known: $($config.Sets.Name -join ', ')." }

dotnet build (Join-Path $repo 'src/StyleBro.CodeFixes') --nologo -v q -c Debug | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Building StyleBro failed.' }
$analyzers = @('StyleBro.Analyzers', 'StyleBro.CodeFixes') | ForEach-Object {
    "<Analyzer Include=""$(Join-Path $repo "src/$_/bin/Debug/netstandard2.0/$_.dll")"" />"
}

function New-Project([string]$dir, [string[]]$ids, [string]$cases, [bool]$withStyleBro, [hashtable]$set) {
    if (Test-Path $dir) { Remove-Item $dir -Recurse -Force }
    New-Item $dir -ItemType Directory -Force | Out-Null
    $bro = if ($withStyleBro) { $analyzers -join '' } else { '' }
    if ($set.StyleCopJson) {
        Set-Content (Join-Path $dir 'stylecop.json') $set.StyleCopJson
        $bro += '<AdditionalFiles Include="stylecop.json" />'
    }
    Set-Content (Join-Path $dir 'p.csproj') @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Library</OutputType><Nullable>disable</Nullable><ImplicitUsings>disable</ImplicitUsings><LangVersion>latest</LangVersion><GenerateDocumentationFile>true</GenerateDocumentationFile><NoWarn>CS0168;CS0219;CS8321;CS0660;CS0661;CS1718;CS0665;CS0414;CS0642;CS0164;CS0162;CS1591;CS1587;CS1572;CS1573;CS0067</NoWarn></PropertyGroup>
  <ItemGroup><PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" PrivateAssets="all" />$bro</ItemGroup>
</Project>
"@
    Set-Content (Join-Path $dir '.editorconfig') ("root = true`n[*.cs]`ndotnet_analyzer_diagnostic.category-StyleCop.CSharp.DocumentationRules.severity = none`n" +
        (($ids | ForEach-Object { "dotnet_diagnostic.$_.severity = warning" }) -join "`n") + "`n" + $set.EditorConfig)
    Copy-Item (Join-Path $cases '*.cs') $dir
}

# Compile errors after a fix, plus any of $remainingIds still reported ("File.cs(1,2) ID").
function Get-CompileErrors([string]$dir, [string[]]$remainingIds = @()) {
    @(dotnet build (Join-Path $dir 'p.csproj') -nologo --no-incremental 2>&1 | ForEach-Object {
        if ("$_" -match '(\w+\.cs\(\d+,\d+\)): (error|warning) (\w+):' -and ($Matches[2] -eq 'error' -or $Matches[3] -in $remainingIds)) {
            "$($Matches[1]) $($Matches[3])"
        } } | Sort-Object -Unique)
}
$failed = $false
foreach ($s in $config.Sets) {
    if ($Set -and $s.Name -notin $Set) { continue }
    $pairs = @{}; foreach ($m in $s.Map) { $sa, $bro = $m -split '='; $pairs[$sa] = $bro }
    $cases = Join-Path $PSScriptRoot "parity/$($s.Name)"
    $sc = Join-Path $work "$($s.Name)/stylecop"; $sb = Join-Path $work "$($s.Name)/stylebro"
    New-Project $sc @($pairs.Keys) $cases $false $s
    New-Project $sb @($pairs.Keys) $cases $true $s

    # Positions, from the project that has both analyzers.
    $pattern = '(?<file>\w+\.cs)\((?<pos>\d+,\d+)\): warning (?<id>' + ((@($pairs.Keys) + @($pairs.Values)) -join '|') + '):'
    $a = @(); $b = @()
    foreach ($line in dotnet build (Join-Path $sb 'p.csproj') -nologo --no-incremental 2>&1 | Sort-Object -Unique) {
        if ("$line" -match $pattern) {
            if ($pairs.ContainsKey($Matches.id)) { $a += "$($pairs[$Matches.id]) $($Matches.file)($($Matches.pos))" }
            else { $b += "$($Matches.id) $($Matches.file)($($Matches.pos))" }
        }
    }
    $a = @($a | Sort-Object -Unique)
    $differences = @(Compare-Object ($a | Sort-Object) ($b | Sort-Object) | ForEach-Object {
        '{0} {1}' -f $(if ($_.SideIndicator -eq '<=') { 'only StyleCop:' } else { 'only StyleBro:' }), $_.InputObject })

    # Fixed output.
    dotnet format analyzers (Join-Path $sc 'p.csproj') --diagnostics @($pairs.Keys) --severity warn 2>&1 | Out-Null
    dotnet format analyzers (Join-Path $sb 'p.csproj') --diagnostics @($pairs.Values) --severity warn 2>&1 | Out-Null
    # A diff of the fixed files (not line by line, so a fix that adds lines doesn't shift everything after it).
    foreach ($file in Get-ChildItem $cases -Filter *.cs | Where-Object { $s.CompareOutput -ne $false }) {
        $x = @(Get-Content (Join-Path $sc $file.Name)); $y = @(Get-Content (Join-Path $sb $file.Name))
        $differences += Compare-Object $x $y -CaseSensitive | ForEach-Object {
            '{0} {1}: [{2}]' -f $(if ($_.SideIndicator -eq '<=') { 'StyleCop output only:' } else { 'StyleBro output only:' }), $file.Name, $_.InputObject.Replace("`t", '<tab>')
        }
    }
    foreach ($e in Get-CompileErrors $sb @($pairs.Values)) {
        $differences += if ($e -match ' CS\d+$') { "StyleBro fix doesn't compile: $e" } else { "StyleBro fix leaves: $e" }
    }
    $differences += Get-CompileErrors $sc | ForEach-Object { "StyleCop fix doesn't compile: $_" }

    $expected = @($s.Expected)
    $unexpected = @($differences | Where-Object { $_ -notin $expected })
    $missing = @($expected | Where-Object { $_ -notin $differences })
    $ok = $unexpected.Count -eq 0 -and $missing.Count -eq 0
    if (-not $ok) { $failed = $true }
    '{0} {1}: StyleCop {2} / StyleBro {3} positions, {4} documented differences' -f $(if ($ok) { 'PASS' } else { 'FAIL' }), $s.Name, $a.Count, $b.Count, $expected.Count
    $unexpected | ForEach-Object { "  unexpected: $_" }
    $missing | ForEach-Object { "  expected but not seen: $_" }
}
if ($failed) { exit 1 }
