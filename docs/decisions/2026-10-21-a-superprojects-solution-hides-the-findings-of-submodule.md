# A superproject's solution hides the findings of submodule projects it builds (2026-10-21)

## Question

Trial 39, MailKit: `MailKit.sln` lists `submodules/MimeKit/MimeKit/MimeKit.csproj`, a project inside a git submodule.
`bro init` wrote a root Directory.Build.props, so MimeKit's project got StyleBro too, and `bro check --build` found 145,994
findings, all in the submodule. `format` leaves submodules alone (another repository's code) and format and check called
the rest clean, on the assumption that the build hides submodule code too. It didn't: trial 28 decided that a project
inside a submodule reports its own findings (a submodule's own checkout, worked on there), so only files a superproject
project compiles from a submodule were hidden. What should happen?

## Choices

1. **Hide via the solution** (owner's pick): the package hides a submodule project's findings when the build's solution
   (`SolutionDir`, which the IDE and `dotnet format` set too) is the superproject's, outside the submodule. Building the
   submodule's own solution or project still reports them (trial 28 unchanged).
2. Keep the build as is; format and check name the submodule projects the solution builds as a gap ("Not verified").
3. `init` writes a Condition so the StyleBro settings apply only to projects outside submodules (the user's
   PackageReference would need the same Condition).

## Decision

Hide via the solution. `_StyleBroFindSubmodules` keeps a submodule at or above the project's repository root in
`StyleBroSubmoduleFolders` when `SolutionDir` is set and lies outside that submodule; without a solution, or with one inside
the submodule, it is dropped as before.
