# Configurations like frameworks (2026-10-16)

## Question

Trial 25: `stylebro-migrate format`, `--verify-no-changes`, the preview and `baseline` loaded every project in its
default configuration (Debug), so code under `#if !DEBUG` or the `#else` of `#if DEBUG` (Flow Launcher's Win32.cs,
MimeKit's Benchmarks/Program.cs) was never fixed or checked. The check said clean, and the CI's Release build with
warnings as errors failed. Should `format` cover the other configurations, and how?

## Choices

1. Configurations like frameworks: where a project's code tests DEBUG/RELEASE/TRACE (or its configurations'
   DefineConstants differ), add a run for Release (and any other configuration the solution builds, within reason) for
   those projects, grouped with the frameworks by how the code parses (the `#if`-symbol comparison), so nothing runs twice.
2. A `--configuration` option: one configuration per command, the user runs it twice.
3. Leave it: document that only Debug is checked.

## Decision

**Choice 1: Configurations like frameworks** (owner's choice, 2026-10-16). Only projects whose files test a symbol that
isn't a framework's (`NET8_0_OR_GREATER` and the like are the same in every configuration) are evaluated per
configuration: their `Configurations` (the SDK's Debug;Release) that the solution builds (.sln: its
SolutionConfigurationPlatforms; .slnx: its BuildType elements, else Debug and Release), at most four besides the
default. A configuration gets a run for a project and framework only where the project's files parse differently there
(the symbols they test that are defined, language version, nullable context) than in its default runs or a
configuration run chosen before. The run sets `Configuration` in the environment, like the framework runs set
`TargetFramework`, so the project file's own conditions see it. `baseline` runs its style and analyzer passes for those
configurations too. `format` names them (`Configurations: runs for Release (Repro) too ...`).
