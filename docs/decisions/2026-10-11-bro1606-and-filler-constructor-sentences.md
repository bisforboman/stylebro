# BRO1606 and filler constructor sentences (2026-10-11)

## Question

BRO1606 puts the standard sentence in front of a constructor summary and keeps the old text after it. On Stateless,
`Default constructor.` became `Initializes a new instance of the <see cref="Alarm"/> class. Default constructor.`: the
kept sentence says nothing the standard one doesn't. What should the fix do with such filler?

## Choices

1. **Replace known filler, keep real text**: a small, deliberate list of sentences that only say "this is a
   constructor" is replaced by the standard sentence; anything else stays after it.
2. Keep everything (StyleCop's behavior).
3. Replace any first sentence that mentions a constructor or an instance.

## Decision

**Choice 1** (owner: "Replace known filler, keep real text"). The list (`ConstructorSummaries.Filler`): `Default
constructor`, `Constructor`, `Ctor`, `Constructor for X`, `Creates a new instance`, `Creates a new instance of X`,
`Creates an instance of the X class`, also with `Constructs` or `Instantiates` and `object` for `instance`; X is one
name or a `<see cref/>`. Only the first sentence is replaced; text after it is kept (`Constructor for Alarm. Uses the
given delay.` keeps `Uses the given delay.`).
