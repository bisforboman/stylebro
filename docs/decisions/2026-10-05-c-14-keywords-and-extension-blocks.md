# C# 14: keywords and extension blocks (2026-10-05)

## Question

A survey of StyleBro on C# 14 code found (1) identifiers C# 14 reads as keywords (`field` in a property accessor,
`extension`, `partial`) silently change meaning or break the build, and nothing fixes them; (2) BRO1001 skipped every
type containing an extension block (`extension(string s) { ... }`, a member kind Roslyn 4.8 doesn't know), so C# 14
`*Extensions` classes got no ordering at all; (3) BRO1509 didn't expand a one-line extension block. Add a rule for (1),
and where do extension blocks sort in (2)?

## Choices

1. Escaping rule: on in the preset (off after `stylebro-migrate`, as for every rule StyleCop doesn't have), or off.
2. Extension blocks in BRO1001: with the methods, first (right before them); last (after every other kind); keep skipping
   such types.

## Decision

The owner: BRO1144, on in the preset and off after migrate, kept narrow to the cases where C# 14 changes the meaning or
breaks the build (Roslyn's breaking-changes list, each checked with the C# 14 compiler). Extension blocks are their own
kind `extension`, right before methods ("with methods, first"), and their members are sorted by the normal rules; BRO1509
expands a one-line block like a type. Agent's choices: `@` rather than renaming (it means the same in every C# version,
so multi-targeted projects stay consistent); in C# 14 code a `field` keyword is escaped only when a member, local or
parameter named `field` of the property's type is in scope (otherwise `@field` wouldn't compile, and the keyword is
clearly meant); `scoped` as a lambda parameter's type (Sonar S8381) is left out; extension blocks are recognized by
their runtime type, since StyleBro compiles against Roslyn 4.8.
