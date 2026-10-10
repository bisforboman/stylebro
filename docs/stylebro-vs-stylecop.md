# StyleBro vs StyleCop.Analyzers

For a team deciding whether to switch. Measured numbers only; each one says where it comes from. As of
**2026-10-05** (StyleBro's `main`; StyleCop.Analyzers 1.2.0-beta.556, its latest release).

## Summary

| | StyleCop.Analyzers 1.2.0-beta.556 | StyleBro (`main`, 2026-10-05) |
|---|---|---|
| Fixes under `dotnet format` | Many rules have no fix, no Fix All, or a fix that misbehaves there ([below](#what-it-is)) | Every rule has a fix with Fix All; one run converges |
| StyleCop's 197 diagnostics | All | 106 by StyleBro rules, 45 by the .NET SDK's rules, 42 dropped by design, 4 variants or not applicable |
| Rules beyond StyleCop | None | 26 (most from Roslynator, Meziantou, Sonar or StyleCop's own tracker) |
| Analyzer time, Newtonsoft.Json (242 files) | 1,291-2,171 ms (182 analyzers) | 411-687 ms (66 analyzers); 3.1-3.4x faster in each of 3 runs ([Speed](#speed)) |
| StyleCop's open bugs on the replaced rules | 54 checked | 34 not shared, 8 shared (7 fixed, 1 kept on purpose), 12 not applicable or the same by design |
| Moving a StyleCop-clean repo | (the starting point) | Files changed after `stylebro-migrate --write` + `dotnet format`: Polly 1, OpenTelemetry 1, a private app 7; only 1 of those 9 is a StyleBro fix ([Migration](#migration)) |
| Baseline (fail only on new violations) | No | Yes (`stylebro-migrate baseline`) |
| Multi-targeted projects | Not measured | Fix All merges each target framework's copy itself (no conflict markers in Newtonsoft.Json, 8 frameworks); `stylebro-migrate format` works around the SDK's IDE0055 crash |
| Release status | 1.2.0-beta.556 (beta for years; last stable 1.1.118) | 0.5.0-alpha.1 on nuget.org (129 rules), a prerelease |

## What it is

StyleBro is a set of Roslyn analyzers whose every rule has a code fix that `dotnet format` applies, with Fix All, so
`dotnet format` fixes everything StyleBro reports. Fixes are deterministic and idempotent: after one run, a second run
changes nothing (the CI check for this repository and the [real-world check](#correctness) both test that). Where a fix
could be unsafe (it would change behavior, lose a comment, or depend on `#if` code), StyleBro skips the case instead of
reporting something it can't fix.

Formatting the SDK already handles (spacing, indentation, using order, modifier order, ...) isn't duplicated: StyleBro's
preset sets the SDK's options and `stylebro-migrate init` turns the SDK rules on.

StyleCop's fixes, run under `dotnet format`, from [differences-from-stylecop.md](differences-from-stylecop.md#same-reports-different-fix):

- **No fix, or none that works there:** SA1108, SA1114, SA1115, SA1117, SA1125, SA1612/SA1613/SA1620/SA1621/SA1627;
  SA1141's fix has no Fix All and SA1142's throws.
- **Renames:** the naming rules (SA1300-SA1314) lower or remove only the first letter or prefix (`MAX_VALUE` -> `MAXVALUE`), and under
  `dotnet format` apply part of the renames per run or nothing; SA1316 renames the declaration only, so every use
  breaks the build.
- **Fixes that lose or mangle code:** SA1132 keeps attributes on the first field only (`[Obsolete]` disappears from the others);
  SA1501/SA1502 write `{` at column 1 in nested blocks and CRLF into LF files; SA1504 collapses or expands the accessors
  of every property in the project the same way; SA1633 deletes a plain comment header (license
  text) when it writes the XML header.
- **More runs:** nested initializers need several runs for SA1413 (StyleCop issue
  [#3953](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3953): three); SA1120 and SA1103 leave work for
  the next run.

## Coverage

StyleCop 1.2 has 197 diagnostics ([stylecop-mapping.md](stylecop-mapping.md), [backlog.md](https://github.com/bisforboman/stylebro/blob/main/docs/backlog.md#summary)):

| | Diagnostics | |
|---|---:|---|
| StyleBro rule | 106 | 92 BRO rules replace them; differences listed in [differences-from-stylecop.md](differences-from-stylecop.md) |
| .NET SDK rule | 45 | Spacing, indentation, using order, modifier order and more: the preset sets the options, `stylebro-migrate init` the severities |
| Dropped: no safe automatic fix | 32 | [skipped-rules.md](skipped-rules.md#dropped-no-safe-automatic-fix) |
| Dropped: StyleCop never reports it | 10 | SA1109, SA1126, SA1301, SA1409, SA1628, SA1630-SA1632, SA1644, SA1650 |
| Variants, not applicable | 4 | SA1119_p, SX1309S, SA0001, SA0002 |

Why the 32 are dropped ([skipped-rules.md](skipped-rules.md) has every one, with what revisiting would take):

- **They need text only a person can write.** Missing documentation (SA1601, SA1602, SA1604-SA1611, SA1614-SA1616,
  SA1618, SA1619, SA1622): the only automatic fix is placeholder text, which satisfies the rule and documents nothing
  (SA1600 alone: ~28,000 findings in the three surveyed StyleCop repos). StyleBro adds `/// <inheritdoc/>` to
  overrides and interface implementations, where that's a real fix (BRO1601).
- **They need code moved between files or files renamed** (SA1402, SA1403, SA1649), which `dotnet format` can't do.
- **They have no deterministic fix:** SA1118 (where to break a multi-line argument), SA1401 (fields should be private),
  SA1404-SA1406 (justification and message text), SA1414 (tuple element names), SA1625, SA1645-SA1648 and others.

## Speed

Measured 2026-10-05 with [scripts/benchmark](https://github.com/bisforboman/stylebro/blob/main/scripts/benchmark/README.md) in `compare` mode on Newtonsoft.Json's main
project (`Src/Newtonsoft.Json` at `52fa3ae`, 242 files): both analyzer DLLs load into one process and take turns on
freshly parsed sources (which goes first alternates, garbage collection before each), single-threaded, compiler
telemetry per analyzer, each analyzer's fastest of 20-30 runs, summed. StyleCop: `StyleCop.Analyzers.dll` from the
StyleCop.Analyzers.Unstable 1.2.0.556 package (what StyleCop.Analyzers 1.2.0-beta.556 installs). StyleBro: a Release
build of `src/StyleBro.Analyzers` from `main` (`fb430c4`). Each tool runs with its default rule set and no
configuration.

| Run | StyleCop.Analyzers (182 analyzers, 197 diagnostics) | StyleBro (66 analyzers, 118 rules) | Ratio |
|---|---:|---:|---:|
| 1 | 2,171 ms | 687 ms | 3.2x |
| 2 | 1,467 ms | 435 ms | 3.4x |
| 3 | 1,291 ms | 411 ms | 3.1x |

Other builds were running on the machine at the same time, so the totals swing by up to 70 % between runs; the
alternation exposes both tools to the same noise, and the ratio held. StyleCop's slowest: SA1121 (159 ms), SA1101
(143 ms), SA1027 (109 ms). StyleBro's: FieldNamingAnalyzer (72 ms), DocumentationAnalyzer (66 ms),
CommentSpacingAnalyzer (59 ms) (run 1).

Not the same work: StyleCop's number includes spacing and indentation rules that StyleBro leaves to the SDK (IDE0055 and
the other rules `init` turns on), whose cost isn't in StyleBro's number. In a real build analyzers run concurrently with
the compiler, so the wall-clock difference is smaller: a private app built in the same time with and without StyleBro
(9-12 s either way), Newtonsoft.Json 8.4 -> ~11.8 s with every StyleBro rule (CLAUDE.md, 2026-10-02, alpha.8). An
earlier figure of 1,671 ms for StyleCop came from the benchmark before it parsed fresh sources per run and isn't
comparable.

## Correctness

- **StyleCop's open bugs** ([differences-from-stylecop.md](differences-from-stylecop.md#stylecops-open-bugs),
  2026-10-04): every open issue on a rule StyleBro replaces was read; the 54 that describe behavior StyleBro could share
  were run against StyleBro. 34 aren't shared, for example: SA1201's fix moves a method into `#if DEBUG`
  ([#1588](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1588)), SA1309's rename doesn't compile
  ([#2939](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2939)), SA1135 crashes on `using X = string;`
  ([#3882](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3882)), fixes write CRLF into LF files
  ([#3656](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3656)). 8 were shared: 7 fixed, 1 kept like
  StyleCop by the owner's decision.
- **Parity with StyleCop:** 42 parity sets run both tools on edge-case files and compare positions and fixed output;
  every difference must be a documented deviation ([scripts/stylecop-survey](https://github.com/bisforboman/stylebro/tree/main/scripts/stylecop-survey)). StyleCop's
  unreleased `master` was compared too: where it fixed a gap, StyleBro follows it.
- **Real-world check on every pull request:** every rule on 8 public repositories (FFMpegCore, Polly, OpenTelemetry,
  Newtonsoft.Json, Serilog, Jellyfin, FluentValidation, CsvHelper; `scripts/realworld/repos.psd1`). A pull request fails
  on an analyzer crash, a new compile error, a merge conflict marker, more than 2 `dotnet format` runs to converge, or a
  test that passed on the untouched code and fails after the fixes. Running the tests found what compiling didn't:
  renames that broke reflection and serialization, member sorting that changed serialized JSON order. Those cases are
  now skipped; one known failure is left (Newtonsoft.Json's `MemberSearchFlags` serializes an attribute-less class's
  private fields by reflection, in declaration order).
- **Mutation testing:** 320 guards (`scripts/mutation/mutations/`) are each broken on purpose; a test must fail for
  every one.
- **Unit tests:** 842, including every example on every rule page (before compiles and is reported, one Fix All pass
  gives exactly the after, the after is clean).

## Migration

`stylebro-migrate` ([migrating.md](migrating.md)) reads a repository's StyleCop setup (rulesets, global configs,
`.editorconfig` files, `stylecop.json`, the StyleCop version) and writes StyleBro and SDK settings that match it, carries
`#pragma warning disable SA...`, `[SuppressMessage]` and `<NoWarn>` suppressions over to the replacing rules, and
reports the StyleCop rules nothing enforces any more.

Files `dotnet format` changes after `stylebro-migrate --write` on repositories that were StyleCop-clean (CLAUDE.md,
"Migration re-measured with the SDK replacements", 2026-10-03; `scripts/stylecop-survey/Measure-MigrationDelta.ps1`):

| Repository | Files changed | What |
|---|---:|---|
| Polly | 1 | A file plain `dotnet format` already breaks (conflict markers in a multi-targeted file), not StyleBro |
| OpenTelemetry | 1 | A line ending in a file with mixed line endings |
| A private app | 7 | 6 line-ending-only, 1 BRO1104 on a target-typed `new()`, which the older StyleCop version used there misses (1.2 reports it) |

The rules StyleCop doesn't have are turned off by the migration, so they change nothing there.

- **Baseline** ([baseline.md](baseline.md)): `stylebro-migrate baseline` records today's violations; the build, the IDE
  and `dotnet format` ignore them and only new or edited lines count. On FFMpegCore with every rule: 820 violations on
  759 lines baselined, then 0 StyleBro errors in the build and `dotnet format --verify-no-changes` clean (2026-10-02).
  Whitespace formatting can't be baselined.
- **Multi-targeted projects:** StyleBro's Fix All merges each target framework's copy of a file itself, so its fixes
  write no conflict markers (Newtonsoft.Json, 8 frameworks). The SDK's IDE0055 fix crashes plain `dotnet format` there;
  `stylebro-migrate format` runs `dotnet format` once per target framework (Serilog: 505 IDE0055 findings -> 0 in one
  run, 2026-10-03; [ci.md](ci.md#notes)).

## Beyond StyleCop

26 rules StyleCop doesn't have ([differences-from-stylecop.md](differences-from-stylecop.md#rules-beyond-stylecop),
chosen in [beyond-stylecop.md](https://github.com/bisforboman/stylebro/blob/main/docs/beyond-stylecop.md) from Roslynator, Meziantou, Sonar and StyleCop's issue tracker),
each with a fix like every other rule. For example: null checks in one form (BRO1133), operator placement when an
expression wraps (BRO1520), split call chains one call per line (BRO1523), no `$`/`@` a plain string doesn't need
(BRO1138), generic crefs in braces (BRO1617), no `else` after a jump (BRO1143, off by default). The preset turns 22
of them on; `stylebro-migrate` turns them off, so migrating changes nothing until you opt in.

`stylebro-migrate init --modernize` ([modernizing.md](modernizing.md)) turns on the SDK's own rules for newer C# and
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

Where it deliberately differs from StyleCop's defaults ([differences-from-stylecop.md](differences-from-stylecop.md#preset-choices)):
no `this.` prefix, usings outside the namespace, region removal (BRO1112) and namespace-name casing (BRO1312) off, and the
XML file header only once a company is configured. `stylebro-migrate` keeps a team's own settings instead.

Checked and not checked:

- Visual Studio 2022 (17.13): build, light bulb fixes, Fix All and live squiggles checked by the owner (2026-10-05,
  [backlog.md](https://github.com/bisforboman/stylebro/blob/main/docs/backlog.md)). **Rider: not checked.**
- Needs Roslyn 4.8 (.NET 8 SDK, Visual Studio 17.8) or newer.
- **Prerelease:** 0.5.0-alpha.1 on nuget.org; versions and behavior can still change between prereleases.
