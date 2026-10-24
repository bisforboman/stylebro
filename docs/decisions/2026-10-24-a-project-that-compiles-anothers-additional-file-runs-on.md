# A project that compiles another's additional file runs on its own (2026-10-24)

## Question

Trial 50, ncalc: Parlot.SourceGenerator makes `*.parlot.cs` an AdditionalFiles item in NCalc.Parser, and NCalc.Benchmarks
compiles the same file. The SDK's MSBuildWorkspace then crashes loading any solution or solution filter with both
("The solution does not contain the specified document"); opening the project file works. The Benchmarks run crashed in
every `bro format`, so format never finished. How should the default engine handle it?

## Choices

1. **Isolate and run the project** (owner's pick): evaluate the projects' AdditionalFiles too, give a project that compiles
   another's additional file a run of its own, and hand `dotnet format` that project's file instead of a one-project
   solution filter.
2. Skip it with a `Not checked:` line naming the project and the SDK bug.

## Decision

Isolate and run the project.
