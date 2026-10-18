# Baseline: fail only on new violations

Turning StyleBro on in a large codebase usually reports hundreds of existing violations. `dotnet format` can fix them
all at once, but a team may not want that one big change (blame history, open pull requests, review time). A baseline
records today's violations so the build, the IDE and `dotnet format` ignore them, and only new code has to follow the
rules. Existing code gets fixed when someone touches it, or whenever the team chooses to regenerate the baseline.

## Creating one

```bash
dotnet tool install --global StyleBro.Tool --prerelease
bro baseline
```

Run it in the folder with your solution (or name it, like `format`: `bro baseline src/MySolution.sln`, or
`--project`). It runs `dotnet format whitespace`, `style` and `analyzers --verify-no-changes`, each on its own (in one
run, the analyzers' lines would be counted after the whitespace pass split a line, and the baseline would point at the
wrong lines), to find every violation `dotnet format` would fix today and
writes them to the baseline the build reads: the file `<StyleBroBaseline>` names (see below), else the nearest
`stylebro.baseline` in that folder or above, else a new one in that folder (the current folder when you name a solution
or project below it). It says where it wrote; with `<StyleBroBaseline>none</StyleBroBaseline>` it writes nothing.
It asks each project which file its build reads (a `<StyleBroBaseline>` in a nested `src/Directory.Build.props` counts),
so projects that read different files each get the findings in the files they compile, and it says so. Findings in files
outside that folder (`src/` files the projects of `tests/Tests.slnx` compile) aren't added, and the entries an existing
baseline has for them stay. Commit the file.

```text
Wrote stylebro.baseline: 820 violations on 759 lines.
  BRO1601  154
  BRO1603  139
  ...
```

The StyleBro.Analyzers package picks up the nearest `stylebro.baseline` above each project, like `.editorconfig`.
Another location: `<StyleBroBaseline>$(MSBuildThisFileDirectory)eng/stylebro.baseline</StyleBroBaseline>` in a
`Directory.Build.props` (the file must be called `stylebro.baseline`); `<StyleBroBaseline>none</StyleBroBaseline>` turns
it off. Start the path with `$(MSBuildThisFileDirectory)` so it's relative to the props file: MSBuild reads a plain
relative path (`eng/stylebro.baseline`) from each project's folder. The package then looks for it in the project's
folder and the folders above (the first that has it), like `bro format` does from the props file's folder,
which agrees unless a folder in between has its own. A baseline outside
the code (`eng/stylebro.baseline` next to `src/`) covers files in other folders through `../` paths
(`../src/App/Program.cs`); `bro format` and `bro baseline` write them that way.

## What it covers

Every StyleBro rule, and the SDK rules `bro init` or `bro migrate --write` turns on (IDE0003, IDE0009, IDE0011, IDE0036,
IDE0040, IDE0047, IDE0048, IDE0049, IDE0055, IDE0065, IDE0073, IDE2000, IDE2002, IDE2003). A baselined violation isn't
reported by the build or the IDE, and `dotnet format` leaves it alone.

**Not covered: whitespace formatting.** `dotnet format` (and `dotnet format whitespace`) formats whitespace directly,
without diagnostics, so nothing can hide it. Either run `dotnet format whitespace` once (whitespace-only changes are
easy to review), or check only `dotnet format style` and `dotnet format analyzers` in CI. `bro baseline`
says how many such formatting changes it found (whitespace, line endings, charset, final newline, using order:
`dotnet format` reports them as WHITESPACE, ENDOFLINE, CHARSET, FINALNEWLINE and IMPORTS). Files under `obj/` and `bin/`
(generated AssemblyInfo and global usings) don't count. A formatting change also brings back the baselined findings on
the lines it changes (an entry matches a line's text): write the baseline after `bro format`'s whitespace
changes, not before.

**Findings kept on purpose are covered too.** A rename StyleBro's fix leaves because it would break code only the fix
can see (the name read by reflection, used in generated code: see
[Findings kept on purpose](getting-started.md#findings-kept-on-purpose)) stays a warning after every `dotnet format`
run. The baseline records it like any other violation, so warnings as errors don't fail the build over it.
`bro format` lists these with their reasons; check them before you rename one by hand.

`bro format` also adds them to the baseline itself: a run that ends clean with findings kept writes them
into the baseline the build reads (the file `<StyleBroBaseline>` names, else the nearest `stylebro.baseline` above, else
a new one in the folder it runs on) and says so (`Baseline: stylebro.baseline: added 3 kept findings, ...`). Entries
already there stay, except those whose line no longer exists in its file. The build and `--verify-no-changes` then agree:
both skip those findings and fail on new ones. `<StyleBroBaseline>none</StyleBroBaseline>` turns this off too: then
`format` and `--verify-no-changes` say that the build still reports the kept findings (an error with
`TreatWarningsAsErrors`).

## How entries match

Each line of the file is a rule, a file (relative to the baseline), a fingerprint of the reported line's text, and how
many violations of that rule the line has:

```text
BRO1106	src/Orders/Invoice.cs	5d1e0c6b7f2a9e31	1
```

The fingerprint ignores where the line is and how it's indented, so edits elsewhere in the file and moved code keep
the entry. **Changing the line itself makes its violations new**: whoever edits a line fixes it (`dotnet format` does
that). Renaming or moving a file makes all of its entries stale, so its violations are reported again.

## Keeping it small

Regenerate the baseline whenever you like; it only ever lists what's still there. Entries for fixed code are harmless:
they only match a line with exactly that text, up to their count, so a stale baseline is just longer than it needs to be.
To pay off part of the debt, fix a folder (`dotnet format --include src/Orders`) and regenerate.
