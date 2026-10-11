# Renames to C# 14 contextual keywords (2026-10-05)

## Question

In C# 14 `field` inside a property accessor is the backing field: BRO1303 renaming `_field` to `field` made
`get => field;` read other storage (CS9258), BRO1301 renaming a local `Field` in a getter didn't compile (CS9273).
Which new names should the rename rules refuse, and should they write `@field` instead?

## Choices

1. **Never rename to `field`**, in any rule and any language version (StyleBro's Roslyn 4.8 can't tell C# 14), and
   never to `value` for a local or parameter inside a property, indexer or event (the implicit setter parameter,
   CS0136, an older bug found on the way). The name is simply not reported.
2. Rename and write `@field`/`this.field` where an accessor uses it: correct, but a name that reads as the keyword.
3. Refuse only when the member has a property accessor in scope: more precise, more code, same result in practice.

## Answer

**Choice 1** (the agent's call while fixing the bug; design rule 3, skip rather than risk). `extension`, `scoped` and
`partial` need no guard: only the camelCase rules can produce a lower-case name, and those names are only keywords
where a type or modifier stands.
