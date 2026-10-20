# Renames keep names quoted by snapshot files and Shouldly messages (2026-10-20)

## Question

Trial 37, shouldly (random pick): after format, 37 of 1,122 tests failed. Shouldly's failure messages quote the call site's
source text, and its own tests compare those messages: 32 with `*.approved.txt` files (`_target` -> `target` by BRO1303,
`ThisString` -> `thisString` by BRO1301, `doSomething` -> `DoSomething` by BRO1309), 5 with expected strings in the test
file ("`methodCall()` should not throw ..."). The snapshot guard (trial 23) kept a name a snapshot or approval file holds
only for data members. What should the renamers do?

## Choices

1. **Widen both guards** (owner's pick): any rename (fields, locals, parameters, methods, local functions) keeps its old
   name when it appears as a word in a `*.approved.*`/`*.verified.*`/`*.received.*` file, not only a data member's; and, in
   a project referencing Shouldly, when it appears as a word in a string literal of the same file.
2. Snapshot guard only (the 5 inline expected strings would still fail).
3. In projects referencing Shouldly, no renames of locals, parameters, private fields and methods at all.
4. Leave it: Shouldly's own suite asserts on its message format, which its users rarely do.

## Decision

Widen both guards. `CamelCaseRenamer.FindQuoted`: the snapshot check now covers every rename (the data-member-only check
in `FindInCode` was removed as redundant); with Shouldly (its assembly referenced, or a project named Shouldly) a string of
the declaring file holding the old name as a word keeps it, kept reason `NameInShouldlyMessage`.
