# Null check style: `x == null` or `x is null` (2026-10-04)

## Question

[BRO1133](../rules/BRO1133.md) ([beyond-stylecop.md](../beyond-stylecop.md) #3, Roslynator RCS1248) writes null checks in one
form. Which form by default, and what about `null == x`?

Facts: the SDK has no rule or option for it. `dotnet_style_prefer_is_null_check_over_reference_equality_method` only
covers `ReferenceEquals(x, null)` (IDE0041). Probed with SDK 10.0.401 (`dotnet format style` and `analyzers`,
IDE0041/IDE0150/IDE0083/IDE0078 at warning with their options on): `x == null`, `x != null`, `null == x` and
`n == null` (an `int?`) stayed as they were; only `ReferenceEquals(o, null)`, `!(o is null)`, `o is object` and
`s == null || s == ""` were rewritten, all into patterns. Nothing turns `x is null` into `x == null`.

Survey of the eight reference repositories (scripts/realworld/repos.psd1, text count of `*.cs` lines that aren't
comments; `is null` also counts a few strings such as messages):

| Repository | `== null` | `!= null` | `null ==` | `is null` | `is not null` |
|---|---|---|---|---|---|
| CsvHelper | 127 | 83 | 0 | 16 | 1 |
| FFMpegCore | 21 | 21 | 0 | 3 | 3 |
| FluentValidation | 68 | 174 | 0 | 1 | 0 |
| Jellyfin | 38 | 94 | 0 | 1,035 | 1,371 |
| Newtonsoft.Json | 638 | 948 | 4 | 5 | 1 |
| OpenTelemetry | 193 | 364 | 0 | 65 | 74 |
| Polly | 372 | 52 | 0 | 85 | 45 |
| Serilog | 45 | 59 | 0 | 14 | 1 |
| Total | 1,502 | 1,795 | 4 | 1,224 | 1,496 |

Equality operators in seven of the eight repositories (3,301 checks); patterns in Jellyfin, which alone has most of the
2,720.

## Choices

1. **`equality_operator` by default**, `pattern_matching` as the option (`stylebro_null_check_style`).
2. **`pattern_matching` by default**: the newer form, never calls a user-defined `==`; needs C# 9 for `is not null`.
3. **Off by default**, on only when the option is set (like Roslynator).

`null == x`: with `pattern_matching` it becomes `x is null` directly (there is no `null is x`); with
`equality_operator` it is left to BRO1103. Both rules report `null == x` in pattern mode, and their fixes converge in
one `dotnet format` run (BRO1133 finds the check again by its start, which the swap keeps; samples/Messy).

## Answer

**Choice 2:** `pattern_matching` (owner's decision, 2026-10-04): the form that never calls a user-defined `==`;
configurable; records skipped. The survey's majority (choice 1, the agent's proposal) stays available as
`equality_operator`. At warning in the preset; off after `stylebro-migrate`.
