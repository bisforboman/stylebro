# StyleBro vs StyleCop.Analyzers

For a team deciding whether to switch. Measured numbers only; each one says where it comes from. As of
**2026-11-07**: StyleBro's `main` against **StyleCop.Analyzers 1.3.177-beta** (2026-11-06), the newest release.
StyleCop is maintained again: since 2026-10-12 it has shipped eleven 1.3 betas (1.3.1-beta to 1.3.177-beta), with
new rules, new code fixes and many bug fixes. Everything below was measured against 1.3.177-beta; where 1.2.0-beta.556
(the release before, still the one most repositories use) differs, it says so.

## Summary

| | StyleCop.Analyzers 1.3.177-beta | StyleBro (`main`, 2026-11-07) |
|---|---|---|
| Fixes under `dotnet format` | 162 of 203 diagnostics have a fix; some have no Fix All, throw, or need several runs ([below](#what-it-is)) | Every rule has a fix with Fix All; one run converges |
| StyleCop's 203 diagnostics | All | 109 by StyleBro rules, 44 by the .NET SDK's rules, 34 dropped (no safe automatic fix), 10 StyleCop never reports, 4 not yet (new in 1.3), 2 not applicable ([Coverage](#coverage)) |
| Rules beyond StyleCop | None | 33 (most from Roslynator, Meziantou, Sonar or StyleCop's own tracker); 3 more now have a 1.3 counterpart |
| Analyzer time, Newtonsoft.Json (242 files) | 1,057-1,077 ms (188 analyzers) | 347-356 ms (76 analyzers); 3.0x faster in each of 3 runs ([Speed](#speed)) |
| StyleCop bugs on the replaced rules (54 issues, read 2026-10-04) | Re-run on 1.3.177-beta: 24 fixed in 1.3, 3 changed, 13 still there, 14 not reproducible on 1.2.0-beta.556 either | 34 not shared, 8 shared (7 fixed, 1 kept on purpose), 12 not applicable or the same by design (2026-10-04) |
| Moving a StyleCop-clean repo | (the starting point) | Files changed after `bro migrate --write` + `dotnet format`: Polly 0, OpenTelemetry 1 (an SDK trimming fix, not StyleBro) ([Migration](#migration)) |
| Baseline (fail only on new violations) | No | Yes (`bro baseline`) |
| Multi-targeted projects | Not measured | Fix All merges each target framework's copy itself (no conflict markers in Newtonsoft.Json, 8 frameworks); `bro format` works around the SDK's IDE0055 crash |
| Release status | 1.3.177-beta (a beta; last stable 1.1.118); 1.2.0-beta.556 before it | 0.8.0-alpha.1 on nuget.org (129 rules), a prerelease |

## What it is

StyleBro is a set of Roslyn analyzers whose every rule has a code fix that `dotnet format` applies, with Fix All, so
`dotnet format` fixes everything StyleBro reports. Fixes are deterministic and idempotent: after one run, a second run
changes nothing (the CI check for this repository and the [real-world check](#correctness) both test that). Where a fix
could be unsafe (it would change behavior, lose a comment, or depend on `#if` code), StyleBro skips the case instead of
reporting something it can't fix.

Formatting the SDK already handles (spacing, indentation, using order, modifier order, ...) isn't duplicated: StyleBro's
preset sets the SDK's options and `bro init` turns the SDK rules on.

StyleCop's own fixes, run under `dotnet format` (`dotnet format analyzers --diagnostics <id>`, up to three runs, LF
files) on one small case per claim, 2026-11-07, both versions
([differences-from-stylecop.md](differences-from-stylecop.md#same-reports-different-fix) has every rule):

- **No fix, or none that works there:** SA1108, SA1125, SA1613, SA1620, SA1621, SA1627 have no fix (41 of 1.3's 203
  diagnostics have none). SA1141's fix has no Fix All, so nothing changes; SA1142's throws on a `t?.Item1` and then
  fixes nothing in the project. SA1302, SA1314 and SA1316 have no Fix All under `dotnet format` either: nothing is
  renamed.
- **Renames:** the naming rules (SA1300-SA1314) lower or remove only the first letter or prefix (`MAX_VALUE` ->
  `MAXVALUE`, `m_Upper` -> `Upper`), and a parameter of an implementing method needs a second run. In 1.3 a
  `_name` whose new name collides isn't fixed, and which of the other renames lands depends on the order.
- **Fixes that change more than the finding:** SA1501/SA1502 write `{` at column 1 in a block nested in an `if` or a
  lambda and split `} while (x);`; SA1504 collapses or expands the accessors of every property in the project the same
  way (which way differs between runs); SA1633 replaces a plain comment header (license text) with the XML header.
- **More runs:** SA1120, SA1103, SA1516 (two members on one line) and nested accessors under SA1501/SA1502 leave
  work for the next run.

1.3 fixed what these pages said about 1.2 for SA1114, SA1115, SA1117 and SA1612 (fixes now), SA1413 (nested
initializers in one run, was three: [#3953](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3953)), SA1116
(a moved lambda's body now moves with it), SA1504's fix deleting a `//` comment, and line endings (1.2's fixes wrote
CRLF into LF files: 44 lines in one SA1501 case; 1.3's write none). Two claims these pages made about 1.2 didn't
reproduce on either version: SA1132's fix copies the attributes and documentation to every field, and SA1316's fix
doesn't run under `dotnet format` at all (instead of renaming only the declaration).

## Coverage

StyleCop 1.3.177-beta has 203 diagnostics, 6 more than 1.2.0-beta.556 (SA1143, SA1315, SA1317, SA1653, SA1654,
SX1412), read from its DLLs ([stylecop-mapping.md](stylecop-mapping.md), [skipped-rules.md](skipped-rules.md);
counted from `scripts/stylecop-survey/data/mapping.csv`):

| | Diagnostics | |
|---|---:|---|
| StyleBro rule | 109 | Differences listed in [differences-from-stylecop.md](differences-from-stylecop.md) |
| .NET SDK rule | 44 | Spacing, indentation, using order, modifier order and more: the preset sets the options, `bro init` the severities |
| Dropped: no safe automatic fix | 34 | [skipped-rules.md](skipped-rules.md#dropped-no-safe-automatic-fix) |
| Dropped: StyleCop never reports it | 10 | SA1109, SA1126, SA1301, SA1409, SA1628, SA1630-SA1632, SA1644, SA1650 (probed on 1.3.177-beta; SA1628 and SA1644 are empty analyzers in its source) |
| Not yet: new in 1.3 | 4 | SA1143, SA1315 and SA1654 have a close StyleBro rule ([BRO1524](rules/BRO1524.md), [BRO1313](rules/BRO1313.md), [BRO1617](rules/BRO1617.md)) that `bro migrate` doesn't map yet; SX1412 is the SDK's `charset = utf-8`, which `bro migrate` doesn't write yet |
| Not applicable | 2 | SA0001, SA0002 |

The SDK column was re-measured on 1.3.177-beta (`Test-SdkCoverage.ps1`): the same verdicts as on 1.2.0-beta.556 for
every rule, plus SX1412.

Why the 34 are dropped ([skipped-rules.md](skipped-rules.md) has every one, with what revisiting would take):

- **They need text only a person can write.** Missing documentation (SA1601, SA1602, SA1604-SA1611, SA1614-SA1616,
  SA1618, SA1619, SA1622): the only automatic fix is placeholder text, which satisfies the rule and documents nothing
  (SA1600 alone: ~28,000 findings in the three surveyed StyleCop repos). StyleBro adds `/// <inheritdoc/>` to
  overrides and interface implementations, where that's a real fix (BRO1601). Names with non-Latin letters (SA1317,
  new in 1.3) need a name a person chooses.
- **They need code moved between files or files renamed** (SA1402, SA1403, SA1649), which `dotnet format` can't do.
- **They have no deterministic fix:** SA1118 (where to break a multi-line argument), SA1401 (fields should be private),
  SA1404-SA1406 (justification and message text), SA1414 (tuple element names), SA1653 (which part of a partial type
  keeps the `<summary>`, new in 1.3), SA1625, SA1645-SA1648 and others.

## Speed

Measured 2026-11-07 with [scripts/benchmark](https://github.com/bisforboman/stylebro/blob/main/scripts/benchmark/README.md)
in `compare` mode (`Compare-Releases.ps1 -Versions stylecop:1.3.177-beta,stylecop -Runs 20 -Repeat 3`) on
Newtonsoft.Json's main project (`Src/Newtonsoft.Json` at `52fa3ae`, 242 files): both analyzer DLLs load into one
process and take turns on freshly parsed sources (which goes first alternates, garbage collection before each),
single-threaded, compiler telemetry per analyzer, each analyzer's fastest of 20 runs, summed. StyleCop:
`StyleCop.Analyzers.dll` from the StyleCop.Analyzers 1.3.177-beta package (and, for comparison, from
StyleCop.Analyzers.Unstable 1.2.0.556, what 1.2.0-beta.556 installs). StyleBro: a Release build of
`src/StyleBro.Analyzers` from `main` (`5aa09f5`). Each tool runs with its default rule set and no configuration. A
cloud container with 4 cores, nothing else running.

| Run | StyleCop 1.3.177-beta (188 analyzers) | StyleBro (76 analyzers, 129 rules) | Ratio | StyleCop 1.2.0-beta.556 (182 analyzers) |
|---|---:|---:|---:|---:|
| 1 | 1,077 ms | 356 ms | 3.0x | 1,695 ms |
| 2 | 1,065 ms | 353 ms | 3.0x | 1,709 ms |
| 3 | 1,057 ms | 347 ms | 3.0x | 1,720 ms |

(StyleBro's column is from the 1.3 runs; in the 1.2 runs it took 351-357 ms.) 1.3 is about 38 % faster than 1.2:
SA1121 went from 109-112 ms to under 32 ms, SA1027 from 73-74 ms to 39 ms. StyleCop 1.3's slowest: SA1101 (121-129
ms), SA1000 (56-57 ms), SA1625 (45 ms). StyleBro's: the shared tree walk (46 ms), DocumentationAnalyzer (39 ms),
BlankLineRunsAnalyzer (23 ms) (run 1).

Not the same work: StyleCop's number includes spacing and indentation rules that StyleBro leaves to the SDK (IDE0055 and
the other rules `init` turns on), whose cost isn't in StyleBro's number. In a real build analyzers run concurrently with
the compiler, so the wall-clock difference is smaller: a private app built in the same time with and without StyleBro
(9-12 s either way), Newtonsoft.Json 8.4 -> ~11.8 s with every StyleBro rule ([performance notes](https://github.com/bisforboman/stylebro/blob/main/docs/notes/performance.md), 2026-10-02, alpha.8).

## Correctness

- **StyleCop's bugs** ([differences-from-stylecop.md](differences-from-stylecop.md#stylecops-open-bugs)): on
  2026-10-04 every open issue on a rule StyleBro replaces was read, and the 54 that describe behavior StyleBro could
  share were run against StyleBro: 34 weren't shared, 8 were (7 fixed, 1 kept like StyleCop by the owner's decision).
  StyleCop has closed all but one of those 54 since. On 2026-11-07 each was rebuilt as a small case and run on
  1.2.0-beta.556 and 1.3.177-beta: 24 are fixed in 1.3 (for example SA1201 on C# 14 extension blocks,
  [#3955](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3955); SA1309's rename that didn't compile,
  [#2939](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2939); SA1135's crash on `using X = string;`,
  [#3882](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3882); CRLF in LF files,
  [#3656](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3656)), 3 changed, 13 are still there, and 14
  don't reproduce on 1.2.0-beta.556 either.
- **Parity with StyleCop:** 50 parity sets run both tools on edge-case files and compare positions and fixed output;
  every difference must be a documented deviation ([scripts/stylecop-survey](https://github.com/bisforboman/stylebro/tree/main/scripts/stylecop-survey)).
  The sets are written against 1.2.0-beta.556. Run against 1.3.177-beta, 30 of the 50 give the same results; in the
  other 20, 1.3 now does what StyleBro does for 15 rules where 1.2 didn't, and differs from it in 12 new places
  ([Changed in StyleCop 1.3](differences-from-stylecop.md#changed-in-stylecop-13)).
- **Real-world check:** every rule on 13 public repositories (`scripts/realworld/repos.psd1`), on every pull request
  that changes code. A pull request fails on an analyzer crash, a new compile error, a merge conflict marker, more
  than 2 `dotnet format` runs to converge, or a test that passed on the untouched code and fails after the fixes.
  Running the tests found what compiling didn't: renames that broke reflection and serialization, member sorting that
  changed serialized JSON order. Those cases are now skipped; one known failure is left (Newtonsoft.Json's
  `MemberSearchFlags` serializes an attribute-less class's private fields by reflection, in declaration order).
- **Mutation testing:** 2,224 guards (`scripts/mutation/entries/`) are each broken on purpose; a test must fail for
  every one.
- **Unit tests:** 2,871, including every example on every rule page (before compiles and is reported, one Fix All
  pass gives exactly the after, the after is clean).

## Migration

`bro` ([migrating.md](migrating.md)) reads a repository's StyleCop setup (rulesets, global configs,
`.editorconfig` files, `stylecop.json`, the StyleCop version: 1.0, 1.1, 1.2 or 1.3) and writes StyleBro and SDK settings
that match it, carries `#pragma warning disable SA...`, `[SuppressMessage]` and `<NoWarn>` suppressions over to the
replacing rules, and reports the StyleCop rules nothing enforces any more. Coming from 1.3:
[migrating.md](migrating.md#coming-from-stylecop-13).

Files `dotnet format` changes after `bro migrate --write` on repositories that were StyleCop-clean, measured
2026-11-07 with `scripts/stylecop-survey/Measure-MigrationDelta.ps1` (a plain `dotnet format` first as the baseline,
then `bro migrate --write`, then `dotnet format` with StyleBro and without StyleCop):

| Repository | StyleCop | Files changed | What |
|---|---|---:|---|
| Polly (`8a37f64`, 2026-11-07) | 1.2.0-beta.556 | 0 | |
| OpenTelemetry (`1fc102a`, 2026-10-28, the last commit before it needs .NET 11) | 1.3.151-beta | 1 | Two `[RequiresUnreferencedCode]`/`[RequiresDynamicCode]` attributes from the SDK's trimming analyzer (the measurement runs plain `dotnet format`), not a StyleBro rule |

Both with the global.json lowered to SDK 10.0.1xx. OpenTelemetry first gave 210 files: `bro migrate` didn't follow its
`$([MSBuild]::GetDirectoryNameOfFileAbove(...))` imports, so its rulesets weren't read and BRO1601 and BRO1506 came on
(fixed on `main` the same day). The earlier figures (2026-10-03: Polly 1, OpenTelemetry 1, a private app 7) were measured on
older commits of these repositories, both on StyleCop 1.2; the private app wasn't re-measured.

The rules StyleCop doesn't have are turned off by the migration, so they change nothing there.

- **Baseline** ([baseline.md](baseline.md)): `bro baseline` records today's violations; the build, the IDE
  and `dotnet format` ignore them and only new or edited lines count. On FFMpegCore with every rule: 820 violations on
  759 lines baselined, then 0 StyleBro errors in the build and `dotnet format --verify-no-changes` clean (2026-10-02).
  Whitespace formatting can't be baselined.
- **Multi-targeted projects:** StyleBro's Fix All merges each target framework's copy of a file itself, so its fixes
  write no conflict markers (Newtonsoft.Json, 8 frameworks). The SDK's IDE0055 fix crashes plain `dotnet format` there (reported upstream as [dotnet/roslyn#86000](https://github.com/dotnet/roslyn/issues/86000) and [dotnet/roslyn#86001](https://github.com/dotnet/roslyn/issues/86001));
  `bro format` runs `dotnet format` once per target framework (Serilog: 505 IDE0055 findings -> 0 in one
  run, 2026-10-03; [ci.md](ci.md#notes)).

## Beyond StyleCop

33 rules StyleCop doesn't have, not even in 1.3 ([differences-from-stylecop.md](differences-from-stylecop.md#rules-beyond-stylecop),
chosen in [beyond-stylecop.md](https://github.com/bisforboman/stylebro/blob/main/docs/beyond-stylecop.md) from Roslynator, Meziantou, Sonar and StyleCop's issue tracker),
each with a fix like every other rule. For example: null checks in one form (BRO1133), operator placement when an
expression wraps (BRO1520), split call chains one call per line (BRO1523), no `$`/`@` a plain string doesn't need
(BRO1138), no `else` after a jump (BRO1143, off by default). Three more were proposals in StyleCop's tracker that 1.3
now ships: BRO1313 (parameter names of overrides, like SA1315), BRO1617 (generic crefs in braces, like SA1654) and
BRO1524 (split conditional expressions, close to SA1143). `bro migrate` turns all of them off, so migrating changes
nothing until you opt in.

`bro init --modernize` ([modernizing.md](modernizing.md)) turns on the SDK's own rules for newer C# and
APIs in tiers, and StyleBro's multi-target guard hides a newer-API rule in a project where one target framework lacks
the API.

## Limits

What StyleBro doesn't do:

- **Report missing documentation** (SA1600 and friends), except overrides and implementations. If you rely on SA1600 to
  make people write docs, keep StyleCop's SA1600 for reporting or use the compiler's CS1591.
- **Move types into their own files or rename files** (SA1402, SA1403, SA1649).
- **Line length.** Neither does StyleCop; there's no deterministic fix (where to break).
- **Baseline whitespace formatting.**
- **Rename everything it reports:** a field whose name appears in a string elsewhere (reflection, serialization) keeps
  its name and its warning (Newtonsoft.Json: 49 such names, 2026-10-01).
- **Follow every 1.3 change yet:** 12 behaviors StyleCop changed in 1.3 (for example SA1407 checking `&`, `|`, `^` and
  `>>>`, SA1413 checking collection expressions) aren't in StyleBro; each is listed in
  [Changed in StyleCop 1.3](differences-from-stylecop.md#changed-in-stylecop-13).

Where it deliberately differs from StyleCop's defaults ([differences-from-stylecop.md](differences-from-stylecop.md#preset-choices)):
no `this.` prefix, usings outside the namespace, region removal (BRO1112) and namespace-name casing (BRO1312) off, and the
XML file header only once a company is configured. `bro migrate` keeps a team's own settings instead.

Checked and not checked:

- Visual Studio 2022 (17.13): build, light bulb fixes, Fix All and live squiggles checked by the owner (2026-10-05,
  [backlog.md](https://github.com/bisforboman/stylebro/blob/generated/docs/backlog.md)). **Rider: not checked.**
- Needs Roslyn 4.8 (.NET 8 SDK, Visual Studio 17.8) or newer.
- **Prerelease:** 0.8.0-alpha.1 on nuget.org; versions and behavior can still change between prereleases.
