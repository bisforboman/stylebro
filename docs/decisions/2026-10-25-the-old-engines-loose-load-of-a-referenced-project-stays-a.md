# The old engine's loose load of a referenced project stays a documented difference (2026-10-25)

## Question

Trial 55's soak (backlog 076): on ZLinq the two engines list different "files with findings" counts in three runs (old
engine 71/220/220, host 69/218/218 in net6.0 part 2, net8.0 and net9.0). In the run for ZLinq.Tests' net6.0, ZLinq has no
net6.0: the old engine (`--engine dotnet-format`, the fallback since the host became the default) clears the forced
framework for it, so MSBuildWorkspace loads ZLinq with all five of its frameworks, and the run also checks and fixes two
netstandard2.0-only files (`Internal/SingleSpan.cs`, `Internal/Polyfill/ArraySegmentExtensions.cs`, BRO1519). The host
loads the instance the build uses (netstandard2.1). The net8.0 and net9.0 counts follow from it: a run checks again the
files an earlier run changed. The netstandard2.0 run fixes those files on both engines; trees, verdicts and the finding
lists are the same. The host matches the build. Fix the old engine or document the difference?

## Choices

1. **Document it** (recommended): an expected difference of the fallback engine, which stays for at least one release; its
   load logic (`StyleBroFormatLoose`, tuned in trials 34 and 54) stays as it is.
2. Fix the old engine: load a referenced project without the run's framework only for the framework the build picks.

## Decision

**Choice 1** (owner, 2026-10-25). The other difference trial 55 found (protobuf-net.Grpc, 28 more findings with the host)
was the host's and is fixed: a run's view now has one instance of every project the run names, also when a project
without the run's framework references it.
