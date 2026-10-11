# What `stylebro-migrate init` does in an existing codebase (2026-10-09)

## Question

`init` applied the preset as is. On Kavita (309 of 309 private fields named `_x`) BRO1303 renamed 337 fields to
`x`; on Spectre.Console, whose `.editorconfig` turns SA1309, SA1201 and SA1202 off, it applied the full BRO1001 sort
and BRO1306. Should `init` look at the repository first?

```csharp
// Kavita before init
private readonly ILogger<SeriesService> _logger;

// After init until now (BRO1303, camelCase)
private readonly ILogger<SeriesService> logger;

// After: init wrote stylebro_private_field_naming = _camelCase, nothing renamed
private readonly ILogger<SeriesService> _logger;
```

## Choices

1. Detect and adapt: keep a clear `_` majority, and stop in a repository with a StyleCop setup, pointing to
   `stylebro-migrate --write`.
2. Only document both cases.
3. Ask interactively.

## Decision

The owner: "Detect and adapt" (choice 1). The threshold is three quarters of the private fields (instance and
non-readonly static; generated code, EF Core migrations, vendored folders and submodules don't count), and the
repository's own `stylebro_private_field_naming` or `dotnet_naming_rule.*` settings win. StyleCop setup means
`stylecop.json`, StyleCop rule ids in `.editorconfig`/rulesets/global configs, or a StyleCop.Analyzers reference, also
in files the MSBuild files import; `init` prints what it found and exits with 1 without writing. After
`stylebro-migrate --write` (its block present) `init` runs as before, e.g. for `--modernize`.
