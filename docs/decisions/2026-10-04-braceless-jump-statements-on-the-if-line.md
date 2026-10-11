# Braceless jump statements on the `if` line (2026-10-04)

Agent's proposal, 2026-10-04, for the owner to review.

## Question

StyleCop issue [#1563](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1563) asks for upper-case literal
suffixes (`50l` -> `50L`, `50.4m` -> `50.4M`). In the issue, maintainers and commenters wanted `L` but preferred lower
case for `m`/`f` (and the exponent's `e`); Sonar's S818 reports only `l` and leaves `ul` alone; the compiler warns
(CS0078) about `l` only. Which suffixes should the rule cover?

## Choices

1. **Integer suffixes** (`u`, `l` and their combinations): `1l` -> `1L`, `1u` -> `1U`, `1ul` -> `1UL`. One way to write
   an integer suffix; real suffixes stay as written.
2. **`l` only** (S818, CS0078): the one letter that looks like a digit.
3. **Every suffix** (the issue as written): also `f`, `d`, `m`.

## Answer (proposed)

**Choice 1** (BRO1135). Survey of the 8 reference repositories (line-based, outside strings and comments): lower-case
integer suffixes only in OpenTelemetry (24 `u`, 1 `ul`, e.g. `0x9E3779B1u`), no `l` anywhere; real suffixes are
written in lower case almost everywhere (e.g. Newtonsoft.Json 517 lower vs 40 upper, Jellyfin 92 vs 0), so choice 3
would rewrite hundreds of literals against the common style. Choice 2 would report nothing in these repos; choice 1
finds 25. Not a StyleCop rule, so `stylebro-migrate` turns it off; the preset turns it on.
StyleCop issue [#2252](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2252) (12 reactions; #1175 asked
the same) wants `if (x) return;` without braces while every other child statement still needs them. Which statements,
where, and how does it combine with the other brace rules?

## Choices

1. **Jump statements on the `if` line only** (the request as written): `return`, `throw`, `break`, `continue`, plus
   `goto` (also a jump statement in the C# spec) and `yield break` (leaves an iterator like `return`). Not on the next
   line (a statement added below looks like part of the `if`), not after `else`.
2. **Any single-line statement on the `if` line** (`if (x) a++;`). The issue's author ruled this out explicitly.
3. **A second option for "next line too"** (sharwell's question 2 in the issue). Nobody asked for it: those who wanted
   the next line wanted it for every statement, which `csharp_prefer_braces = when_multiline` already allows.

## Answer (proposed)

**Choice 1**, as `stylebro_allow_single_line_jump_statements = true|false` (default `false`: StyleCop's behavior). It
only exempts statements from BRO1514. In an `if`/`else` chain where another clause has braces or gets them in the same
run, BRO1516 still wants them on the jump too, so the chain ends up consistent in one `dotnet format` run.
`when_multiline` already allows all single-line children (the option adds nothing there), and BRO1508/BRO1509 aren't
involved (no block, so nothing to collapse or expand). No `stylecop.json` setting maps to it.

Survey of the 8 reference repos (line-based count of braceless `if (...) <jump>;` on one line / jump on the next line /
another statement on the `if` line): CsvHelper 27 / 0 / 0, FluentValidation 22 / 13 / 2, Newtonsoft.Json 19 / 33 / 1,
Serilog 31 / 81 / 0, FFMpegCore, Jellyfin, OpenTelemetry and Polly 0 / 0 / 0. Where braceless `if`s exist, the
same-line form is almost always a jump, which is what the option allows.
