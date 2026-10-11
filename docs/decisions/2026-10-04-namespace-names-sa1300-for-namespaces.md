# Namespace names (SA1300 for namespaces) (2026-10-04)

## Question

BRO1309 skipped namespaces, so SA1300's check on namespace names had no StyleBro rule. A namespace rename changes the
full name of every type in it and can break things a code rename can't see (details and experiments in
[proposals/namespace-names.md](../proposals/namespace-names.md)): plain embedded resources follow `RootNamespace`, not the
code; strings and stored data with type names (`Type.GetType`, `$type`); Razor and XAML references; config files. In
22 public repositories every lower-case namespace part was deliberate (brand names like `iText`, `iOS`; culture
codes). Open questions: ship a rule at all, as a separate id and on or off by default; should `stylebro-migrate` turn
it on with SA1300; report the root namespace (which the fix can't rename) or not; rename public namespaces in
libraries; and what to do about resources in folders, config files and XAML.

## Choices

1. **Ship opt-in:** a separate rule (BRO1312), off in the descriptor and the preset, with the prototype's guards;
   `stylebro-migrate` turns it on when SA1300 is on; the root namespace isn't reported; public namespaces are renamed
   like BRO1309 renames public types; the resource, config and XAML limits are documented.
2. **Ship with the root namespace reported but not fixed** (a diagnostic `dotnet format` can't fix).
3. **Don't rename public namespaces** (skip namespaces with a public type, or packable projects).
4. **Keep the skip** and record it with the findings.

## Answer

Ship opt-in (user's decision, 2026-10-04). Choice 1: BRO1312 off by default, on through `stylebro-migrate` with
SA1300; root namespace unreported; public namespaces renamed; resources in folders, config files and XAML documented as
limits (no package change to expose embedded resources).
