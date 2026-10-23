# Migrate reads the global configs packages add (2026-10-23)

## Question

Trial 48, benjamin-hodgson/Pidgin (StyleCop 1.1.118, StyleCop-clean): its StyleCop configuration comes from a NuGet
package. Benjamin.Pizza.BuildScripts adds `Benjamin.Pizza.globalconfig` through `GlobalAnalyzerConfigFiles`, which turns
SA1200, SA1101, SA1201-SA1204, SA1309 and others off. `bro migrate` read only the repository's files, took those rules as
StyleCop's defaults (on), and the first format changed 115 files: 100 files' usings moved inside the namespace, 71 got
`this.`, 36 were sorted. How should migrate treat a global config that a package adds?

## Choices

1. **Read it** (owner's pick): migrate evaluates each project's `GlobalAnalyzerConfigFiles` (MSBuild, after a restore) and
   reads a package's global config like the repository's own: its StyleCop severities and its settings keys. Without the
   restore output it says to run `dotnet restore` first for that project.
2. Read it, restoring if needed: the same, but migrate runs `dotnet restore` itself when the restore output is missing.
3. Warn only: keep reading the repository's files, name the package's global config and say the block may turn on rules
   it turns off.

## Decision

Read it.
