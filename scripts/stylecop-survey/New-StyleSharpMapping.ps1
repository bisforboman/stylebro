#!/usr/bin/env pwsh
# Writes data/stylesharp-mapping.csv: for every StyleSharp rule (SST ids, data/inventory-stylesharp-<version>.csv), the
# StyleCop rules (SA/SX ids) it stands for, so stylebro-migrate reads a StyleSharp setup like a StyleCop one. StyleSharp
# numbered its StyleCop-style rules like StyleCop's (SST1309 is at SA1309's number), but not every number kept its
# meaning: the exceptions are below, checked against each rule's title and description. An SST rule without a StyleCop
# equivalent maps to nothing (migrate reports it as a rule that goes with StyleSharp).
#
#   ./scripts/stylecop-survey/New-StyleSharpMapping.ps1 [-Version 3.40.1]
param([string]$Version = '3.40.1')
$ErrorActionPreference = 'Stop'

$data = Join-Path $PSScriptRoot 'data'
$styleCop = Import-Csv (Join-Path $data 'inventory-1.2.0-beta.556.csv') | ForEach-Object { $_.Id }
$styleSharp = Import-Csv (Join-Path $data "inventory-stylesharp-$Version.csv")

# The SST rules whose number means something else in StyleCop, or that stand for other StyleCop rules.
$exceptions = [ordered]@{
    # 'Prefer the shortest symbol-equivalent name'; SA1116 (split parameters start on the next line) has no SST rule.
    SST1116 = @('', 'not SA1116: shortens qualified names')
    # stylesharp.instance_member_qualification: require_this is SA1101, omit_this (the default) StyleCop's SX1101.
    SST1117 = @('SA1101 SX1101', "'this.' as stylesharp.instance_member_qualification says: require_this = SA1101, omit_this (default) = SX1101")
    # 'Digit separators should group digits regularly'; SA1119's unnecessary parentheses are wider than SST1459's.
    SST1119 = @('', 'not SA1119: digit separators')
    # SA1214 is static and instance readonly fields; StyleSharp splits it.
    SST1215 = @('SA1214', 'instance half of SA1214 (SST1214 is the static half)')
    # 'Private fields should be _camelCase': StyleCop's SX1309 (instance) and SX1309S (static), the opposite of SA1309.
    SST1309 = @('SX1309 SX1309S', "private fields start with '_' (StyleCop's SX1309/SX1309S, the opposite of SA1309)")
    # Only with file_header_template: a plain header with that text (SA1633 + its text checks, xmlHeader false).
    SST1633 = @('SA1633 SA1635 SA1636', 'a plain header from file_header_template; nothing without it')
}

# SA1117's cases, one id per list kind in StyleSharp.
foreach ($n in 1150..1171) { $exceptions["SST$n"] = @('SA1117', 'one list kind of SA1117') }

$rows = foreach ($rule in $styleSharp) {
    $same = 'SA' + $rule.Id.Substring(3)
    $mapped, $note = if ($exceptions.Contains($rule.Id)) { $exceptions[$rule.Id] }
        elseif ($styleCop -contains $same) { $same, '' }
        else { '', '' }
    [pscustomobject]@{ Id = $rule.Id; StyleCop = $mapped; Note = $note }
}

$path = Join-Path $data 'stylesharp-mapping.csv'
$text = "Id,StyleCop,Note`n" + (($rows | ForEach-Object { '"{0}","{1}","{2}"' -f $_.Id, $_.StyleCop, ($_.Note -replace '"', '""') }) -join "`n") + "`n"
[IO.File]::WriteAllText($path, $text)
"$(@($rows | Where-Object StyleCop).Count) of $($rows.Count) StyleSharp rules map to StyleCop rules -> $path"
