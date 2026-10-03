# Backlog

Style rules StyleBro aims to add, and what's left open in rules that exist. Update the status when work starts or
lands; when a rule is done, it also moves from `skipped.psd1` to `decisions.psd1` (see `docs/stylecop-mapping.md`).

Status: **Planned** (decided, not started), **In progress**, **Done** (date), **Maybe** (worth doing if someone asks;
the reason it isn't planned is in [skipped-rules.md](skipped-rules.md)).

Coverage today: 95 StyleCop rules by StyleBro, 53 by the SDK, 44 dropped by design (StyleCop 1.2, 197 diagnostics).

## 1. Fix-safe replacements for SDK rules (multi-targeted projects)

Decided 2026-10-03 ([decisions.md](decisions.md)): the SDK's fixers for these crash or write conflict markers in
multi-targeted projects, so StyleBro ships its own. Once a rule is done, `init` and `stylebro-migrate` turn it on and
the SDK rule off. IDs are reserved, not final until the rule ships.

| Planned ID | StyleCop | What | Replaces SDK | Status |
|------------|----------|------|--------------|--------|
| BRO1514 | SA1503 | Braces must not be omitted | IDE0011 | Done (2026-10-03) |
| BRO1515 | SA1519 | Braces must not be omitted from a multi-line child statement | IDE0011 | Done (2026-10-03) |
| BRO1516 | SA1520 | Use braces consistently in an if/else chain | IDE0011 | Done (2026-10-03) |
| BRO1404 | SA1400 | Access modifier must be declared | IDE0040 | Planned |
| BRO1007 | SA1205 | Partial elements must declare an access modifier | IDE0040 | Planned |
| BRO1405 | SA1119 | Statement must not use unnecessary parentheses | IDE0047 | Planned |
| BRO1406 | SA1407 | Arithmetic expressions must declare precedence | IDE0048 | Planned |
| BRO1407 | SA1408 | Conditional expressions must declare precedence | IDE0048 | Planned |
| BRO1517 | SA1507 | Code must not contain multiple blank lines in a row | IDE2000 | Planned |
| BRO1518 | SA1508 | Closing braces must not be preceded by a blank line | IDE2002 | Planned |
| BRO1519 | SA1513 | Closing brace must be followed by a blank line | IDE2003 | Planned |

Not planned: IDE0055 (the SDK's whole formatter; `dotnet format`'s whitespace pass still does that job safely).
Also to do: report the SDK fixer bugs upstream (dotnet/roslyn).

## 2. StyleCop rules not covered yet

| StyleCop | What | Status | Notes |
|----------|------|--------|-------|
| SA1316 | Tuple element names use the configured casing | Maybe | StyleCop's fix renames only the declaration and breaks every use; a safe rename must change deconstructions, `t.name` and inferred names together. 0 findings in the surveyed repos. |
| SA1305 | No Hungarian notation in field names | Maybe | Possible with the BRO13xx renamer and an explicit prefix list (`stylebro_hungarian_prefixes`). StyleCop has no fix. |
| SA1108 | No comment between a statement and its block | Maybe | A fix could move the comment above the statement; probe what teams expect first. StyleCop has no fix. |

The other dropped rules (missing documentation, one type per file, ...) can't be fixed by `dotnet format` without
inventing text or moving code between files; reasons in [skipped-rules.md](skipped-rules.md).

## 3. Gaps in existing rules

| Rule | Gap | Status |
|------|-----|--------|
| BRO1303/BRO1306 (SA1306, SA1309) | Protected fields aren't renamed (`_x`, casing) | Maybe |
| BRO1309 (SA1300) | Namespace names aren't checked | Maybe |
| BRO1001 (SA1201) | Types aren't ordered within a namespace | Maybe |
| BRO1001 | Types with `#region`/`#if` between members are skipped (a region-heavy codebase needs a second run after BRO1112) | Maybe |
| BRO1514-BRO1516 | stylecop.json `allowConsecutiveUsings: false` isn't configurable (StyleBro always allows `using (a) using (b) { }`) | Maybe |

## Done recently

| ID | StyleCop | Done |
|----|----------|------|
| BRO1514-BRO1516 | SA1503, SA1519, SA1520 | 2026-10-03 |
| BRO1006 | SA1006 | 2026-10-03 |
| BRO1131 | SA1100 | 2026-10-03 |
| BRO1127-BRO1130 | SA1102-SA1105 | 2026-10-03 |
