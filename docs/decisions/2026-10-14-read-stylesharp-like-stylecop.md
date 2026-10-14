# Read StyleSharp like StyleCop (2026-10-14)

## Question

Trial 21's Splat uses StyleSharp.Analyzers, a StyleCop successor with SST ids (SST1309 = error: private fields are
`_camelCase`) and `stylesharp.*` settings, next to Sonar and a leftover `stylecop.json`. `stylebro-migrate` didn't know
StyleSharp: with no StyleCop.Analyzers reference it read the leftover `stylecop.json` with StyleCop's defaults (because
Sonar was there), wrote BRO1303 `camelCase` and `this.` qualification, and `format` renamed fields in 72 files: 973 build
errors. How should the migration treat a StyleSharp setup?

## Choices

1. **Read it like StyleCop** (owner's pick): a StyleSharp.Analyzers reference counts as StyleCop running; each SST
   rule's severity counts as the StyleCop rule it stands for (a mapping generated from StyleSharp's DLL, exceptions by
   hand: SST1309 is StyleCop's SX1309, SST1117 is SA1101 or SX1101 by `stylesharp.instance_member_qualification`, ...),
   its `stylesharp.*` keys as `stylecop.json`'s settings, SST suppressions carried over, the package swap reported.
2. Treat it like any other analyzer: init's convention detection decides, StyleSharp's rules are only reported.
3. Leave it out: tell the user to migrate by hand.

## Decision

**Choice 1** (owner: "Read it like StyleCop"). SST rules StyleCop doesn't have are listed in the report as rules that go
with StyleSharp. With both packages referenced, a StyleCop rule is on when either enforces it. Also decided with it: a
leftover `stylecop.json` (or SA severities) with neither package referenced no longer makes the migration apply StyleCop's
defaults when a Sonar setup is there (the exception in
[2026-10-13](2026-10-13-stylecop-settings-without-the-stylecop-package-are-leftovers.md) is dropped): the migration
points to `init`, which follows the Sonar setup and the code's conventions.
