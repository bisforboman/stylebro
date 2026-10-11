# BRO1134: a comment ending the header's line (2026-10-06)

## Question

A first run on new repositories found BRO1134 moving notes that belong to the declaration's name or signature:

```csharp
// Before
public int GetHashCollissions() // legacy incorrect spelling, oops
{
    return collissions;
}

// After (until now)
public int GetHashCollissions()
{
    // legacy incorrect spelling, oops
    return collissions;
}
```

Keep moving such comments into the body?

## Choices

1. **Skip a trailing comment**: a comment that ends the header's last line, with `{` on the next line, stays where it
   is. A comment on a line of its own between the header and `{` still moves, and so does one before a `{` on the
   header's line (`void M() /* note */ {`).
2. Keep moving every comment (the rule as built, like SA1108 for statements).

## Decision

The owner: **skip a trailing comment** (choice 1). BRO1132 (statements, StyleCop's SA1108) is unchanged.
