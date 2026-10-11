# Comments in declaration headers (StyleCop issue #605) (2026-10-04)

## Question

StyleCop issue #605 ([beyond-stylecop.md](../beyond-stylecop.md#from-stylecops-issue-tracker) #4) proposes SA1108's check
for declarations: no comment between a class's, namespace's, member's or accessor's header and its `{`. Extend BRO1132
(same id) or a new rule, and which declarations?

Survey of the eight reference repositories (scripts/realworld/repos.psd1), 50,403 declarations with a `{` (7,989 types,
2,462 namespaces, 29,151 methods/constructors/operators, 9,864 properties/indexers/events, 734 accessors, 203 local
functions): 9 with a comment between the header and `{`. 7 single-line headers with a trailing comment (methods: CsvHelper
1, Jellyfin 1, Polly 1, Serilog 1, mostly a note or issue link after a test name; types: Newtonsoft.Json 3, e.g.
`class ErrorPerson2 //:IPerson - oops!`), and 2 multi-line headers (Jellyfin's comment after the last parameter, Serilog's
ReSharper markers around a constructor initializer), which the BRO1132 rule for multi-line headers (decision A' below)
skips. None on accessors, property accessor lists, namespaces or local functions.

## Choices

1. **A new id, BRO1134**, sharing BRO1132's code (same analyzer and fix, same skips): `stylebro-migrate` maps BRO1132 to
   SA1108 and turns it on for repositories with SA1108 on, which shouldn't start changing declarations StyleCop never
   checked; teams can turn the two on separately.
2. **Extend BRO1132:** one id, but SA1108 then means more in StyleBro than in StyleCop, and migrated repositories change.

Scope: types (incl. enums, records with a body), block-scoped namespaces, methods/constructors/finalizers/operators with a
block body, a property's, indexer's or event's accessor list, accessor bodies, local functions. Comments after an
attribute (`[Fact] // flaky`) aren't in the header. Found on the way, fixed for both rules: with blank lines right after
`{`, the moved comment went above them, and a run that had already applied BRO1506 ("no blank line below a comment") left
one behind; it now goes below them, where BRO1503 removes them (FixOrderTests).

Preset: warning; `stylebro-migrate` writes it as `none`.
