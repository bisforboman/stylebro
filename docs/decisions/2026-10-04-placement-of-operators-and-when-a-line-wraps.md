# Placement of operators, `=>` and `=` when a line wraps (2026-10-04)

## Question

The first rules beyond StyleCop ([beyond-stylecop.md](../beyond-stylecop.md) #1 and #2) put a token on one side of a line
break: binary operators and `?`/`:` (Roslynator RCS0027/RCS0028), `=>` (RCS0032), `=` (RCS0052). What shape, which
tokens, and which defaults?

Facts: the SDK has `dotnet_style_operator_placement_when_wrapping` (default `beginning_of_line`) but no rule enforces
it. Probed with SDK 10.0.401: with the key set to `end_of_line`, `dotnet_diagnostic.IDE0055.severity = warning` and
beginning-of-line operators, `dotnet format --severity info` changed none of them, and the reverse (`beginning_of_line`
with end-of-line operators) didn't either; a build with `EnforceCodeStyleInBuild` reported no IDE diagnostic. Roslynator's
four rules are off by default and have no default value (their options must be set). There's no SDK key for `=>` or `=`.

Survey of the eight reference repositories (scripts/realworld/repos.psd1), tokens with a line break on one side,
beginning of line / end of line:

| Tokens | Beginning | End | Repositories (beginning / end) |
|---|---|---|---|
| `&&`, `\|\|` | 1,785 | 370 | Jellyfin 1,497/96, OpenTelemetry 165/175, Newtonsoft.Json 94/36, CsvHelper 17/17, FluentValidation 9/3, Serilog 3/33, Polly 0/10 |
| `+` | 134 | 168 | Newtonsoft.Json 41/15, Jellyfin 37/6, OpenTelemetry 56/62, CsvHelper 0/39, Polly 0/25, Serilog 0/18, FFMpegCore 0/3 |
| `??` | 128 | 27 | Jellyfin 63/18, OpenTelemetry 51/0, Newtonsoft.Json 10/5, FFMpegCore 4/2, Serilog 0/2 |
| other binary | 13 | 45 | Newtonsoft.Json 0/29, OpenTelemetry 1/9, Jellyfin 12/5, Polly 0/2 |
| `?` / `:` | 641 / 650 | 48 / 63 | beginning in every repository; Serilog closest (8/6) |
| `=>` expression bodies | 1,259 | 901 | beginning: OpenTelemetry 676/335, Jellyfin 398/14, FluentValidation 52/3, Serilog 2/0; end: Polly 130/543, FFMpegCore 0/3, CsvHelper 0/2; Newtonsoft.Json 1/1 |
| `=>` switch arms | 8 | 14 | OpenTelemetry 4/7, Jellyfin 3/5, Polly 0/2, Newtonsoft.Json 1/0 |
| `=>` lambdas | 9 | 5,299 | end everywhere (block bodies included) |
| `=` assignments and initializers | 6 | 518 | end in all eight |

All operators together: beginning in six repositories, end in Serilog, FFMpegCore even.

## Choices

1. **One rule per token kind, one analyzer and one fix:** BRO1520 operators (follows the SDK key), BRO1521 `=>`
   (`stylebro_arrow_placement_when_wrapping`), BRO1522 `=` (`stylebro_equals_placement_when_wrapping`); each can be
   turned off on its own.
2. **One rule with three options:** fewer ids, but a team that wants only the operator rule has to set the other two to
   "don't care", which needs a third option value.
3. **Two rules** like the survey's pairing (operators; `=>` and `=` together): `=>` and `=` have opposite majorities, so
   they need separate options anyway.

Proposed defaults: operators `beginning_of_line` (the SDK's default, and the survey's majority), `=>` `beginning_of_line`
(four repositories to three, 1,259 to 901 tokens), `=` `end_of_line` (518 to 6). Scope of `=>`: expression bodies and
switch arms; lambdas left out (5,299 to 9 at the end, and a block body's `{` belongs on its own line). Preset: all three
at warning; `stylebro-migrate` writes them as `none` (no StyleCop rule asks for them).

## Answer

**Choice 1** (user's decision, 2026-10-04), with these defaults, all configurable: operators `beginning_of_line` (the
SDK's default and the survey's majority), `=` `end_of_line` (the survey), `=>` `end_of_line` (the owner's choice over the
proposed `beginning_of_line`: one convention for every `=>`, like lambdas and StyleBro's own code). All three at warning
in the preset; off after `stylebro-migrate`.
