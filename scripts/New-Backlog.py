"""Writes docs/backlog.md: every rule StyleBro has (from docs/rules/README.md and the analyzer release files) and the planned and
"maybe" ones listed below. Edit PLANNED and the "Maybe" tables here, then run from the repository root:
python scripts/New-Backlog.py"""
import re

readme = open('docs/rules/README.md', encoding='utf-8').read()
rows = re.findall(r'^\| \[(BRO\d{4})\]\(BRO\d{4}\.md\) \| (.*?) \| (.*?) \|', readme, re.M)
unshipped = set(re.findall(r'^(BRO\d{4})\s*\|', open('src/StyleBro.Analyzers/AnalyzerReleases.Unshipped.md').read(), re.M))

PLANNED = [
]

# Work beyond single rules (decided 2026-10-03, see decisions.md "After StyleCop parity"), in this order.
WORK = [
    ('Hardening', 'More real-world repos in the PR pipeline', 'Jellyfin (an application using StyleCop), FluentValidation and CsvHelper (seven target frameworks) added; they found a BRO1302 rename that broke CsvHelper at run time and a BRO1001 blank line that needed a second run.', 'Done (2026-10-03)'),
    ('Hardening', 'IDE0055 in multi-targeted repos', "`dotnet format` crashes in Roslyn's linked-file merge there. `stylebro-migrate format` runs it once per target framework (projects loaded for one framework: nothing to merge); `init` writes IDE0055 at warning everywhere. See decisions.md.", 'Done (2026-10-03)'),
    ('Parity', 'SA1316: tuple element names in PascalCase', 'BRO1311: renamed with every use, literal and override solution-wide (Roslyn\'s Renamer crashes on tuple elements); `stylebro_tuple_element_name_casing`.', 'Done (2026-10-03)'),
    ('Parity', 'SA1108: no comments inside block statements', 'BRO1132: the comment moves into the block, right after `{`; multi-line headers skipped (decisions.md, proposals/sa1108.md).', 'Done (2026-10-04)'),
    ('Parity', 'BRO1309: namespace names', 'BRO1312, opt-in (off by default, on through `stylebro-migrate` with SA1300); the root namespace is left out. See docs/proposals/namespace-names.md.', 'Done (2026-10-04)'),
    ('Adoption', 'Getting started', 'docs/getting-started.md: from install to the first `dotnet format` run, for new projects and for StyleCop users; multi-targeted repos and baselines.', 'Done (2026-10-03)'),
    ('Adoption', 'IDE experience', "Checked by the owner in Visual Studio 2022 (17.13) with the package from a local feed and `init --write`: the build reports the same 8 rules as the command line (not the preset-off BRO1112), the light bulb renames (BRO1303) with a preview, Fix All in the solution sorts (BRO1001), and squiggles appear while typing (BRO1002). Rider not checked (not installed).", 'Done (2026-10-05)'),
    ('Adoption', 'Migration sample', 'samples/StyleCopMigration: a StyleCop-clean project with changed defaults, migrated step by step with real output; `dotnet format` changes no code afterwards.', 'Done (2026-10-03)'),
    ('Adoption', '`init --modernize`', 'Opt-in block of the SDK\'s modernization rules in three tiers ([modernizing.md](modernizing.md)): older C# everywhere; newer C# at warning unless a multi-targeted project leaves LangVersion unset; newer APIs only without multi-targeted projects (else suggestions with a note). See decisions.md.', 'Done (2026-10-04)'),
    ('Adoption', 'Multi-target guard for modernization rules', 'MultiTargetSuppressor: the package passes the project\'s target frameworks (StyleBroTargetFrameworks, comma-separated) and the suppressor hides a tier C rule where a framework lacks its API, so init --modernize writes tier C at warning everywhere. See modernizing.md.', 'Done (2026-10-05)'),
    ('Beyond StyleCop', 'Survey other analyzers', "Fixable style rules in Roslynator, Meziantou, Sonar and ErrorProne.NET that the SDK doesn't cover, ranked in [beyond-stylecop.md](beyond-stylecop.md); the top five are under Maybe.", 'Done (2026-10-03)'),
    ('Beyond StyleCop', "Survey StyleCop's issue tracker", 'Proposals, requested fixes and options, and known bugs checked against StyleBro, in [beyond-stylecop.md](beyond-stylecop.md#from-stylecops-issue-tracker); the top five are under Maybe, possible bugs listed there.', 'Done (2026-10-04)'),
    ('Beyond StyleCop', 'Operator, `=>` and `=` placement when wrapping (survey #1, #2)', 'BRO1520 (operators, follows `dotnet_style_operator_placement_when_wrapping`), BRO1521 (`=>`), BRO1522 (`=`); defaults from a survey of the reference repositories, see decisions.md. Off after `stylebro-migrate`.', 'Done (2026-10-04)'),
    ('Beyond StyleCop', 'Comments in declaration headers (StyleCop issue #605)', 'BRO1134: BRO1132\'s move for types, namespaces, members, accessors and local functions; multi-line headers skipped. Off after `stylebro-migrate`.', 'Done (2026-10-04)'),
    ('Beyond StyleCop', 'Call chain layout (survey #4, Roslynator RCS0054)', 'BRO1523: in a split call chain every call after the first line starts its own line; the chain keeps its own indentation. Defaults from a survey of the reference repositories, see decisions.md. Off after `stylebro-migrate`.', 'Done (2026-10-04)'),
    ('Beyond StyleCop', 'Upper-case literal suffixes (StyleCop issue #1563, Sonar S818)', 'BRO1135: integer suffixes (`1l` -> `1L`, `1ul` -> `1UL`); real suffixes (`f`, `d`, `m`) left alone, lower case is the norm there. Off after `stylebro-migrate`.', 'Done (2026-10-04)'),
    ('Beyond StyleCop', 'No blank line after attributes (StyleCop\'s proposed SA1521, issue #738)', 'BRO1525: also between stacked attribute lists; comments and directives in the gap skipped. Off after `stylebro-migrate`.', 'Done (2026-10-04)'),
    ('Beyond StyleCop', 'Null check style (survey #3)', 'BRO1133: `x is null` (default, decided by the owner) or `x == null` (`stylebro_null_check_style`), see decisions.md. Off after `stylebro-migrate`.', 'Done (2026-10-04)'),
    ('Beyond StyleCop', 'Split conditional expressions (StyleCop issue #651)', 'BRO1524: the condition, `? a` and `: b` each start their own line once the expression is split; `?`/`:` side from BRO1520\'s setting; chains skipped. Off after `stylebro-migrate`.', 'Done (2026-10-04)'),
    ('Beyond StyleCop', '`<summary>` on one line or on three (survey #5)', 'BRO1616: tags on lines of their own (default, from a survey of the reference repositories: 14,329 to 148) or `single_line_when_fits` (`stylebro_summary_layout`, `max_line_length`); summary only. Off after `stylebro-migrate`.', 'Done (2026-10-04)'),
    ('Beyond StyleCop', 'Naming: camelCase constants, base parameter names, `Async` suffix (StyleCop #2641/#3793, #1949; Roslynator RCS1046)', 'BRO1306 follows a `dotnet_naming_rule` that asks for camelCase private constants or static readonly fields; BRO1313 (parameters named like the base member) and BRO1314 (`Async` on methods returning Task/ValueTask/IAsyncEnumerable), both off by default. See decisions.md.', 'Done (2026-10-05)'),
    ('Beyond StyleCop', 'Braceless `if (x) return;` (StyleCop issue #2252)', '`stylebro_allow_single_line_jump_statements` for BRO1514, off by default: a jump statement on its `if` line needs no braces (decisions.md).', 'Done (2026-10-04)'),
    ('Beyond StyleCop', 'Switch section blank lines, no `else` after a jump, overloads together (survey #6, #16, #17)', 'BRO1526 (`stylebro_blank_line_between_switch_sections`, default include; after a multi-line block BRO1519 decides), BRO1143 (off by default; `else if` chains skipped), BRO1001 option `stylebro_keep_overloads_together` (default false). See decisions.md. BRO1526 off after `stylebro-migrate`.', 'Done (2026-10-05)'),
    ('Beyond StyleCop', 'Documentation batch A (StyleCop issues #758, #3546/#1490; Meziantou MA0154; survey #15)', 'BRO1617 cref type arguments in braces (`List{T}`), BRO1618 `<see langword="null"/>` for a keyword alone in `<c>`, BRO1619 top-level doc elements in a fixed order (stable, so BRO1611 keeps `<param>` order); `stylebro_comment_blank_line_exempt_prefixes` for BRO1504. On in the preset, off after `stylebro-migrate` (decisions.md).', 'Done (2026-10-05)'),
    ('Performance', 'Cheap checks first, one walk per tree', 'DocumentationAnalyzer, CommentTextAnalyzer, NullCheckAnalyzer, WrappingPlacementAnalyzer, CamelCaseNamingAnalyzer, BaseCallsAnalyzer, CallChainAnalyzer, EmbeddedCommentAnalyzer: same diagnostics, all 55 analyzers ~830 -> ~700 ms on Newtonsoft.Json (scripts/benchmark/README.md).', 'Done (2026-10-04)'),
    ('Performance', 'CI regression check', '`scripts/benchmark` compare mode: the PR build and main\'s alternated in one process on Newtonsoft.Json, freshly parsed sources per run; fails when an analyzer or the total is clearly slower. A required check since 2026-10-05.', 'Done (2026-10-05)'),
    ('Performance', 'FieldNamingAnalyzer', 'Profiled with dotnet-trace: its per-type scan built every documentation comment\'s XML to find strings in directives; now opens only directives (~25-30 % faster, same diagnostics).', 'Done (2026-10-04)'),
    ('Performance', 'One token pass for the layout analyzers', 'Done as a shared per-tree cache (`TreeWalk`) instead of a merged analyzer: ~16 analyzers read one cached walk; 595 -> 386 ms on Newtonsoft.Json, same diagnostics.', 'Done (2026-10-05)'),
    ('Superseding StyleCop', "Parity with StyleCop's `master`", "The unreleased commits since 1.2.0-beta.556 that change a replaced rule: BRO1104 `new nint()`, BRO1604/BRO1605 `init`, BRO1611 primary constructors, BRO1401 property patterns, BRO1505 single-line properties and BRO1601 explicit implementations follow `master`, BRO1606/BRO1607 report blank summaries; BRO1606 `<para>` and BRO1311 keep their behavior (decisions.md). `Compare-WithStyleCop.ps1 -StyleCopFeed` runs the parity sets against a local `master` build.", 'Done (2026-10-04)'),
    ('Superseding StyleCop', "StyleCop's open bugs", "54 reports checked against StyleBro: 34 not shared, 12 by design or not applicable, 8 shared (7 fixed, 1 kept like StyleCop by decision; decisions.md). Listed in differences-from-stylecop.md.", 'Done (2026-10-04)'),
    ('Superseding StyleCop', 'The remaining tracker candidates', 'beyond-stylecop.md #6-#10 and #12: BRO1617, BRO1504\'s exempt prefixes, BRO1136, BRO1137, BRO1306\'s naming-rule option, BRO1313; #11 as the narrow BRO1409 (off by default).', 'Done (2026-10-05)'),
    ('Superseding StyleCop', 'The remaining other-analyzer candidates', 'beyond-stylecop.md survey #6-#10 and #12-#18: BRO1526, BRO1142, BRO1138, BRO1618, BRO1141, BRO1139, BRO1140, BRO1408, BRO1619, BRO1143, BRO1001\'s overloads option, BRO1314.', 'Done (2026-10-05)'),
    ('Beyond StyleCop', 'Readability batch B (StyleCop issues #762, #760; survey #7, #8, #10, #12, #13, #14)', 'BRO1136 `x => x`, BRO1137 redundant `return;`/`yield break;`, BRO1138 unneeded `$`/`@`/raw string, BRO1139 `else if`, BRO1140 empty record body, BRO1141 object creation parentheses (`stylebro_object_creation_parentheses`, default omit), BRO1142 one local per declaration, BRO1408 redundant base type. On in the preset, off after `stylebro-migrate`.', 'Done (2026-10-05)'),
    ('Beyond StyleCop', 'Public methods of internal types (StyleCop issue #2981)', 'BRO1409, off by default: `public` -> `internal` on ordinary methods of internal types only; members reflection, serializers, frameworks or the compiler look for are left out, names in strings or `nameof` not fixed. See decisions.md. Off after `stylebro-migrate`.', 'Done (2026-10-05)'),
    ('Beyond StyleCop', 'Redundant `!` and `HasValue` (newer-rules survey #5, #6; Sonar S8969, Meziantou MA0171)', "BRO1147 (a null-forgiving `!` on a value already not null; skips multi-targeted projects, type arguments, `null!`) and BRO1148 (`x.HasValue` -> `x is not null`, in BRO1133's form), both off by default (decisions.md). The SDK's IDE0370 works too once its severity is set (rule page).", 'Done (2026-10-05)'),
    ('Beyond StyleCop', 'C# 14: keywords and extension blocks (survey 2026-10-05)', 'BRO1144 `@field`/`@extension`/`@partial` where C# 14 reads the name as a keyword (Sonar S8367/S8368/S8380, no fix elsewhere); BRO1001 sorts extension blocks right before methods and sorts their members; BRO1509 expands a one-line extension block. On in the preset, BRO1144 off after `stylebro-migrate`. See decisions.md.', 'Done (2026-10-05)'),
    ('Beyond StyleCop', 'Declaration forms (Roslynator RCS0042, Meziantou MA0206, MA0174)', 'BRO1527 auto-accessors on one line (blank lines BRO1505/BRO1519 want added in the same fix), BRO1146 `record` instead of `record class` (on in the preset, off after `stylebro-migrate`); BRO1145 `class Marker;` for empty bodies (C# 12; off by default, skipped in multi-targeted projects unless every framework gets C# 12). See decisions.md.', 'Done (2026-10-05)'),
    ('Adoption', 'First-run trial (2026-10-06)', 'GuardClauses, Dapper, Nerdbank.Streams, CleanArchitecture installed like a new user: fixed BRO1601/BRO1602 looping on `#if` attributes, BRO1601 only with doc generation, BRO1603 period placement, BRO1604/BRO1606 wording, BRO1134 trailing comments, migrate (StyleCop casing pinned, submodules skipped, IDE0073 only with SA1636, merged format reports), getting-started rewritten.', 'Done (2026-10-06)'),
    ('Performance', 'Analyzer speed, round 3', 'Shared trivia array, one walk for tokens and nodes, cheaper checks first: all analyzers about a third faster on Newtonsoft.Json (#63).', 'Done (2026-10-07)'),
    ('Configuration', 'Options where preferences differ (decisions.md 2026-10-07)', 'Eleven .editorconfig options from a survey of the 12 reference repos: `default` literal (BRO1104), `""` (BRO1106), `l`-only suffixes (BRO1135), omit trailing commas (BRO1401), empty `{ }` on one line (BRO1508/BRO1509), `)` on its own line (BRO1110), first item on the `(` line (BRO1107), adjacent single-line members (BRO1505), `<inheritdoc />` (BRO1601), `: base` / `where` on the declaration line (BRO1105/BRO1111).', 'Done (2026-10-08)'),
    ('Hardening', 'Four fix-order bugs', 'BRO1001 on one-line types and blank lines in other slots, BRO1114 on documented fields, BRO1116 vs BRO1108 (#69).', 'Done (2026-10-08)'),
    ('Hardening', 'BRO1112 sort path vs BRO1505', "The regions fix sorts members itself (`GetSortChangesAsync`) but doesn't add BRO1505's missing blank lines first, as BRO1001's fix now does: likely order-dependent when BRO1112, BRO1505 and BRO1001 all fire. Reproduce with a FixOrderTests case first.", 'Idea'),
    ('Hardening', 'BRO1105/BRO1111 same_line at the line limit', 'Another fix in the same run can change the joined line\'s length right at `max_line_length`, so `dotnet format` may need a second run (documented). Could measure the line as the other fixes will leave it.', 'Idea'),
    ('Hardening', 'BRO1110 own_line indentation', 'In own_line mode a `)` already on its own line isn\'t checked for indentation.', 'Idea'),
    ('CI', 'Split the mutation job', 'Was 70-106 minutes for ~475 entries in one job. Now 4 parallel shards (index modulo 4) plus a summary job named `mutation` (the required check) that fails when any shard fails or is cancelled (#71).', 'Done (2026-10-09)'),
    ('Adoption', 'Second first-run trial with the published package', 'Install 0.2.0-alpha.1 from nuget.org into 3-4 repos nobody has tried, exactly as a new user would; the first trial (local packages) found a loop and 213 build errors.', 'Idea'),
    ('Adoption', 'Readable release notes', "GitHub's generated notes list every PR title; a short summary per release (what's new, how to upgrade, known limits) for users.", 'Idea'),
    ('Adoption', 'Rider check', 'The IDE check was done in Visual Studio 2022 only.', 'Idea'),
    ('Upstream', 'Report the SDK fixer bugs to dotnet/roslyn', 'Minimal repros for IDE0011 (crash), IDE0040/IDE0047 (conflict markers), IDE0055 (crash) and IDE0048 (broken edit) in multi-targeted projects under `dotnet format`; the owner posts them.', 'Idea'),
    ('Parity', 'SA1119_p and SX1309S', 'The two StyleCop variants StyleBro doesn\'t cover: unnecessary parentheses in patterns (`x is (> 0)`, next to BRO1405) and the `_` prefix for static fields (a BRO1303 style). See skipped-rules.md.', 'Idea'),
    ('Beyond StyleCop', 'Unnecessary parentheses in patterns', 'BRO1410: `x is (> 0)`, `o is (string s)`, a pair around a pair; leaves the ones BRO1407 adds to `and`/`or` patterns. BRO1405_p/BRO1410_p fade the parentheses in an IDE like StyleCop\'s SA1119_p. Off after `stylebro-migrate`. See decisions.md.', 'Done (2026-10-09)'),
    ('Performance', 'FieldNamingAnalyzer variance', 'Swings 35-90 ms between runs in both builds (it often runs first on a file and pays for the shared walk); find a stable measure or split its cost.', 'Idea'),
]

# Candidates from StyleCop's issue tracker and other analyzers (docs/beyond-stylecop.md); planned as a group (WORK).
MAYBE = [
]

blocks = [
    ('10xx', 'Ordering, spacing and comments'),
    ('11xx', 'Readability'),
    ('13xx', 'Naming'),
    ('14xx', 'Maintainability'),
    ('15xx', 'Layout'),
    ('16xx', 'Documentation'),
]

done = len(rows)
out = []
out.append('# Backlog\n')
out.append('<!-- Generated by scripts/New-Backlog.py. Edit the planned and maybe lists there, then run it. -->\n')
out.append('Every style rule StyleBro has or aims to add, with its status, and what\'s left open in existing rules. Update it')
out.append('when work starts or lands; a finished rule also moves from `skipped.psd1` to `decisions.psd1` (see')
out.append('`docs/stylecop-mapping.md`).\n')
out.append('Status: **Released** (on nuget.org), **Done** (on `main`, in the next release), **Planned** (decided, not started), **Idea** (work item to pick up later, not decided),')
out.append('**Maybe** (candidates without an id yet, from the StyleCop tracker and other analyzers; planned as a group under Work, see [beyond-stylecop.md](beyond-stylecop.md)).\n')
out.append('## Summary\n')
out.append('| | Rules |')
out.append('|---|---|')
out.append(f'| Released | {done - len(unshipped)} |')
out.append(f'| Done, not released yet | {len(unshipped)} |')
out.append(f'| Planned | {len(PLANNED)} rules, {len(WORK)} work items |')
out.append(f'| Maybe | {len(MAYBE)} candidates from the StyleCop tracker and other analyzers |')
out.append('')
mapping = open('docs/stylecop-mapping.md', encoding='utf-8').read()
m = re.search(r'Of (\d+) rules: (\d+) SDK, (\d+) StyleBro .*?, (\d+) drop', mapping)
total, sdk, bro, drop = m.groups()
out.append(f'StyleCop coverage ({total} diagnostics in StyleCop 1.2): {bro} by StyleBro, {sdk} by the .NET SDK, {drop} dropped by design (they')
out.append('can\'t be fixed without inventing text or moving code between files), the rest variants or not applicable. Details in')
out.append('[stylecop-mapping.md](stylecop-mapping.md).\n')

out.append('## Planned\n')
if PLANNED:
    out.append('| ID | Rule | StyleCop | Replaces SDK |')
    out.append('|----|------|----------|--------------|')
    for id, title, sa, ide in PLANNED:
        out.append(f'| {id} | {title} | {sa} | {ide} |')
    out.append('')
else:
    out.append('No new rules planned. The last batch, fix-safe replacements for the SDK rules whose fixes crash `dotnet format`')
    out.append('or write merge conflict markers in multi-targeted projects (decided 2026-10-03, see [decisions.md](decisions.md)),')
    out.append('is done: BRO1514-BRO1516 (IDE0011), BRO1404/BRO1007 (IDE0040), BRO1405-BRO1407 (IDE0047/IDE0048) and')
    out.append('BRO1517-BRO1519 (IDE2000/IDE2002/IDE2003). `init` and `stylebro-migrate` turn those SDK rules off.\n')

out.append('### Work beyond single rules\n')
out.append('StyleCop parity is done apart from the parity items below. Decided 2026-10-03 ([decisions.md](decisions.md)), in this order:\n')
out.append('| Area | Item | Notes | Status |')
out.append('|------|------|-------|--------|')
for area, item, notes, status in WORK:
    out.append(f'| {area} | {item} | {notes} | {status} |')
out.append('')

out.append('### Read the SDK\'s own settings\n')
out.append('Decided 2026-10-03 ([decisions.md](decisions.md)): where the SDK has an `.editorconfig` key for the same choice, StyleBro')
out.append('follows it. Precedence: a `stylebro_*` key, then the SDK key, then StyleCop\'s behavior (today\'s default, unchanged')
out.append('when neither is set). `stylebro-migrate` keeps writing what matches the StyleCop setup.\n')
out.append('| Rules | SDK key | What changes | Status |')
out.append('|-------|---------|--------------|--------|')
for rules, key, change, status in [
    ('BRO1303 (private fields)', '`dotnet_naming_rule.*` / `dotnet_naming_style.*`', 'A naming rule for private fields (required prefix `_`, camel case) picks `camelCase` or `_camelCase` when `stylebro_private_field_naming` isn\'t set (the preset no longer sets it); a style the rule can\'t produce leaves the default. BRO1307 leaves a one-letter prefix a naming rule asks for (`s_`).', 'Done (2026-10-03)'),
    ('BRO1131', '`dotnet_style_qualification_for_method/_property/_field/_event`', '`false` drops `base.` (`Reset()`) instead of writing `this.Reset()`, where the unqualified name binds the same.', 'Done (2026-10-03)'),
    ('BRO1514-BRO1516', '`csharp_prefer_braces`', '`when_multiline`: only multi-line child statements (as IDE0011 counts them) and inconsistent chains; `false`: none.', 'Done (2026-10-03)'),
    ('BRO1404', '`dotnet_style_require_accessibility_modifiers`', '`never`/`omit_if_default`: not reported (removing modifiers is not what these rules do); `always`: interface members too (C# 8+).', 'Done (2026-10-03)'),
    ('BRO1406/BRO1407', '`dotnet_style_parentheses_in_arithmetic_binary_operators` / `_in_other_binary_operators`', '`never_if_unnecessary` turns the rule off (no parentheses added). Removing stays SA1119 (BRO1405): following the SDK\'s broader removal would bring back the `a ?? (b ?? c)` difference in migrated StyleCop repos. `always_for_clarity` keeps StyleCop\'s operator families.', 'Done (2026-10-03)'),
    ('All text fixes', '`end_of_line`', 'Not needed: a full `dotnet format` run normalizes line endings to `end_of_line` in its whitespace pass before the analyzer fixes run, and those copy the file\'s ending.', 'Dropped'),
]:
    out.append(f'| {rules} | {key} | {change} | {status} |')
out.append('')
out.append('IDE0055 (the SDK\'s whole formatter) isn\'t replaced: in multi-targeted repositories `stylebro-migrate format` runs')
out.append('`dotnet format` once per target framework, which avoids the crash. Also to do: report the SDK fixer bugs upstream')
out.append('(dotnet/roslyn).\n')

out.append('## Maybe\n')
out.append('The next rules from other analyzers, from the survey in [beyond-stylecop.md](beyond-stylecop.md) (more there):\n')
out.append('| Source | Rule | Notes |')
out.append('|--------|------|-------|')
for source, rule, notes in MAYBE:
    out.append(f'| {source} | {rule} | {notes} |')
out.append('')
out.append('The other dropped StyleCop rules (missing documentation, one type per file, ...) are in [skipped-rules.md](skipped-rules.md).\n')

out.append('## All rules\n')
for block, name in blocks:
    prefix = 'BRO' + block[:2]
    out.append(f'### BRO{block}: {name}\n')
    out.append('| ID | Rule | StyleCop | Status |')
    out.append('|----|------|----------|--------|')
    entries = [(id, title, sa, 'Done' if id in unshipped else 'Released') for id, title, sa in rows if id.startswith(prefix)]
    entries += [(id, title, sa, 'Planned') for id, title, sa, _ in PLANNED if id.startswith(prefix)]
    for id, title, sa, status in sorted(entries):
        link = f'[{id}](rules/{id}.md)' if status != 'Planned' else id
        out.append(f'| {link} | {title} | {sa} | {status} |')
    out.append('')

open('docs/backlog.md', 'w', encoding='utf-8', newline='').write('\n'.join(out).rstrip('\n') + '\n')
print(done, len(unshipped))
