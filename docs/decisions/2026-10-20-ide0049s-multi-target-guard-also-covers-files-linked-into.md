# IDE0049's multi-target guard also covers files linked into projects with an older C# (2026-10-20)

## Question

Trial 35, sqlite-net: the first IDE0049 guard (hide the fix where a framework of the project defaults to a C# too old
for it) didn't catch the trial's own case. `src/SQLite.cs` is linked into SQLite.Tests (net8.0 only, C# 12) and into
SQLite-net-static (netstandard2.0, C# 7.3). The `using X = nint;` fix came through SQLite.Tests, whose suppressor sees one
framework, and the netstandard2.0 build still failed (CS8370). How should StyleBro close this?

## Choices

1. **format tells the guard** (owner's pick): `bro format` already evaluates every project and framework for its
   shared-file runs; it writes the files some compilation builds with C# below 12 (and another with a newer one) into a
   list its MSBuild hook adds as an AdditionalFile (`*.oldcsharp`), and the suppressor hides those IDE0049s in every
   project that compiles the file. Plain `dotnet format` and the build keep the project-only guard.
2. init writes IDE0049 as suggestion in such repositories (they lose every `Int32` -> `int` fix).
3. Keep the project-only guard and document that linked files need LangVersion set.

## Decision

format tells the guard.
