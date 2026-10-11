# Summary layout: one line or three (2026-10-04)

## Question

[beyond-stylecop.md](../beyond-stylecop.md) #5: should a `<summary>` be written on one line
(`/// <summary>Gets the name.</summary>`) or with its tags on lines of their own, which elements does the rule check,
and what's the default?

Facts: Roslynator's [RCS1253](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1253) (off by default, needs
`roslynator_doc_comment_summary_style = single_line | multi_line`) checks only `<summary>`: `multi_line` reports a
summary written on one line; `single_line` reports a multi-line one whose text is one line, unless it has `<code>`,
`<list>` or `<para>`, with no length limit. Meziantou's
[MA0211](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0211.md) (off by default) reports
non-empty one-line summaries; [MA0177](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0177.md)
(off by default) reports any top-level element (summary, param, returns, ...) on several lines whose content is one
line and whose joined line fits within `max_line_length` (no limit when it isn't set). None of them moves the tags of a
summary whose text starts or ends on a tag's line (`/// <summary>Draws the shape` + `/// on the screen.</summary>`).

Survey of the eight reference repositories (scripts/realworld/repos.psd1), summaries whose text is one line (no
`<para>`/`<code>`/`<list>`), one-line form / tags on their own lines: Jellyfin 19/8,507, Newtonsoft.Json 11/1,650,
Polly 26/1,360, OpenTelemetry 75/1,163, CsvHelper 15/725, Serilog 1/456, FluentValidation 1/380, FFMpegCore 0/88;
in all 148/14,329. No summary had text on a tag's line. (StyleBro's own code is the other way round: 291/4.)

## Choices

1. **One rule, summary only, `stylebro_summary_layout = multi_line | single_line_when_fits`**, `max_line_length` for
   "fits" (no limit when unset, like MA0177); a summary with text on a tag's line gets its tags on lines of their own
   in both layouts.
2. Every top-level element (like MA0177): `<param>`, `<returns>` and `<remarks>` are usually written on one line even
   by teams that write summaries on three, so one option for all would fight most codebases.
3. A default limit (say 120) when `max_line_length` isn't set: the rule only joins text that is already one line, so
   the joined line is the text's line plus about 20 characters; a limit nobody configured would be a guess.

Proposed default: `multi_line` (the survey: 99% in every repository), BRO1616 at warning in the preset, off after
`stylebro-migrate` (no StyleCop rule asks for it). StyleBro's repository sets `single_line_when_fits` for its own code.

## Answer

**Choice 1**, default `multi_line`. Default chosen from the survey (agent's proposal, 2026-10-04, for the owner to
review); configurable. Implemented as [BRO1616](../rules/BRO1616.md).
