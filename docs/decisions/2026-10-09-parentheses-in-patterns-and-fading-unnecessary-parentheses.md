# Parentheses in patterns, and fading unnecessary parentheses (2026-10-09)

## Question

StyleCop's SA1119 (BRO1405) only looks at parenthesized expressions, so `x is (> 0)` or `o is (string s)` were never
reported. And StyleCop also reports a hidden SA1119_p on the `(` and `)` of each finding so the IDE fades them, which
StyleBro didn't. Should StyleBro check patterns, and should it fade the parentheses?

```csharp
// Before
public bool IsSmall(int x) => x is ((1 or 2)) or (> 5 and < 9);

// After
public bool IsSmall(int x) => x is 1 or 2 or (> 5 and < 9);
