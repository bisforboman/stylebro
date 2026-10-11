# IDE0370 in `init --modernize` (2026-10-05)

## Question

The SDK 10's IDE0370 (redundant `!`) does work in builds and `dotnet format` when its severity is set explicitly.
Should `init --modernize` turn it on next to BRO1147?

## Choices

1. No, BRO1147 only.
2. Yes, in tier B.

## Decision

The owner: no, keep BRO1147 only (choice 1). IDE0370 decides all-or-nothing per member, and in a multi-targeted probe
its fix left the `!` while the build still reported it; BRO1147 skips multi-targeted projects. The comparison is on
[BRO1147.md](../rules/BRO1147.md).
