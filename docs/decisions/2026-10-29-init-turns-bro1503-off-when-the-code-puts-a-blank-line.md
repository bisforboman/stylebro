# init turns BRO1503 off when the code puts a blank line below a type's brace (2026-10-29)

## Question

Trial 61, jbevain/cecil: 543 of 720 namespace and type `{` are followed by a blank line (Mono style); in method bodies
only 5 of 5,334. init had no convention for it, so BRO1503 (SA1505) removed ~540 blank lines in ~200 files. What should
init do?

## Choices

- **Detect it, turn BRO1503 off** (recommended): init counts the blank lines below the `{` of namespaces and types; when
  that's the code's convention, BRO1503 goes off, as for BRO1511 and BRO1506.
- Keep as is (SA1505 parity): BRO1503 stays on; removing them is what StyleCop and the preset want.
- A new key for types and namespaces only: BRO1503 allows a blank line there and still removes them in blocks; init sets
  it when the code does that.

## Decision

Detect it, turn BRO1503 off (owner, 2026-10-29). Like init's other blank-line conventions, mixed code (neither form at
75 %, or at all places below 10) turns the rule off too; mostly without keeps it on. Counted: the `{` of block namespaces
and types (classes, structs, interfaces, records, enums) with code below it on a later line; not empty or one-line
bodies, a brace with a comment after it, or blocks inside members.
