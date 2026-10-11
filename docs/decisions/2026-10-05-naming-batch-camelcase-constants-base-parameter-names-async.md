# Naming batch: camelCase constants, base parameter names, Async suffix (2026-10-05)

## Question

Three naming candidates from [beyond-stylecop.md](../beyond-stylecop.md): camelCase private constants and static readonly
fields (StyleCop #2641, #3793; tracker #10), a parameter named like the one it overrides or implements (StyleCop's
proposed SA1315, #1949, the SDK's CA1725 without a fix; tracker #12), and the `Async` suffix on asynchronous methods
(Roslynator RCS1046, Meziantou MA0137; survey #18). All three are renames, so the rename guards apply, and two of them
change names other code depends on (named arguments, public method names).

## Choices

1. camelCase constants: a new rule; an option for BRO1306 (a `stylebro_*` key, or following the SDK's
   `dotnet_naming_rule.*` like BRO1303 does); skip.
2. Base parameter names: on by default; off by default; skip (CA1725 reports it).
3. `Async` suffix: on by default; off by default; skip (renames public API, frameworks bind some names).

## Decision

The owner: BRO1306 follows the SDK's naming rules, no new id: a naming rule that singles out private constants
(`required_modifiers` with `const`) or private static readonly fields (`static`/`readonly`) and asks for camel case
(no prefix or `_`) makes BRO1306 rename them that way; without one, PascalCase like StyleCop. BRO1313 (base parameter
names) and BRO1314 (`Async` suffix) are built and off by default (`stylebro-migrate` turns them off too; StyleCop never
shipped either). BRO1313 matches bases in libraries too and skips discards, disagreeing bases, partial methods and
clashing names; BRO1314 counts `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>` and `IAsyncEnumerable<T>` return types
and skips `Main`, names containing `Async`, attributed methods (tests and frameworks), controllers, hubs and event
handlers. The reverse (no `Async` on synchronous methods) is left out.
