# Superseding StyleCop, and performance (2026-10-04)

## Question

StyleCop parity is done (every rule StyleBro, SDK or dropped with a reason). To properly supersede StyleCop, what else
goes on the backlog? And after the analyzer speed-ups (all 55 analyzers ~830 -> ~700 ms on Newtonsoft.Json), what
performance work?

## Choices

Backlog: (1) a sweep of StyleCop's open bug reports on the rules StyleBro replaces, each checked with a test; (2) the
remaining candidates from StyleCop's tracker (beyond-stylecop.md #6-#10, #12); (3) parity with StyleCop's unreleased
`master` (~20 commits that change what a replaced rule reports); (4) the remaining candidates from other analyzers
(survey #6-#10, #12-#18). Performance: a CI regression check, a FieldNamingAnalyzer deep dive (with a profiler), one
shared token pass for the layout analyzers, a per-file IDE responsiveness check.

## Decision

The owner: all four backlog items ("to properly supersede StyleCop we should have implemented most of their reported
issues/planned improvements too"), and the CI check, the FieldNamingAnalyzer deep dive and the shared token pass
("performance is an important metric for analyzers, you don't want them to be slow"). Not now: the per-file IDE check.
All are under Work in [backlog.md](https://github.com/bisforboman/stylebro/blob/generated/docs/backlog.md).
