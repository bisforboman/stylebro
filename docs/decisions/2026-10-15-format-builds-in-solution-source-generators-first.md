# format builds in-solution source generators first (2026-10-15)

## Question

Trial 23: with an in-solution source generator (a `ProjectReference` with `OutputItemType="Analyzer"`) not built yet,
`dotnet format` runs without it; the generated code is missing, a rename of a member it uses went through, and the
build failed with CS0103 while verify said clean. What should format do?

## Choices

1. **Build generators first**: format (and verify's copy, the preview, baseline) builds those projects once before its
   runs, says so, and skips that when they're already built.
2. Warn that the generator isn't built and keep its renames back.
3. Leave it to the user (build first).

## Decision

**Choice 1** (owner's choice 2026-10-15). "Built" means the output (TargetPath) exists and is newer than the project
folder's files. The build runs without analyzers and warnings as errors (the generator's own code isn't formatted yet);
a failed build is said and the runs go on without it.
