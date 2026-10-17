# Private field names in any string when only a field has them (2026-10-17)

## Question

Trial 29 (Harmony): the tests keep private field names in a list (`["_root", "_type", "_info", "_method", "_params"]`)
and read each with `AccessTools.DeclaredField(typeof(Traverse), name)` in a loop. An earlier decision lets only
"reflection-shaped" strings (`GetField("_x")`, a method with Field in its name, `[MemberData]`, ...) keep a private
field's name, so BRO1303 renamed `_root` to `root` and three Traverse tests failed. Should the guard widen?

## Choices

1. **Underscore names anywhere**: a string that is exactly a private field's name and looks like a field name (a leading
   `_`, or a one-letter prefix like `m_`, `s_`, `t_`) keeps it wherever it appears.
2. Any exact match: any string equal to a private field's name keeps it (common words would block many renames again).
3. Leave as is, and document it as a known limit.

## Decision

**Choice 1: Underscore names anywhere** (owner's choice, 2026-10-17). `CamelCaseRenamer.MatchInStrings` keeps a field
whose name `LooksLikeFieldName` and is any string in the solution (or the names file a per-framework run reads); other
private names still need a reflection-shaped string, so words like `"options"` don't block renames (Orleans).
