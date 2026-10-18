#!/usr/bin/env pwsh
# Checks StyleBro rules against the StyleCop rules they replace. For each set in parity/parity.psd1, both tools run on
# the same case files: the reported positions are compared, then each tool fixes its own copy and the fixed files
# are compared line by line (and must still compile). StyleBro's fixed files must also be clean: a fix that leaves
# a diagnostic behind would need a second 'dotnet format' run. Every difference must be listed in the set's
# 'Expected' entries, which document StyleBro's deliberate deviations; anything else fails. A set with
# CompareOutput = $false skips the output comparison (for StyleCop rules without a working fix). Optional per set:
# StyleCopJson (stylecop.json for both projects) and EditorConfig (extra .editorconfig lines, e.g. StyleBro settings).
# Several StyleCop rules may map to one StyleBro rule; StyleCop's reports at the same position then count once.
#
# The 'Expected' entries are written against StyleCop 1.2.0-beta.556 (the last release). To compare with another build,
# e.g. StyleCop's unreleased master, pass its package: -StyleCopPackage/-StyleCopVersion and -StyleCopFeed (a folder
# with the .nupkg; restored into an isolated folder so the odd version stays out of the global cache). For master:
#   git clone --filter=blob:none https://github.com/DotNetAnalyzers/StyleCopAnalyzers sc
#   git -C sc -c core.longpaths=true checkout -f origin/master      # plain checkout fails on Windows (long paths)
#   dotnet build sc/StyleCop.Analyzers/StyleCop.Analyzers.CodeFixes -c Release -p:NBGV_GitEngine=Disabled  # SDK 6
#   pwsh scripts/stylecop-survey/Compare-WithStyleCop.ps1 -StyleCopPackage StyleCop.Analyzers.Unstable `
#       -StyleCopVersion 1.2.0-g -StyleCopFeed <folder with StyleCop.Analyzers.Unstable.1.2.0-g.nupkg>
# Differences that are expected against master but not beta.556 then show up as failures; the rule pages and
# docs/differences-from-stylecop.md ("Compared with StyleCop's unreleased master") say which are deliberate.
#
# -StyleSharp runs StyleSharp.Analyzers (a StyleCop successor, SST ids) instead, as bro migrate reads it: each SA id
# of a set stands for the SST ids data/stylesharp-mapping.csv maps to it, stylecop.json is left out (StyleSharp doesn't
# read it) and StyleBro gets the settings migrate writes for a StyleSharp repository ($styleSharpSettings below). The
# 'Expected' entries are StyleCop's, so this mode only lists the differences (a probe, it never fails); docs/migrating.md
# ("StyleSharp") says which are known.
#   pwsh scripts/stylecop-survey/Compare-WithStyleCop.ps1 -StyleSharp [-StyleCopVersion 5.1.2] [-Set braces]
param(
    [string[]]$Set,
    [string]$StyleCopPackage = 'StyleCop.Analyzers',
    [string]$StyleCopVersion = '1.2.0-beta.556',
    [string]$StyleCopFeed,
    [switch]$StyleSharp)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$config = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'parity/parity.psd1')
# What bro migrate writes for a StyleSharp repository with StyleSharp's defaults (SST1309: '_camelCase').
$styleSharpSettings = @'
stylebro_private_field_naming = _camelCase
stylebro_summary_wording = stylesharp
stylebro_member_order = field, constructor, finalizer, delegate, event, enum, interface, property, indexer, conversion|operator|extension|method, struct, class, record
stylebro_allowed_hungarian_prefixes = db, ef, gc, id, io, js, ms, my, ok, so, ui, up, us
stylebro_trailing_comma_switch_expressions = false
stylebro_trailing_comma_with_expressions = false
stylebro_allow_semicolon_after_closing_brace = true
stylebro_member_order_namespaces = false
stylebro_allow_empty_single_line_blocks = true
stylebro_exclude_from_punctuation_check = seealso, param, typeparam, exception, see, paramref, typeparamref, c, a, b, i, u, em, strong, sub, sup, span, tt
dotnet_diagnostic.BRO1312.severity = none
'@
$fromStyleCop = @{}
if ($StyleSharp) {
    if ($StyleCopPackage -eq 'StyleCop.Analyzers') { $StyleCopPackage = 'StyleSharp.Analyzers' }
    if ($StyleCopVersion -eq '1.2.0-beta.556') { $StyleCopVersion = '5.1.2' }
    foreach ($row in Import-Csv (Join-Path $PSScriptRoot 'data/stylesharp-mapping.csv')) {
        foreach ($sa in $row.StyleCop -split ' ' | Where-Object { $_ }) { $fromStyleCop[$sa] = @($fromStyleCop[$sa]) + $row.Id | Where-Object { $_ } }
    }
}
$work = Join-Path ([IO.Path]::GetTempPath()) $(if ($StyleSharp) { 'stylebro-parity-stylesharp' } else { 'stylebro-parity' })
$restore = if ($StyleCopFeed) {
    "<RestoreAdditionalProjectSources>$((Resolve-Path $StyleCopFeed).Path)</RestoreAdditionalProjectSources><RestorePackagesPath>$(Join-Path $work 'packages')</RestorePackagesPath>"
} else { '' }
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
    if ($set.StyleCopJson -and -not $StyleSharp) {
        Set-Content (Join-Path $dir 'stylecop.json') $set.StyleCopJson
        $bro += '<AdditionalFiles Include="stylecop.json" />'
    }
    Set-Content (Join-Path $dir 'p.csproj') @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Library</OutputType><Nullable>disable</Nullable><ImplicitUsings>disable</ImplicitUsings><LangVersion>latest</LangVersion><GenerateDocumentationFile>true</GenerateDocumentationFile><NoWarn>CS0168;CS0219;CS8321;CS0660;CS0661;CS1718;CS0665;CS0414;CS0642;CS0164;CS0162;CS1591;CS1587;CS1572;CS1573;CS0067</NoWarn>$restore</PropertyGroup>
  <ItemGroup><PackageReference Include="$StyleCopPackage" Version="$StyleCopVersion" PrivateAssets="all" />$bro</ItemGroup>
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
    $pairs = @{}; foreach ($m in $s.Map) { $sa, $bro = $m -split '='; foreach ($id in $(if ($StyleSharp) { $fromStyleCop[$sa] } else { $sa })) { $pairs[$id] = $bro } }
    if ($pairs.Count -eq 0) { "SKIP $($s.Name): no StyleSharp rule stands for $($s.Map -join ', ')"; continue }
    if ($StyleSharp) { $s = $s.Clone(); $s.EditorConfig = "$($s.EditorConfig)`n$styleSharpSettings`n" }
    $cases = Join-Path $PSScriptRoot "parity/$($s.Name)"
    $sc = Join-Path $work "$($s.Name)/stylecop"; $sb = Join-Path $work "$($s.Name)/stylebro"
    # The BRO ids are set too, for rules that are off by default (BRO1310).
    New-Project $sc @($pairs.Keys) $cases $false $s
    New-Project $sb (@($pairs.Keys) + @($pairs.Values | Sort-Object -Unique)) $cases $true $s

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
    # An empty side would fail to bind: both get one empty entry, which never differs.
    $differences = @(Compare-Object (@('') + @($a | Sort-Object)) (@('') + @($b | Sort-Object)) | ForEach-Object {
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

    if ($StyleSharp) {
        # Positions differ by convention (StyleSharp reports an 'if' at its keyword, StyleCop at the statement), so this mode
        # checks behaviour: StyleBro's fix must leave nothing StyleSharp reports (else StyleSharp's build breaks while both
        # run), and code StyleSharp's own fix made clean should give StyleBro nothing to change (else migrating churns).
        # A finding as rule, file and line text: fixes move lines, so positions before and after can't be compared.
        function Get-Keyed([string]$dir, [string[]]$ids) {
            @(Get-CompileErrors $dir $ids | Where-Object { $_ -match '^(?<file>\w+\.cs)\((?<line>\d+),\d+\) (?<id>[A-Z]+\d+)$' -and $Matches.id -notlike 'CS*' } | ForEach-Object {
                $null = $_ -match '^(?<file>\w+\.cs)\((?<line>\d+),\d+\) (?<id>\w+)$'
                '{0} {1}: {2}' -f $Matches.id, $Matches.file, @(Get-Content (Join-Path $dir $Matches.file))[[int]$Matches.line - 1].Trim()
            })
        }
        $clean = Join-Path $work "$($s.Name)/clean"
        New-Project $clean (@($pairs.Keys) + @($pairs.Values | Sort-Object -Unique)) $cases $true $s
        $before = Get-Keyed $clean @($pairs.Keys)
        $breaks = @(Get-Keyed $sb @($pairs.Keys) | Where-Object { $_ -notin $before })
        Copy-Item (Join-Path $sc '*.cs') $clean -Force
        $left = Get-Keyed $clean (@($pairs.Keys) + @($pairs.Values))
        $unfixed = @($left | Where-Object { $_ -like 'SST*' } | ForEach-Object { $_.Substring($_.IndexOf(' ')) })
        $churn = @($left | Where-Object { $_ -like 'BRO*' -and $_.Substring($_.IndexOf(' ')) -notin $unfixed })
        '{0}: StyleSharp {1} / StyleBro {2} positions; new StyleSharp findings after StyleBro''s fix: {3}; StyleBro on StyleSharp-clean code: {4}' -f $s.Name, $a.Count, $b.Count, $breaks.Count, $churn.Count
        $breaks | ForEach-Object { "  new StyleSharp finding after StyleBro's fix: $_" }
        $churn | ForEach-Object { "  StyleBro on StyleSharp-clean code: $_" }
        continue
    }
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
