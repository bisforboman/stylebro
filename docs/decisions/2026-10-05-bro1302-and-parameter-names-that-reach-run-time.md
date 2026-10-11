# BRO1302 and parameter names that reach run time (2026-10-05)

## Question

BRO1313 now keeps a parameter whose name reaches run time (`nameof(x)`, or `x` passed to a `[CallerArgumentExpression]`
parameter such as `ThrowIfNull(x)`): its rename changed an exception's `ParamName` and broke a Newtonsoft.Json test.
BRO1302 (parameters start lower-case, on by default) changes `ParamName` the same way. Should it get the same guard?

```csharp
public void Load(string Path)
{
    ArgumentNullException.ThrowIfNull(Path);   // after BRO1302: ThrowIfNull(path), ParamName "path"
}
```

## Choices

1. Skip those parameters, like BRO1313 (they keep their wrong-cased name).
2. Report, but don't fix (like the string guard).
3. Keep renaming, like StyleCop's SA1313; document it.

## Decision

The owner: keep renaming (choice 3). BRO1302's rename fixes the casing the team asked for; the changed `ParamName` is
the new name of the parameter. BRO1313 differs because its rename only aligns a name with the base. Documented on
[BRO1302.md](../rules/BRO1302.md).
