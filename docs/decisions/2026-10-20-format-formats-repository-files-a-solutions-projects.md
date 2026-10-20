# format formats repository files a solution's projects compile from above its folder (2026-10-20)

## Question

Trial 36, NodaTime: `src/NodaTime.Tools.Common` compiles `..\..\lib\commandline\*.cs`, inside the git repository but above
`src/`, the folder of `NodaTime.slnx` that format works from. `dotnet format --include` can't name a path above its folder,
so format dropped the file silently: it was never formatted, plain check said only IDE0049 wasn't checked ("the rest is
clean"), and the TreatWarningsAsErrors build failed on 134 StyleBro findings there (`check --build` caught it, exit 2). What
should format do with such files?

## Choices

1. **Format them** (owner's pick): format works from the folder holding the solution's projects and the repository files
   they compile (here the repository root), so `--include` names `lib/commandline/` and the file is fixed like the rest.
   Files outside the repository stay left alone, as before.
2. A gap only: leave them unformatted, name them in a "Not checked:" line (Not verified, exit code 3).
3. Both: format them, and name as a gap any that still can't be reached.

## Decision

Format them.
