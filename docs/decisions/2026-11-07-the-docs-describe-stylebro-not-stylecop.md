# The docs describe StyleBro, not StyleCop (2026-11-07)

## Question

The docs measured StyleBro against StyleCop.Analyzers 1.2.0-beta.556: a "Compared with StyleCop" section on every rule
page, docs/differences-from-stylecop.md (test-enforced: every rule and every preset value that differs from
StyleCop's defaults), docs/stylebro-vs-stylecop.md (coverage, speed, correctness), and a README pitch built on
"StyleCop writes you a ticket". StyleCop is active again since 2026-10-12 (1.3.1-beta to 1.3.177-beta in a month),
and claims such as "StyleCop has no fix for SA1114, SA1115, SA1117, SA1612" went stale within weeks (issue #335).
Re-measure against each 1.3 beta, or stop comparing?

## Choices

Asked of the owner as two multiple-choice questions (2026-11-07), after the owner proposed "remodel our docs to not
refer to StyleCop, as it can change". The SA-to-BRO ID mapping stays in every option: `bro migrate` reads each
rule's "Replaces StyleCop SAxxxx" from its description, and people coming from StyleCop look rules up by SA ID.

Q1, "how far should it go?":

1. **One dated page (Recommended)**: rule pages, README and guides describe StyleBro only; all behavioural
   comparisons move to one "Coming from StyleCop" page that names the version and date it was measured against.
2. **No comparisons at all**: keep only the ID mapping and the migration steps; delete differences-from-stylecop,
   stylebro-vs-stylecop and beyond-stylecop; no "StyleCop does X" claims anywhere.
3. **Rule pages only**: strip the rule pages' sections, leave the comparison pages for #335 to re-measure.

Q2, delivery: **decision + one PR (Recommended)**, decision first, or a plan only.

## Decision

The owner chose **no comparisons at all**, delivered as this decision and one PR:

- **What stays about StyleCop**: the ID mapping. Each rule page has one line `Replaces StyleCop **SAxxxx**.` naming
  exactly the SA IDs of the rule's description (a test checks it, and that the page names StyleCop nowhere else);
  docs/stylecop-mapping.md maps every StyleCop rule to a StyleBro rule, an SDK rule or "not covered";
  docs/skipped-rules.md says why StyleBro has no rule for the rest, in StyleBro's terms; docs/migrating.md keeps the
  steps of `bro migrate` (what it reads, what it writes).
- **What goes**: docs/differences-from-stylecop.md, docs/stylebro-vs-stylecop.md, every rule page's "Compared with
  StyleCop" section and "like/unlike StyleCop" wording, claims about StyleCop's fixes, defaults, bugs or versions,
  and the README's StyleCop-based pitch. The two tests that required every rule and every differing preset value on
  the differences page are removed; the migration's own tests still check what `bro migrate` writes.
- docs/beyond-stylecop.md was a survey of rule candidates (other analyzers, StyleCop's issue tracker), not a
  comparison: it moves to docs/notes/rule-candidates.md, off the site.
- The pitch states what holds whatever StyleCop does: one `dotnet format` run fixes everything StyleBro reports, every
  rule has a fix and Fix All, the SDK's `IDE` rules are used instead of copies, `bro init`, `bro baseline`,
  `bro migrate`.
- The StyleCop survey's data (teams, counts, defaults, StyleCop's fixes) stays under scripts/stylecop-survey/ for
  planning; it is not published as a claim.
- Issue #335's re-measuring (items 1 and 2) is no longer needed; its README point is covered by the new pitch.
- New rules: a rule page and the rules index, never a comparison. "Compared with other analyzers" sections are not
  covered by this decision.
