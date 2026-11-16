# StyleBro's own build and tests run on SDK 10; fixtures and real world on the newest (2026-11-15)

## Question

The workflows install the SDK with `actions/setup-dotnet` (`dotnet-version: 10.0.x`), and the repository had no
global.json. The runner image `ubuntu-26.04` of 2026-11-11 brings the .NET 11 SDK (11.0.100, released 2026-11-10), and
`dotnet` picks the newest SDK installed. From 2026-11-13 three nightly runs failed on an unchanged main:

- **Fixtures** (#353): `dotnet sln add` of SDK 11 fails for a project another listed project references ("Solution
  folder 'src' already contains a project with the filename 'Lib.csproj'"), so nearly every shape failed while it was
  created.
- **Real world** (#354): Jellyfin's untouched code has 7 compile errors on SDK 11 (CA1860, an error in its rule set).
  A fix that merges two `if`s moved three of them to another line or column, and the run compared errors with their
  positions: "3 new compile errors".
- **Mutation** (#342): three guards in `FormatCommand.cs` work around what SDK 10's MSBuild and `dotnet format` do
  (a query that fails on an item condition, a solution filter that misses referenced projects). On SDK 11 the tests
  pass without the guards, so the mutations survived.

Which SDK should CI use?

## Choices

Asked of the owner as a multiple-choice question (2026-11-15):

1. **SDK 10 for StyleBro, the newest outside (Recommended)**: a global.json pins StyleBro's own build, tests and
   mutation run to SDK 10 (the LTS), as before 2026-11-11. Fixtures and real-world repositories live outside the
   repository and keep running on the newest SDK, which is what new users have.
2. **SDK 10 everywhere**: also pin the fixture and real-world work folders; SDK 11 becomes later work.
3. **SDK 11 everywhere**: no pin; rework the three guards' tests or drop their mutation entries.

## Decision

The owner chose **SDK 10 for StyleBro, the newest outside**:

- `global.json` at the root: `10.0.100` with `rollForward: latestFeature` (any 10.0.x SDK). Building StyleBro needs an
  SDK 10.
- The tests write small repositories to the temporary folder and run `dotnet` there, outside the global.json's reach.
  `tests/StyleBro.Tests/TestSdk.cs` (a module initializer) points the test process's temporary folder at
  `<temp>/stylebro-tests`, which holds a copy of global.json, so those commands run on SDK 10 too.
- Fixtures and real world run on whatever `dotnet` picks outside the repository (11.0.100 today). Both print it
  (`== SDK for the shapes`, `- SDK:` in the report), and the real-world caches are keyed by it instead of by the
  version setup-dotnet installed.
- The fixtures write their solution files themselves (`New-Solution`), in the layout SDK 10's `dotnet sln add` gave,
  so a shape is the same repository whatever SDK creates it.
- The real-world run compares compile errors per file and text, whatever their position: an error of the untouched
  code that a fix moved is not new.

What this leaves open: StyleBro's unit tests don't cover SDK 11's `dotnet format` and MSBuild; the fixtures and the
real-world repositories do.
