# A bulk silent severity and StyleBro's rules (2026-10-13)

## Question

sharpcompress sets `dotnet_analyzer_diagnostic.severity = silent` for all C# files and turns the analyzers it wants on
by id. After `init --write`, `stylebro-migrate format` said clean and `--verify-no-changes` exited 0, but `dotnet build`
failed with ~40,000 BRO errors (TreatWarningsAsErrors). `dotnet format` reads severities from .editorconfig only, so the
bulk `silent` hid every StyleBro rule from it. The build takes the preset's global config, whose entry per rule wins over
a bulk setting. How should init and format handle a bulk (or category) severity below warning?

## Choices

- Keep StyleBro's rules on: init writes `dotnet_diagnostic.BROxxxx.severity` (the preset's severity) for every rule the
  preset turns on and the bulk or category severity would hide, so `dotnet format` and the build see the same rules.
- Read the bulk `silent` as "only what we enable by id" and turn StyleBro off as well (preset `none` or every rule
  `none`).
- Leave the block alone and only warn.

## Decision

Agent's proposal (trial 18): keep StyleBro's rules on and set them by id. Installing StyleBro and running `init` is the
team enabling it explicitly. The bulk `silent` covers analyzers the team didn't choose (the SDK's CA rules come with
it), and sharpcompress turns each of those on by id too. Turning StyleBro off would make `init` do nothing. The lines go
into the init block under a comment that names the reason. A rule the .editorconfig already sets by id stays the team's
choice, and so do the lines the conventions or a Sonar setup write. To turn one off, add a line below the block.

`stylebro-migrate format` (also with `--verify-no-changes`) never reports clean while the .editorconfig files that apply
to the project hide preset rules the build reports. It names them, points to `init --write` and exits with the not-clean
code. A migrated repository (`StyleBroPreset` none) sets every rule by id, so nothing is hidden there. The check reads
the .editorconfig files instead of building the project, which would take minutes.
