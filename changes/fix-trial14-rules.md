### Fixed

- **BRO1151** no longer unwraps a one-element array whose element the call would take as the array itself:
  `Take(new Item[] { default! })` became `Take(default!)`, which compiles but passes a null array. The compiler now
  decides (the element must bind as a `params` element), so no wrapping (`!`, parentheses, casts) can slip through.
- **BRO1311** no longer keeps a tuple element's rename because code a `stylebro-migrate format` run doesn't load has a
  variable or lambda parameter of that name: only uses as a member (`.value`, `value:`, another tuple's element)
  count, and for an internal member only in projects `InternalsVisibleTo` names.
- **BRO1149** with `stylebro_layout_by = csharpier` writes CSharpier's layout when the merged inner condition is an
  `||` chain that doesn't fit its line: `&& (`, each `||` operand on its own line, `)`.
