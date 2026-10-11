# No blank line after attributes (StyleCop's proposed SA1521) (2026-10-04)

## Question

BRO1525 ([beyond-stylecop.md](../beyond-stylecop.md#from-stylecops-issue-tracker) #2): StyleCop's issue #738 proposed SA1521,
"attributes defined for type or member should never be followed by blank line", and never implemented it (no
documentation page, no analyzer). Which gaps count (before the element only, or also between two attribute lists of
one element), what about comments in the gap, and does it conflict with BRO1505 (elements separated by a blank line)?

Survey of the eight reference repositories (scripts/realworld/repos.psd1), 25,160 attribute lists on members, 36 on
accessors, 2,539 on parameters:

| Gap after an attribute list | Count | Repositories |
|---|---|---|
| Blank line, then the element | 8 | Jellyfin 5, Newtonsoft.Json 1, OpenTelemetry 1, Polly 1 (mostly `[InlineData]` rows above a test method) |
| Blank line, then the next attribute list | 1 | Jellyfin |
| Blank line and a comment, then the next attribute list | 9 | Jellyfin (comments grouping `[InlineData]` rows) |
| Blank line and a comment, then the element | 1 | Jellyfin |
| On accessors or parameters | 0 | |

BRO1505 doesn't count these blank lines (an element's code starts at its first attribute), so a member whose only blank
line sits below its attribute is already a BRO1505 finding; the two fixes edit different lines and converge in either
order (FixOrderTests).

## Choices

1. **Every attribute list except assembly/module ones, before the element and between lists; gaps with a comment,
   documentation comment or directive skipped** (BRO1504 wants a blank line above a comment that follows code; an `#if`
   changes which element the attribute belongs to). The 9 commented `[InlineData]` groups stay.
2. **Only before the element:** stacked lists may be grouped with blank lines; 1 finding in the survey.
3. **Also comments:** remove the blank line above the comment; conflicts with BRO1504.

Preset: warning; `stylebro-migrate` writes it as `none` (StyleCop never shipped SA1521, so a StyleCop-clean repository
may have such blank lines).
