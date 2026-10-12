# BRO1301/BRO1302: names in expression trees are not reported (2026-10-12)

## Question

Trial 11 (Mapster): BRO1301 renamed `from Param_0 in products.AsQueryable()` to `param0`, and a test comparing the
query's `ToString()` failed. Range variables and lambda parameters that end up in a `System.Linq.Expressions` tree
(a query over `IQueryable`, an `Expression<Func<...>>` lambda, a lambda nested in one) are data at run time: the
expression's text, query providers that translate names. They are now never renamed. Should they still be reported
(as findings kept on purpose, like the renames the fix's solution-wide guards hold back), or not reported at all?
StyleCop's SA1312/SA1313 report them.

## Choices

1. Not reported, like the analyzer's other skips for names that reach run time (`nameof`, `[CallerArgumentExpression]`
   for BRO1313, constructor parameters named like a member).
2. Reported and kept: the warning stays, `dotnet format` leaves it, `stylebro-migrate format` lists it with a reason.

## Decision

1 (agent's proposal). The analyzer can tell from the semantic model alone (the anonymous function's conversion), so
there is nothing the fix knows that the analyzer doesn't: kept findings exist for what only the solution-wide fix can
see. A finding nobody can act on safely is noise, and after a migration from StyleCop (SA1312 maps to BRO1301) a
repository that was clean would get warnings it can only suppress. The rule pages and differences-from-stylecop.md
list it under "Not reported".
