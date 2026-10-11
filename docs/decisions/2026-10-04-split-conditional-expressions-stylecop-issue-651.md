# Split conditional expressions (StyleCop issue #651) (2026-10-04)

## Question

BRO1524 ([beyond-stylecop.md](../beyond-stylecop.md#from-stylecops-issue-tracker) #1): once a conditional expression is
split, the condition, `? a` and `: b` each start their own line. Which side of the line break do `?` and `:` go on, what
counts as split, and what about chains (`a ? x : b ? y : z`), the open question in #651 (a contributor wanted
`cond ? value :` per line allowed, the maintainer preferred the strict rule or a switch expression)?

Survey of the eight reference repositories (scripts/realworld/repos.psd1), 2,283 conditional expressions, 719 with a line
break right next to `?` or `:`:

| Shape | Count | Repositories |
|---|---|---|
| Every part on its own line, `?`/`:` first | 631 | all eight (Jellyfin 332, OpenTelemetry 189, Newtonsoft.Json 53, Polly 23, CsvHelper 22, Serilog 7, FluentValidation 4, FFMpegCore 1) |
| Every part on its own line, `?`/`:` last | 41 | Jellyfin 20, OpenTelemetry 15, Serilog 5, Polly 1 |
| A token with its line break on the wrong side only (BRO1520's) | 8 | Jellyfin 4, OpenTelemetry 4 |
| Comment next to `?`/`:` | 8 | |
| `?` or `:` sharing its line with both neighbors (this rule) | 31 | Jellyfin 19, Newtonsoft.Json 6, OpenTelemetry 4, FluentValidation 1, Serilog 1 |
| ... of which in chains | 18 | Jellyfin 11 (both list styles), Newtonsoft.Json 5 (`cond ? value :` per line), OpenTelemetry 2 |

Ten more conditionals had a part spanning lines (a lambda, a wrapped call) with no line break next to `?`/`:`; none had
a moved part spanning lines.

## Choices

1. **Each conditional on its own, chains skipped** (a conditional whose `: b` is a conditional, or that is one's `: b`):
   the 13 non-chain findings are fixed; the 18 chain links stay as their authors laid them out.
2. **Each conditional on its own, chains included:** a chain laid out as a list becomes a staircase (each link one level
   deeper), which nobody in the survey writes.
3. **Chains as one list** (each `: condition ? value` on its own line): a second layout to define and fix, for 18 cases.

Side of the line break: BRO1520's setting (`dotnet_style_operator_placement_when_wrapping`, default `beginning_of_line`,
the survey's majority 631 to 41), so the two rules never disagree. Split = a line break right next to `?` or `:` (not
inside a part). Indentation of the new line: like the first part that already starts a line. Preset: warning;
`stylebro-migrate` writes it as `none` (no StyleCop rule asks for it).

## Answer

Choice 1 (agent's proposal, 2026-10-04, for the owner to review).
