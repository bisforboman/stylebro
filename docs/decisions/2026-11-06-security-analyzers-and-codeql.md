# Security analyzers and CodeQL (2026-11-06)

## Question

The vulnerable-package gate (2026-11-06-vulnerable-packages-fail-every-build.md) keeps known-bad packages out, but
nothing scanned StyleBro's own code for security bugs: the `bro` tool reads and writes users' files, starts `dotnet`
processes and reads XML (project files, `.editorconfig`, StyleCop settings). The .NET SDK ships analyzers for that
(category Security: CA2xxx, CA3xxx, CA5xxx) but enables only a few by default, as warnings.

Asked of the owner after the two questions of the vulnerable-package decision (2026-11-06, multiple-choice prompt):
"Should I also add scanning of your own code?"

## Choices

1. **Security analyzers, all 5 (Recommended)**: the SDK's Security rules, all of them, as errors, in all five of the
   owner's repositories.
2. **CodeQL on StanzaSharp, stylebro**: GitHub's code scanning on the two public repositories (free there).
3. **CodeQL on Svarta**: the private repository; needs paid GitHub Code Security.
4. **Not now.**

## Decision

The owner chose the security analyzers in all five repositories, and CodeQL on StanzaSharp and stylebro (not on
Svarta). For StyleBro:

- Directory.Build.props sets `AnalysisModeSecurity` to `All` (the SDK adds its global config enabling every Security
  rule) and the SDK's private property `_GlobalAnalyzerConfigFileName_MicrosoftCodeAnalysisNetAnalyzersSecurity_WarnAsErrorSuffix`
  to `_warnaserror`, which picks that config's variant with each rule at error: what `CodeAnalysisTreatWarningsAsErrors`
  would pick, but for Security only (that property would also make every other category's default rules errors). Every
  build fails on a finding: local, CI, the pack and the release.
- `dotnet_analyzer_diagnostic.category-Security.severity = error` in .editorconfig doesn't work (Svarta tried it
  first): the SDK's global config sets each rule's severity by id, and a rule's own severity wins over its category's.
- The SDK turns its analyzers off for netstandard2.0 and gives no analysis level there, which is what
  StyleBro.Analyzers and StyleBro.CodeFixes target. So the props also set `EnableNETAnalyzers` to true and
  `AnalysisLevelSecurity` to `latest`: every project gets the SDK's newest Security rules, not those of its target
  framework (net8.0 projects would otherwise get level 8's).
- Since the suffix property is the SDK's, not a documented one, `_StyleBroCheckSecurityAnalyzers`
  (Directory.Build.targets, after CoreCompile) fails the build unless the .NET analyzers are on and the Security config
  in use exists and ends in `_all_warnaserror.globalconfig`: an SDK that renames it is noticed at once, not by the
  rules quietly going back to warnings. Checked when this was set up: a temporary `MD5` call failed a plain build with
  CA5351 (in a net8.0 and in the netstandard2.0 project), and clearing the suffix failed it with the target's error.
- Scope, as for the NuGet audit: StyleBro's own projects only (src/, tests/, scripts/benchmark, the StyleCop survey's
  inventory). samples/ (users' code shapes, checked by StyleBro's own rules in verify-format) and scripts/fixtures/
  (a package packed for the fixture matrix) are left out by path; .trials/ and samples/StyleCopMigration have props of
  their own, and the fixture, real-world and verify-package runs build outside the repository. The package
  (build/StyleBro.Analyzers.targets) and the presets are unchanged: StyleBro doesn't turn these rules on for its users.
- A finding that is a real weakness is fixed (the owner decides how); a false positive is suppressed where it is, as
  narrowly as possible (`[SuppressMessage]` with a justification, or one path-scoped .editorconfig section), never by
  turning a rule off for the repository.
- `.github/workflows/codeql.yml`: CodeQL for C# in build-free mode, on pull requests to main (not those changing only
  Markdown or docs/), pushes to main, weekly (Monday 04:23 UTC) and on dispatch; actions pinned by commit SHA,
  `security-events: write` only on its job. Not a required check. It is an advanced setup, so code scanning's default
  setup (Settings, Code security) must be off, or its upload fails: the owner's step.

Findings when the analyzers were turned on: one rule, CA5392 (P/Invokes without `DefaultDllImportSearchPaths`), three
times in `src/StyleBro.Migrate/ChildProcesses.cs` (the Windows job object's `CreateJobObject`,
`SetInformationJobObject`, `AssignProcessToJobObject` from kernel32.dll). kernel32.dll is a known DLL that Windows
loads from System32 anyway, so `[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]` on each changes nothing
and was added. Nothing else in src/, tests/ or scripts/ was reported.

## Consequences

- A new SDK with new Security rules may fail the build on update; those findings are triaged like the first ones.
- The analyzers and CodeQL find patterns, not every bug: they don't replace reading the code that handles users' files.
