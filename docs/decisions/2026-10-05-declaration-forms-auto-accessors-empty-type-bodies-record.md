# Declaration forms: auto-accessors, empty type bodies, `record class` (2026-10-05)

## Question

Three candidates from the survey of newer rules (Roslynator RCS0042, Meziantou MA0206 beyond records, MA0174/MA0175):
build them, and with which defaults? The empty type body (`class Marker;`) needs C# 12, which a multi-targeted project
without `LangVersion` doesn't have in its older frameworks.

## Choices

1. BRO1527 auto-accessors on one line: on in the preset, or off.
2. BRO1145 empty class/struct/interface body: on with a multi-target guard, or off by default.
3. BRO1146 `record class` -> `record` (or the opposite style as an option): on, or off.

## Decision

The owner: BRO1527 on in the preset (off after `stylebro-migrate`), only lists of auto-accessors without bodies,
attributes or comments; it must converge with BRO1505/BRO1510/BRO1509 and not fight IDE0360. BRO1145 off by default
(`isEnabledByDefault: false`, preset `none`), skipped below C# 12 and, through the package's
`StyleBroTargetFrameworks`, in a project with several frameworks unless every one defaults to C# 12 or the project sets
`LangVersion`. BRO1146 on in the preset (off after `stylebro-migrate`), `record` only, no option for the opposite
style. Agent's choices: the package also passes `LangVersion` and `MaxSupportedLangVersion` (equal when the project
didn't set `LangVersion`); BRO1527 adds the blank lines BRO1505 (or BRO1519 when BRO1505 is off) wants next to the
multi-line property, so every fix order gives the same text, and is skipped when
`csharp_preserve_single_line_blocks = false` (the SDK formatter would expand the list again).
