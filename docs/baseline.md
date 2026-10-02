# Baseline: fail only on new violations

Turning StyleBro on in a large codebase usually reports hundreds of existing violations. `dotnet format` can fix them
all at once, but a team may not want that one big change (blame history, open pull requests, review time). A baseline
records today's violations so the build, the IDE and `dotnet format` ignore them, and only new code has to follow the
rules. Existing code gets fixed when someone touches it, or whenever the team chooses to regenerate the baseline.

## Creating one

```bash
dotnet tool install --global StyleBro.Migrate --prerelease
stylebro-migrate baseline
```

Run it in the folder with your solution (or name it: `stylebro-migrate baseline --project MySolution.sln`). It runs
`dotnet format --verify-no-changes` to find every violation `dotnet format` would fix today and writes them to
`stylebro.baseline` in that folder. Commit the file.

```text
Wrote stylebro.baseline: 820 violations on 759 lines.
  BRO1601  154
  BRO1603  139
  ...
```

The StyleBro.Analyzers package picks up the nearest `stylebro.baseline` above each project, like `.editorconfig`.
Another location: `<StyleBroBaseline>path/to/stylebro.baseline</StyleBroBaseline>` in a `Directory.Build.props` (the
file must be called `stylebro.baseline`); `<StyleBroBaseline>none</StyleBroBaseline>` turns it off.

## What it covers

Every StyleBro rule, and the SDK rules `stylebro-migrate init` or `stylebro-migrate --write` turns on (IDE0003, IDE0009, IDE0011, IDE0036,
IDE0040, IDE0047, IDE0048, IDE0049, IDE0055, IDE0065, IDE0073, IDE2000, IDE2002, IDE2003). A baselined violation isn't
reported by the build or the IDE, and `dotnet format` leaves it alone.

**Not covered: whitespace formatting.** `dotnet format` (and `dotnet format whitespace`) formats whitespace directly,
without diagnostics, so nothing can hide it. Either run `dotnet format whitespace` once (whitespace-only changes are
easy to review), or check only `dotnet format style` and `dotnet format analyzers` in CI. `stylebro-migrate baseline`
says how many whitespace changes it found.

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
