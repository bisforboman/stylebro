# BRO1601 without documentation generation (2026-10-06)

## Question

A first run on new repositories (CleanArchitecture: 49 files) added `/// <inheritdoc/>` to projects that don't generate
documentation at all, where nothing reads it. StyleCop's documentation rules stay quiet there (SA0001 says the
documentation isn't parsed). Report BRO1601 in such projects?

```csharp
// Before, in a project without <GenerateDocumentationFile>true</GenerateDocumentationFile>
public override string ToString() => Name;

// After (until now); with choice 1 the code stays as it was
/// <inheritdoc/>
public override string ToString() => Name;
```

## Choices

1. **Skip without doc generation**: no BRO1601 unless the project generates documentation, like StyleCop.
2. Report everywhere (the rule as built; it's text-based and works without parsed documentation).

## Decision

The owner: **skip without doc generation** (choice 1). The signal has to be the same in a build and under
`dotnet format`, which parses documentation comments even where the build doesn't: the package's build targets make
`GenerateDocumentationFile` compiler-visible (the SDK always sets it, `true` also when only `DocumentationFile` is set).
Without the package's targets (the analyzer referenced directly), the compiler's documentation mode decides.
