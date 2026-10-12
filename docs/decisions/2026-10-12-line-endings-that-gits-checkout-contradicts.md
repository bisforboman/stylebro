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

### Extended: an `end_of_line` the committed files contradict (2026-10-12, trial 10)

CleanArchitecture says `end_of_line = crlf`, but 349 of 350 C# files are committed (and checked out) as LF: no
`.gitattributes`, `core.autocrlf = false`. `init` reported the key as set, and `format` turned 86 files to CRLF, 44 of them
with no other change. The same choice applies: endings stay as they are. But `unset` isn't how here: probed with
`dotnet format whitespace` on Windows, `end_of_line = unset` rewrote an untouched LF file to CRLF (the formatter then uses
the OS's ending for the whole file), while without the key it kept the file's LF. With git converting on checkout, the OS's
ending is the checkout's, so `unset` is right there; here it isn't. So when the C# files git leaves alone clearly (the usual
majority) have the other ending, nothing enforced the key, and the files decide, like a convention key at severity
`silent`: the block writes `end_of_line = <the files' ending>` for C# files, IDE0055 keeps its severity (the key now
matches the files), and the report says how to get the key's ending instead (remove the line and let `format` rewrite the
files, or `*.cs text eol=<key>` in `.gitattributes` and `git add --renormalize .`). The migration does the same. Measured on
CleanArchitecture: the root's `format` run changed 40 files, none of them only in their endings, and none became CRLF.

### Extended: a `charset` the files don't follow (2026-10-12, trial 11)

Ical.Net says `charset = utf-8` in `[*]`, but 108 of its 223 C# files start with a BOM; `format` stripped all 108 (also
from files it changed nothing else in, probed). Fitnet says `charset = utf-8-bom` with 15 of 127 files having one, and
`format` added 112. The same choice applies: nothing enforced the key, so the files decide. When the key's form doesn't
have the conventions' share (75 % of at least 10, all of 3 to 9), `init` writes the files' charset for C# files, or
`charset = unset` when they're mixed: probed, `dotnet format` then leaves each file's BOM (or its absence) alone. The
report says how to get the key's charset instead (remove the line and run `stylebro-migrate format`).

### Extended: an `insert_final_newline` the files don't follow (2026-10-12, trial 12)

The starter kit's `src/.editorconfig` says `insert_final_newline = false`, but 1,092 of its 1,597 C# files (git's
conversion aside) end with a line break; nothing enforced it until `init` turned the whitespace pass on, and `format`
removed the line break from 1,092 files. The same choice applies, judged like `charset`: the files' value is written for
C# files, or `insert_final_newline = unset` when they're mixed (probed: `dotnet format whitespace` then leaves files with
and without a final line break alone), with a report note. The migration does the same (and for `charset`, which it
didn't before). `trim_trailing_whitespace` isn't read by `dotnet format` at all (probed: `false` still trimmed every
formatted line), so it can't be kept that way; the report says so when the key is `false` and files have trailing
whitespace.
