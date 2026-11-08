#!/usr/bin/env pwsh
# Generates docs/stylecop-mapping.md (the published ID mapping), docs/skipped-rules.md (the published list of rules
# StyleBro doesn't cover) and survey.md (internal: the full survey, with StyleCop's defaults, fixes, the teams and the
# counts) from data/ (inventory, repo surveys, SDK check), repos.psd1, decisions.psd1 and skipped.psd1.
# The published pages say what StyleBro and the SDK cover, never what StyleCop does (see docs/decisions/).
param(
    [string]$Out = (Join-Path $PSScriptRoot '../../docs/stylecop-mapping.md'),
    [string]$SkippedOut = (Join-Path $PSScriptRoot '../../docs/skipped-rules.md'),
    [string]$SurveyOut = (Join-Path $PSScriptRoot 'survey.md'))
$ErrorActionPreference = 'Stop'
$data = Join-Path $PSScriptRoot 'data'
$inv = Import-Csv (Join-Path $data 'inventory-1.3.177-beta.csv')
$in118 = (Import-Csv (Join-Path $data 'inventory-1.1.118.csv')).Id
$in12 = (Import-Csv (Join-Path $data 'inventory-1.2.0-beta.556.csv')).Id
$repos = (Import-PowerShellDataFile (Join-Path $PSScriptRoot 'repos.psd1')).Repos
$decisions = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'decisions.psd1')
$skipped = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'skipped.psd1')
$check = @{}; Import-Csv (Join-Path $data 'sdk-check.csv') | ForEach-Object { $check[$_.Id] = $_.Verdict }
$invById = @{}; $inv | ForEach-Object { $invById[$_.Id] = $_ }

# Per repo: the effective production setting of each rule ('on', 'info' or 'off') and the counts with every rule on.
$state = @{}; $counts = @{}
foreach ($repo in $repos) {
    $settings = Import-Csv (Join-Path $data "$($repo.Name)-configs.csv")
    $s = @{}
    foreach ($r in $inv) { $s[$r.Id] = if ($r.EnabledByDefault -eq 'True') { 'on' } else { 'off' } }
    foreach ($file in $repo.ProdConfigs) {  # lowest precedence first, so later files win
        foreach ($setting in $settings | Where-Object File -eq $file) {
            $s[$setting.Id] = switch ($setting.Setting) {
                { $_ -in 'none', 'silent', 'hidden' } { 'off' }
                { $_ -in 'suggestion', 'info' } { 'info' }
                default { 'on' }
            }
        }
    }
    $state[$repo.Name] = $s
    $c = @{}; Import-Csv (Join-Path $data "$($repo.Name)-counts.csv") | ForEach-Object { $c[$_.Id] = [int]$_.Diagnostics }
    $counts[$repo.Name] = $c
}
$labels = ($repos | ForEach-Object Label) -join ' / '
$primary = $repos[-1].Name  # the candidates table breaks ties by the last repo's counts

function Get-Proposal($r) {
    if ($decisions.Proposals.ContainsKey($r.Id)) { return $decisions.Proposals[$r.Id] }
    if ($skipped.Skipped[$r.Id] -is [hashtable]) {
        $s = $skipped.Skipped[$r.Id]
        return $(switch ($s.Status) {
            'Candidate' { 'Not yet: StyleBro candidate. ' + $s.Why }
            'SdkLater' { 'Not yet: the SDK can fix it, but it isn''t turned on. ' + $s.Why }
            'Drop' { 'Drop: ' + $s.Why }
            'NotInStyleCop' { 'Drop: ' + $s.Why }
            'NotApplicable' { 'Not applicable: ' + $s.Why }
            'Variant' { 'Variant: ' + $s.Why }
            default { throw "skipped.psd1: unknown status '$($s.Status)' for $($r.Id)" }
        })
    }
    if ($check[$r.Id] -eq 'fixed') { return 'SDK: ' + $decisions.SdkSettings[$r.Id] }
    if ($r.Id -notmatch '^SA\d{4}$') { return 'Variant of another rule (not in 1.1.118 as a separate rule)' }
    if ($r.Category -eq 'DocumentationRules') {
        if ($r.StyleCopHasFix -eq 'True') { return 'Untested. BRO16xx candidate (StyleCop has a fix)' }
        return 'Untested. Likely drop: the fix would need human-written text'
    }
    if ($r.StyleCopHasFix -eq 'True') { return 'Untested. StyleCop has a fix, so a StyleBro fix is feasible' }
    return 'Untested. StyleCop has no fix'
}
function Get-Kept($id) { if ($id -notin $in12) { return 'not surveyed' }; ($repos | ForEach-Object { $state[$_.Name][$id] }) -join ' / ' }
# The surveys ran StyleCop 1.2: a rule 1.3 added wasn't counted.
function Get-Counts($id) { if ($id -notin $in12) { return 'not surveyed' }; ($repos | ForEach-Object { ([int]$counts[$_.Name][$id]).ToString('n0', [cultureinfo]::InvariantCulture) }) -join ' / ' }
function Get-Check($id) { switch ($check[$id]) { 'fixed' { 'fixed' } 'not-fixed' { 'not fixed' } 'partly' { 'partly' } default { '' } } }

$rows = foreach ($r in $inv) {
    [pscustomobject]@{
        Rule = $r
        Proposal = Get-Proposal $r
        KeptCount = @($repos | Where-Object { $state[$_.Name][$r.Id] -eq 'on' }).Count
        PrimaryCount = [int]$counts[$primary][$r.Id]
    }
}
$tally = [ordered]@{
    SDK = @($rows | Where-Object Proposal -like 'SDK:*').Count
    StyleBro = @($rows | Where-Object Proposal -like 'StyleBro*').Count
    Done = @($rows | Where-Object Proposal -like '*(done*').Count
    Drop = @($rows | Where-Object Proposal -like 'Drop*').Count
    NotYet = @($rows | Where-Object Proposal -like 'Not yet*').Count
    Untested = @($rows | Where-Object Proposal -like 'Untested*').Count
}
$other = $rows.Count - $tally.SDK - $tally.StyleBro - $tally.Drop - $tally.NotYet - $tally.Untested
foreach ($row in $rows | Where-Object { $_.Proposal -like 'Untested*' -or ($_.Proposal -like 'Variant of*') }) {
    Write-Warning "$($row.Rule.Id) is neither covered nor in skipped.psd1"
}
$statusShort = @{ Candidate = 'candidate'; SdkLater = 'SDK not on yet'; Drop = 'drop'; NotInStyleCop = 'nothing to replace'; Variant = 'variant'; NotApplicable = 'n/a' }
function Get-Link($id, $base = 'skipped-rules.md') { if ($skipped.Skipped[$id] -or $skipped.Partial[$id]) { " ([details]($base#$($id.ToLowerInvariant())))" } else { '' } }

# The published 'Covered by' text: StyleBro's or the SDK's rule, or why it isn't covered. Never what StyleCop does.
function Get-Covered($row) {
    $id = $row.Rule.Id; $p = $row.Proposal
    $why = " ([why](skipped-rules.md#$($id.ToLowerInvariant())))"
    $text = if ($decisions.Mapping.ContainsKey($id)) { $decisions.Mapping[$id] }
        elseif ($p -like 'SDK:*') {
            if (-not $decisions.SdkSettings.ContainsKey($id)) { throw "decisions.psd1: $id is SDK-covered by a proposal; give it a Mapping entry" }
            'SDK: ' + $decisions.SdkSettings[$id]
        }
        elseif ($p -like 'StyleBro*') {
            $ids = @([regex]::Matches($p, '\*\*(BRO\d{4})\*\*') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
            if ($ids.Count -eq 0) { throw "decisions.psd1: $id's proposal names no **BROxxxx**; give it a Mapping entry" }
            ($ids | ForEach-Object { "**$_**" }) -join ' and '
        }
        elseif ($skipped.Skipped[$id] -is [hashtable]) {
            switch ($skipped.Skipped[$id].Status) {
                'NotInStyleCop' { 'Nothing to replace' + $why }
                'NotApplicable' { 'Not applicable' + $why }
                'Variant' { 'Variant of another rule' + $why }
                { $_ -in 'Candidate', 'SdkLater' } { 'Not covered yet' + $why }
                default { 'Not covered' + $why }
            }
        }
        elseif ($p -like 'Variant of*') { 'Variant of another rule' }
        else { 'Not covered yet' }
    [regex]::Replace($text, '\*\*(BRO\d{4})\*\*', { param($m) "[$($m.Groups[1].Value)](rules/$($m.Groups[1].Value).md)" })
}
$tested = @($check.Keys).Count

$sb = [Text.StringBuilder]::new()
function W([string]$line = '') { [void]$sb.Append($line).Append("`n") }

W '# StyleCop survey'
W
W '<!-- Generated by New-Mapping.ps1. Edit decisions.psd1, skipped.psd1 or repos.psd1, then regenerate. -->'
W
W 'Internal: the evidence behind [docs/stylecop-mapping.md](../../docs/stylecop-mapping.md) and [docs/skipped-rules.md](../../docs/skipped-rules.md), not published on the site. It compares with StyleCop.Analyzers as measured (the versions below); the published pages don''t.'
W
W 'What happens to each StyleCop.Analyzers rule when a project moves to StyleBro. Every rule gets one of three answers:'
W
W '- **SDK**: the .NET SDK already has an equivalent that `dotnet format` fixes. StyleBro''s preset turns it on.'
W '- **StyleBro**: StyleBro provides (or should provide) a rule with a safe automatic fix.'
W '- **Drop**: there is no safe automatic fix, so StyleBro deliberately doesn''t cover it.'
W '- **Not yet**: a StyleBro rule (or turning on an SDK setting) looks feasible but isn''t done.'
W
W 'Every rule StyleBro doesn''t cover is explained, with what it would take to revisit it, in [skipped-rules.md](../../docs/skipped-rules.md).'
W
W "Status: **$($decisions.Status)**. Of $($rows.Count) rules: $($tally.SDK) SDK, $($tally.StyleBro) StyleBro ($($tally.Done) done), $($tally.Drop) drop, $($tally.NotYet) not yet done, $other not applicable or variants."
W
W '## How this was measured'
W
W 'Scripts and data: this folder ([README.md](README.md)).'
W
W '- **Inventory**: every diagnostic in StyleCop.Analyzers 1.1.118, 1.2.0-beta.556 and 1.3.177-beta, read from the analyzer DLLs, including whether StyleCop itself ships a code fix (the column below is 1.3.177-beta''s). 1.2.0-beta.556 adds SA1141, SA1142, SA1316, SA1414 and some variants; 1.3.177-beta adds SA1143, SA1315, SA1317, SA1653, SA1654 and SX1412, and fixes for SA1114, SA1115, SA1117, SA1612 and SA1648.'
W "- **Teams keeping it on**: the effective setting for production code in $($repos.Count) repos that use StyleCop: $(($repos | ForEach-Object Title) -join ', '). Shown as $labels."
W "- **Diagnostics with every rule on**: each repo built with all StyleCop rules enabled as warnings (and XML docs on), counting unique diagnostics. Shown as $labels. Where a team keeps a rule on, the count is near zero; where it is off, the count shows how much code would change. The surveys ran StyleCop 1.2, so the rules 1.3 added say *not surveyed*. $(($repos | Where-Object Note | ForEach-Object Note) -join ' ')"
W "- **SDK check**: for $tested rules, a small violating example was formatted with ``dotnet format style`` and ``dotnet format whitespace`` (SDK only, not StyleCop's own fixes) using StyleCop-like SDK settings, and StyleCop was run again. ""fixed"" means the diagnostic was gone. Each rule was checked on one example, so a ""fixed"" rule can still have cases the SDK handles differently."
W
W '## Not yet done, by demand'
W
W "Rules that look feasible but aren't covered yet, ordered by how many of the $($repos.Count) teams keep them on, then by how often they fire in $($repos[-1].Label). Details and what each would take: [skipped-rules.md](../../docs/skipped-rules.md)."
W
W "| Rule | Title | Teams keeping it on ($labels) | Diagnostics ($labels) | Status |"
W '|---|---|---|---|---|'
$rows | Where-Object Proposal -like 'Not yet*' |
    Sort-Object @{ E = 'KeptCount'; Descending = $true }, @{ E = 'PrimaryCount'; Descending = $true }, @{ E = { $_.Rule.Id } } |
    ForEach-Object { W ('| [{0}](../../docs/skipped-rules.md#{1}) | {2} | {3} | {4} | {5} |' -f $_.Rule.Id, $_.Rule.Id.ToLowerInvariant(), $_.Rule.Title, (Get-Kept $_.Rule.Id), (Get-Counts $_.Rule.Id), $statusShort[$skipped.Skipped[$_.Rule.Id].Status]) }
W
foreach ($note in $decisions.Notes) { W $note; W }
W '## All rules'
W
foreach ($cat in ($inv | ForEach-Object Category | Sort-Object -Unique)) {
    W "### $($cat -replace 'Rules$', '')"
    W
    W "| Rule | Title | Default | StyleCop fix | Teams keeping it on ($labels) | Diagnostics ($labels) | SDK check | Proposal |"
    W '|---|---|---|---|---|---|---|---|'
    foreach ($row in $rows | Where-Object { $_.Rule.Category -eq $cat }) {
        $r = $row.Rule
        $id = if ($r.Id -notin $in12) { "$($r.Id) (1.3 beta)" } elseif ($r.Id -notin $in118 -and $r.Id -match '^SA\d{4}$') { "$($r.Id) (1.2 beta)" } else { $r.Id }
        W ('| {0} | {1} | {2} | {3} | {4} | {5} | {6} | {7} |' -f $id, $r.Title,
            $(if ($r.EnabledByDefault -eq 'True') { 'on' } else { 'off' }), $(if ($r.StyleCopHasFix -eq 'True') { 'yes' } else { 'no' }),
            (Get-Kept $r.Id), (Get-Counts $r.Id), (Get-Check $r.Id), ($row.Proposal + (Get-Link $r.Id '../../docs/skipped-rules.md')))
    }
    W
}
W '## How to revisit a rule'
W
W '1. Check the demand: how many surveyed teams keep the rule on, and how often it fires (the tables above).'
W '2. If the SDK might cover it, add a case to `sdk-check-cases.ps1` and run `Test-SdkCoverage.ps1`. Then check it'
W '   doesn''t change code StyleCop accepts: `Measure-MigrationDelta.ps1` on a StyleCop-clean repo (that''s how the'
W '   preset''s SA1501/SA1502 and IDE0040 settings were caught).'
W '3. For a StyleBro rule, follow the design rules in AGENTS.md (fix + Fix All, idempotent, skip unsafe cases), add a'
W '   parity set to `parity/parity.psd1` and run `Compare-WithStyleCop.ps1`.'
W '4. Move the entry out of `skipped.psd1` (into `decisions.psd1`, with a `Mapping` entry if it needs a scope note) and'
W '   regenerate.'
[IO.File]::WriteAllText([IO.Path]::GetFullPath($SurveyOut), $sb.ToString())

# docs/stylecop-mapping.md: the published ID mapping.
$sb = [Text.StringBuilder]::new()
W '# StyleCop rules and StyleBro'
W
W '<!-- Generated by scripts/stylecop-survey/New-Mapping.ps1. Edit decisions.psd1 or skipped.psd1 there, then regenerate. -->'
W
W 'Which StyleBro rule or .NET SDK rule covers each StyleCop.Analyzers rule ID. `bro migrate` uses the same mapping to'
W 'turn on the StyleBro rules that replace the StyleCop rules a project has on, and to carry its suppressions over.'
W
W '- **StyleBro** (a BRO rule): StyleBro''s rule, with a code fix that `dotnet format` applies.'
W '- **SDK**: a .NET SDK rule or setting that `dotnet format` fixes; StyleBro''s preset turns it on unless the entry says otherwise.'
W '- **Not covered**: StyleBro has no rule for it, usually because there is no safe automatic fix. Each one is explained in [skipped-rules.md](skipped-rules.md).'
W
W "Of $($rows.Count) rules: $($tally.SDK) SDK, $($tally.StyleBro) StyleBro ($($tally.Done) done), $($tally.Drop) drop, $($tally.NotYet) not yet done, $other not applicable or variants."
W
foreach ($cat in ($inv | ForEach-Object Category | Sort-Object -Unique)) {
    W "## $($cat -replace 'Rules$', '')"
    W
    W '| Rule | Title | Covered by |'
    W '|---|---|---|'
    foreach ($row in $rows | Where-Object { $_.Rule.Category -eq $cat }) {
        W ('| {0} | {1} | {2} |' -f $row.Rule.Id, $row.Rule.Title, (Get-Covered $row))
    }
    W
}
[IO.File]::WriteAllText([IO.Path]::GetFullPath($Out), $sb.ToString().TrimEnd("`n") + "`n")

# The same proposals for bro's report (embedded in the tool): why a rule isn't covered.
# CRLF on every OS (the committed file has it), so a regeneration on Linux doesn't rewrite every line.
$csv = $rows | ForEach-Object { [pscustomobject]@{ Id = $_.Rule.Id; Proposal = $_.Proposal } } | ConvertTo-Csv -NoTypeInformation -UseQuotes AsNeeded
[IO.File]::WriteAllText((Join-Path $data 'mapping.csv'), (($csv -join "`r`n") + "`r`n"))
# docs/skipped-rules.md: every rule StyleBro doesn't cover, and the covered ones with known differences.
$statusTitles = [ordered]@{
    Candidate     = 'Not yet: StyleBro candidates'
    SdkLater      = 'Not yet: the SDK can fix it, but it isn''t turned on'
    Drop          = 'Not covered: no safe automatic fix'
    NotInStyleCop = 'Nothing to replace'
    Variant       = 'Variants of other rules'
    NotApplicable = 'Not applicable'
}
$entries = foreach ($id in $skipped.Skipped.Keys) { [pscustomobject]@{ Id = $id; Rule = $invById[$id]; Entry = $skipped.Skipped[$id] } }
$unknown = @($entries | Where-Object { -not $_.Rule })
if ($unknown) { throw "skipped.psd1 has ids that aren't StyleCop rules: $($unknown.Id -join ', ')" }

$sb = [Text.StringBuilder]::new()
W '# StyleCop rules StyleBro doesn''t cover'
W
W '<!-- Generated by scripts/stylecop-survey/New-Mapping.ps1. Edit skipped.psd1 there, then regenerate. -->'
W
W 'The StyleCop.Analyzers rule IDs StyleBro has no rule for, why, and what would make StyleBro revisit them. Rules'
W 'StyleBro covers only in part are at the end. Every ID and what covers it: [stylecop-mapping.md](stylecop-mapping.md).'
W
W "## Summary"
W
$parts = foreach ($s in $statusTitles.Keys) { "$(@($entries | Where-Object { $_.Entry.Status -eq $s }).Count) $($statusShort[$s])" }
W "$($entries.Count) rules: $($parts -join ', ')."
W
W '| Rule | Title | Status |'
W '|---|---|---|'
foreach ($s in $statusTitles.Keys) {
    foreach ($e in $entries | Where-Object { $_.Entry.Status -eq $s } | Sort-Object Id) {
        W ('| [{0}](#{1}) | {2} | {3} |' -f $e.Id, $e.Id.ToLowerInvariant(), $e.Rule.Title, $statusShort[$s])
    }
}
W
foreach ($s in $statusTitles.Keys) {
    $group = @($entries | Where-Object { $_.Entry.Status -eq $s } | Sort-Object Id)
    if ($group.Count -eq 0) { continue }
    W "## $($statusTitles[$s])"
    W
    foreach ($e in $group) {
        W "<a id=""$($e.Id.ToLowerInvariant())""></a>"
        W
        W "### $($e.Id): $($e.Rule.Title)"
        W
        W "**Why:** $($e.Entry.Why)"
        W
        W "**To revisit:** $($e.Entry.Revisit)"
        W
    }
}
W '## Partly covered'
W
foreach ($id in $skipped.Partial.Keys | Sort-Object) {
    $p = $skipped.Partial[$id]
    W "<a id=""$($id.ToLowerInvariant())""></a>"
    W
    W "### $($id): $($invById[$id].Title)"
    W
    $covered = Get-Covered ($rows | Where-Object { $_.Rule.Id -eq $id })
    $covered = $covered -replace ' \(\[(why|details)\]\(skipped-rules\.md#[^)]*\)\)', ''
    W "**Covered by:** $covered"
    W
    W "**Scope:** $($p.Why)"
    W
    W "**To revisit:** $($p.Revisit)"
    W
}
[IO.File]::WriteAllText([IO.Path]::GetFullPath($SkippedOut), $sb.ToString().TrimEnd("`n") + "`n")

"$([IO.Path]::GetFullPath($Out)): $($rows.Count) rules. SDK $($tally.SDK), StyleBro $($tally.StyleBro), drop $($tally.Drop), not yet $($tally.NotYet), untested $($tally.Untested), other $other"
"$([IO.Path]::GetFullPath($SkippedOut)): $($entries.Count) skipped, $($skipped.Partial.Count) partly covered"
"$([IO.Path]::GetFullPath($SurveyOut)): the full survey"
