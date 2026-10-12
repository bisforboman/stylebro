# BRO1504 inside lists (2026-10-12)

## Question

Chickensoft's GameDemo has a `//` comment between two arguments of a split call (`Path.Join(...),`, the comment, then
`new JsonSerializerOptions()`). BRO1504 (SA1515) inserted a blank line above the comment, inside the argument list.
Should a comment between the items of a list need a blank line above it?

## Choices

1. **Don't require it inside lists**, like the exemption for comments between call chain links (2026-10-11).
2. Keep StyleCop's SA1515 behavior (a blank line above every such comment).

## Decision

**Choice 1** (owner: "Don't require it inside lists"). A `//` comment right after a list's `(`, `[`, `{` or a `,`, or
right before a `,` or the list's closing token, needs no blank line above it. Lists: argument, bracketed argument
(indexers), attribute argument, parameter, bracketed parameter, initializer (object, collection, array), collection
expression and tuple (expression and type) lists. Not type argument/parameter lists (comments there are rare) or switch
expression arms. A deviation from SA1515 that only makes BRO1504 quieter, so a StyleCop-clean repository stays clean.
The regions fix (BRO1112/BRO1113) asks the same check.
