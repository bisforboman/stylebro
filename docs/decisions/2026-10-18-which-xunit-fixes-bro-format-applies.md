# Which xUnit fixes bro format applies (2026-10-18)

## Question

xunit.analyzers ships with every xunit reference (backlog item 073). Should `bro format` (and `check`, `preview`,
`baseline`) also apply its code fixes, and which ones? Should `init` write anything for xUnit, should the baseline cover
xUnit ids, and do xunit v2 and v3 differ?

Survey (xunit.analyzers 1.18-2.2.0; CacheCow, Ardalis.Extensions, stateless with 2.2.0, and a probe with one finding per
rule): maintained repositories had no xUnit warnings at all (BitFaster.Caching, GuardClauses, Ardalis.Specification,
YoutubeExplode, Telegram.Bot: the build shows them, so they get fixed). Where they remain, the assertion fixes converged in
two runs to the same text in every order (they cascade: xUnit2004 leaves an xUnit2009/2017, xUnit2010 an xUnit2000, xUnit2000
an xUnit2013), and the repositories built and passed their tests. Two fixes changed behaviour: xUnit1013 added `[Theory]` to
CacheCow's `ILogger.Log` implementation (a new test with no data), and xUnit1030 (drop `ConfigureAwait(false)`) failed
stateless's `Ensure_XUnit_can_lose_sync_context` test.

## Choices

1. Ids: **an allowlist** (owner's pick) of fixes that keep the test's meaning; every xUnit id the repository enables; the
   allowlist behind a flag.
2. Borderline: xUnit1048 (`async void` -> `async Task`), xUnit1025 (duplicate `[InlineData]` removed): neither, one,
   **both** (owner's pick).
3. init and baseline: **nothing in init, the baseline covers the allowlist** (owner's pick); neither; init raises the
   Info-level assertion rules.
4. xunit v2 and v3: **the same list** (owner's pick); v3 also gets its migration fixes (xUnit1051 CancellationToken).

## Decision

`bro format` adds xUnit1014, xUnit1025, xUnit1048, xUnit2000, xUnit2003, xUnit2004, xUnit2006-xUnit2013, xUnit2015,
xUnit2017, xUnit2020 and xUnit2029-xUnit2031 to its `--diagnostics` list when the repository references xunit. `dotnet
format`'s `--severity warn` then fixes each only where the repository leaves the rule at warning or error, like
StyleBro's own. Left out: fixes that add or remove tests or change what runs (xUnit1013, xUnit1030, xUnit1051,
xUnit1004), fixes that change a parameter's type or data shape (xUnit1006, 1008, 1012, 1021, 1034), rules with two
competing fixes (xUnit1001, 1005), fixes that turn an assert that always fails into one that passes (xUnit2002, 2005,
2018), rules without Fix All and analyzer errors (a repository that builds has none). `init` writes nothing for xUnit;
the baseline suppressor covers the allowlist. `--all` still passes every fix through.
