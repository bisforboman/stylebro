# Microsoft style: open questions answered (2026-10-16)

## Question

The seven open questions of [the Microsoft style proposal](../proposals/microsoft-style.md#5-questions-all-answered-a-on-2026-10-16),
each with options and a recommendation.

## Choices

The options of each question are in the proposal; A was recommended for all seven.

## Decision

The owner picked **A** for every question:

1. **StyleBro rules the sources don't mention:** runtime's build decides, through `eng/CodeAnalysis.src.globalconfig` and
   `stylebro-migrate`'s SA-to-BRO mapping. StyleBro's rules beyond StyleCop are off.
2. **Severities `init --style microsoft` writes:** runtime's build severities for the ids whose options the style sets.
3. **`init` without `--style`:** suggests the style in its report when the code looks like runtime's. It never applies it.
4. **Name:** `<StyleBroStyle>microsoft</StyleBroStyle>`; the docs say it is dotnet/runtime's coding style.
5. **Following runtime's changes:** pinned per StyleBro major. Changes to runtime's guide are listed as known differences
   until then.
6. **Microsoft Learn variant:** not now.
7. **When:** it ships after gaps G1-G3, independent of 1.0, with a trial on runtime-style repositories first. The work
   starts after the trial 24 fix PRs merge.
