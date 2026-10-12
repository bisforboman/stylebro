# Line endings that git's checkout contradicts (2026-10-12)

## Question

kubernetes-client says `end_of_line = lf` in `[*]`, while `.gitattributes` has `*.cs text=auto`: git gives each machine its
own ending, so a Windows checkout has CRLF. The migration raised IDE0055 to error (from SA1500), and a fresh Windows
checkout failed the build with 266 IDE0055 errors; `format` rewrote 205 files to LF, and 4 tests that compare against
`"\r\n"` failed. What should the migration (and `init`) do when the repository's `end_of_line` and git's conversion disagree?

## Choices

1. **Keep endings out of it, warn**: leave the endings as checked out, keep IDE0055 from failing the build on them, and say
   in the report how the team can settle it.
2. Follow `end_of_line`: let `format` rewrite the files (what the trial did).
3. Change `.gitattributes` to match `end_of_line`.

## Decision

**Choice 1** (owner: "Keep endings out of it, warn"). When the repository's own `end_of_line` (lf or crlf, in a section
for C# files) meets C# files stored as LF that git converts on checkout (a `text` attribute without `eol`, or
`core.autocrlf` here), the generated settings write `end_of_line = unset` for C# files and IDE0055 as `suggestion`
(a stronger severity from StyleCop is lowered; a team's own IDE0055 severity stays). Probed: with `unset`,
`dotnet format whitespace` reports no ENDOFLINE on CRLF files, and the build's IDE0055 (34 at warning with
`end_of_line = lf` in KubernetesClient) reports none; `format` still fixes other whitespace. The report names the
contradiction and both ways out: `*.cs text eol=lf` in `.gitattributes` (then `git add --renormalize .`), or removing
`end_of_line`. `init` does the same for the IDE0055 it writes (not with CSharpier, where IDE0055 is off).
