# README: where the rule list goes (2026-10-06)

## Question

The README's rule table had grown to 124 rows, too long for a start page. Where should the list go, and should the
contributor sections stay?

## Choices

1. The full table on its own page (`docs/rules/README.md`, grouped by area), a six-row summary in the README.
2. Keep the table in the README, each area collapsed.
3. Only a link from the README.

And: move Repository layout / Developing / Roadmap to `CONTRIBUTING.md`, or keep them.

## Decision

The owner: choice 1, and move the contributor sections to `CONTRIBUTING.md`. The index adds a Default column (on,
off, off in the preset); `DocExamplesTests.TheRuleIndex_ListsEveryRulePage` keeps it complete, and New-Backlog.py
reads the titles from it.
