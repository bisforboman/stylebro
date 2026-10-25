# Trial 55: the engines' finding-report differences go to a follow-up PR (2026-10-25)

## Question

Trial 55's engine soak (the host the default since #303, `--engine dotnet-format` next to it) gave identical trees,
exit codes, verdicts, builds and tests on all four repositories, but two output differences beyond the load-warning note:

- ZLinq: the per-run "files with findings" counts (old engine 71/220/220, host 69/218/218 in the net6.0, net8.0 and net9.0
  runs). In a run for a framework a project lacks (ZLinq.Tests' net6.0, ZLinq has none), the old engine loads the
  referenced ZLinq with all its frameworks (`StyleBroFormatLoose`, by design), so it also checks netstandard2.0-only code.
- protobuf-net.Grpc: on unformatted code the host's check lists 28 more findings than dotnet format (23 IDE0055 next to
  dotnet format's WHITESPACE errors, 5 BRO findings on lines an earlier fix changes): dotnet format analyzes again after
  each fix, the host keeps its first analysis. The same gap: `changesPerRule.IDE0055` 54 vs 31 in `--json`, the preview's
  "reported changes" 407 vs 384 (stable over two runs).

The soak is not clean (owner, decision of 2026-10-23). What happens with the differences?

## Choices

- **Follow-up PR** (recommended): make the host report like dotnet format (analyze again after the whitespace pass), and
  narrow the old engine's loose loading or accept it, in a PR of its own; the trial's PR stays as is.
- **Fix in this PR**: hold the trial's PR until the host's reporting matches dotnet format.
- **Accept as is**: the host's fuller list and counts are fine; document them as allowed differences.

## Decision

Follow-up PR (owner, 2026-10-25). Trial 55 itself: "Not clean" (three fixes), soak "Not clean".
