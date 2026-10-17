# The format host uses the SDK's own Roslyn and MSBuild (2026-10-17)

## Question

StyleBro builds against Roslyn 4.8, which can't parse C# 14, and the SDK's code-style analyzers don't load in an older
Roslyn than the SDK's own. Which Roslyn and MSBuild should the in-process host
([engine decision](2026-10-17-format-engine-an-in-process-host-in-stylebro-migrate.md)) use
([proposal](https://github.com/bisforboman/stylebro/blob/main/docs/proposals/format-speed.md), Q-D3)?

## Choices

1. **The SDK's own Roslyn and MSBuild, loaded at runtime from the SDK `global.json` selects**: the same compiler,
   code-style analyzers and fixers `dotnet format` and the build use; works with every future SDK. Costs an assembly
   load context, API limited to Roslyn 4.8's surface (anything newer through reflection), and a fallback to the child
   processes when the SDK's runtime isn't there. Before/after for users: none.
2. Ship a Roslyn version in the tool: simpler code, but the SDK's code-style analyzers don't load in an older Roslyn, and
   C# newer than the shipped Roslyn doesn't parse: every new SDK would need a tool release first.

## Decision

**Choice 1** (owner's choice 2026-10-17).
