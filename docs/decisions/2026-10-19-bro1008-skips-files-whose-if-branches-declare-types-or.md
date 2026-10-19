# BRO1008 skips files whose #if branches declare types or namespaces (2026-10-19)

## Question

Trial 34, protobuf-net: `protobuf-net.Core/Internal/DynamicallyAccessedMembersAttribute.cs` has
`using System.Diagnostics.CodeAnalysis;` inside its namespace and, further down, declares that namespace as a polyfill
under `#if !PLAT_DYNAMIC_ACCESS_ATTR`. BRO1008 reported the using in Core's net8.0 build, where it can move. Its fix
declined, because protobuf-net.BuildTools compiles the same file without the symbol and the copies disagree. The finding
stayed for good and the TreatWarningsAsErrors build reported it. (`format` said Clean above it; it now says "Not clean: 1
finding no fix changes", like `check`.) What should BRO1008 do?

## Choices

1. **Skip such files** (owner's pick): the analyzer doesn't report a file whose `#if` branches declare namespaces or
   types, because another build of the file may bind the usings differently. No diagnostic rather than one no fix can
   change (design rule 3).
2. Keep reporting: `format` and `check` both say Not clean and list it; the user suppresses or baselines it.

## Decision

Skip such files.
