# Using placement: a StyleBro rule instead of IDE0065 (2026-10-09)

## Question

In the 0.3.0-alpha.1 trial, the SDK's IDE0065 (which `stylebro-migrate init` turned on and migrate mapped SA1200 to)
broke builds: moving usings out of a namespace changes how names are looked up. In eShop
`CreateOrderDraftCommandHandler.cs` got CS0104 ('Order' is an ambiguous reference), in Ocelot
`DownstreamRouteProviderFactoryTests.cs` got 7x CS0118 ('DownstreamRouteFinder' is a namespace but is used like a
type; reported upstream as [dotnet/roslyn#86002](https://github.com/dotnet/roslyn/issues/86002)). It also left a blank first line in files and needed second runs (usings sorted, BRO1505's blank line). What to
do about using placement?

## Choices

1. Keep IDE0065 and document the risk.
2. Turn IDE0065 off and leave using placement unchecked.
3. A StyleBro rule that moves the usings only when every name in the file binds the same afterwards, and writes the
   layout the other rules want.

## Decision

3 (owner: "StyleBro rule that checks binding"): [BRO1008](../rules/BRO1008.md) replaces IDE0065 and SA1200. It follows
`csharp_using_directive_placement` (not set: StyleCop's default, inside; changed to nothing reported on 2026-10-10, see above). The check follows C#'s lookup order rather
than compiling the moved file again (a first version did, and cost 180 ms on the Newtonsoft.Json benchmark; this one
9 ms): every identifier the moved usings import must find the same symbols in the enclosing namespaces, or nothing
there and nothing else through the global usings; extension methods they bring must not share a name with others in
scope; a using's own name must start with a global namespace member no enclosing namespace has. A file that fails
isn't reported. `init` writes IDE0065 = none; migrate maps SA1200 to BRO1008 (off with stylecop.json's `preserve`).
