# Microsoft style: the source is dotnet/runtime's coding style (2026-10-16)

## Question

StyleBro's preset follows StyleCop's defaults. Many teams instead write C# "the way Microsoft does": `_camelCase` and
`s_` fields, no `this.`, braces optional on one-line bodies. Which text defines a "Microsoft" style for StyleBro?

## Choices

1. **dotnet/runtime's coding style** (owner's pick): `docs/coding-guidelines/coding-style.md` and the repository's root
   `.editorconfig` in github.com/dotnet/runtime, read at their current `main`. Concrete numbered rules, an
   `.editorconfig` that already encodes most of them, and the code base the rest of .NET copies.
2. The "C# coding conventions" pages on Microsoft Learn: written for samples and docs, prose only (no `.editorconfig`
   to check a mapping against).
3. The .NET Framework Design Guidelines: public API naming only, nothing about layout or private fields.

## Decision

**Choice 1.** Both files are read at their current `main` (commit `24543ddd`, 2026-10-16) and mapped rule by rule in
[the proposal](../proposals/microsoft-style.md). A Microsoft Learn variant is an open question there.
