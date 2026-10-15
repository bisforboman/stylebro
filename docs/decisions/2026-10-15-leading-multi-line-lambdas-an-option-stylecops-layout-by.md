# Leading multi-line lambdas: an option, StyleCop's layout by default (2026-10-15)

## Question

CommunityToolkit has about 50 calls whose first argument is a multi-line lambda and whose other arguments sit on the
lambda's closing line:

```csharp
context.RegisterSymbolAction(ctx =>
{
    ...
}, SymbolKind.Field);
```

StyleCop's SA1116/SA1117 report this, and so do BRO1107/BRO1108, whose fix puts every argument on its own line (trial
22). Should StyleBro allow the idiom?

## Choices

1. **Keep StyleCop's behaviour by default and add an option** (`stylebro_allow_leading_multiline_lambda = true`) that
   allows a first argument that is a multi-line lambda, anonymous method or anonymous object whose closing line holds the
   remaining arguments. `stylebro-migrate init` counts the shape and writes the option when the repository uses it (at
   least 3 places, all of them below 10, 75 % from 10 on; mixed: both allowed); the migration from StyleCop keeps
   StyleCop's behaviour.
2. Allow it always (a deviation from StyleCop).
3. Keep reporting it, no option.

## Decision

**Choice 1** (owner's choice 2026-10-15). BRO1107 and BRO1108 skip such a call when the option is on; any other layout of
the same call is still reported. A FixOrderTests case checks it next to BRO1110's `own_line`.
