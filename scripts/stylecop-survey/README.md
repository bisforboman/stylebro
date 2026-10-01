# StyleCop survey

Scripts and data behind [docs/stylecop-mapping.md](../../docs/stylecop-mapping.md): what happens to each StyleCop rule
when a project moves to StyleBro.

| Step | Script | Writes |
|---|---|---|
| 1. List every StyleCop rule, and whether StyleCop has a fix for it | `Get-Inventory.ps1` | `data/inventory-<version>.csv` |
| 2. Survey a repo that uses StyleCop: its settings, and counts with every rule on | `Invoke-RepoSurvey.ps1` | `data/<name>-configs.csv`, `data/<name>-counts.csv` |
| 3. Check which rules `dotnet format` already fixes with SDK settings alone | `Test-SdkCoverage.ps1` | `data/sdk-check.csv` |
| 4. Generate the mapping and the skipped-rules list | `New-Mapping.ps1` | `docs/stylecop-mapping.md`, `docs/skipped-rules.md`, `data/mapping.csv` |
| Check a migration changes (almost) nothing in a StyleCop-clean repo | `Measure-MigrationDelta.ps1` | commits in the target clone |

The measured data is committed, so step 4 works on its own. Hand-written parts live in three files:

- `decisions.psd1`: the proposal per covered rule (SDK / StyleBro), which SDK setting covers a rule, and notes.
- `skipped.psd1`: every rule StyleBro doesn't cover: status, why, and what it would take to revisit it.
  `New-Mapping.ps1` warns about an uncovered rule without an entry.
- `repos.psd1`: the surveyed repos, and which of their config files apply to production code.

## Checking a rule against StyleCop

`Compare-WithStyleCop.ps1` runs StyleCop and StyleBro on the same case files in `parity/<set>/`, compares the
reported positions, lets each tool fix its own copy, and diffs the fixed files (both must still compile). Every
difference has to be listed under `Expected` in `parity/parity.psd1`, next to a comment naming the documented
deviation; anything else fails. Run it after changing a rule; add a set (and edge-case files) for every new rule
that replaces a StyleCop rule.

```powershell
./Compare-WithStyleCop.ps1                  # all sets
./Compare-WithStyleCop.ps1 -Set blank-lines
```

## Adding a repo

```powershell
git clone --depth 1 https://github.com/owner/repo $env:TEMP/repo
./Invoke-RepoSurvey.ps1 -Path $env:TEMP/repo -Name repo -Solution Repo.sln
```

Then add it to `repos.psd1` (look in `data/repo-configs.csv` for the config files that apply to production code)
and run `./New-Mapping.ps1`.

The survey builds the repo with every StyleCop rule turned on. It does that by temporarily appending a section to
each `.editorconfig`, which it restores with git afterwards; it refuses to run on a checkout with uncommitted changes.
Use `git -c core.longpaths=true clone` on Windows if the repo has long paths.

## Testing more rules with the SDK

Add a case to `sdk-check-cases.ps1`: a small piece of code that violates the rule and compiles. If the SDK needs a
setting to fix it, add that to `sdk-check.editorconfig`. Then run `./Test-SdkCoverage.ps1`, record which SDK setting
did the fix in `decisions.psd1` (and remove the rule's `skipped.psd1` entry), and regenerate. The cases at the end
of `sdk-check-cases.ps1` are probes: `no-repro` there means StyleCop doesn't report an obvious violation at all. A case is "fixed" when `dotnet format style` plus
`dotnet format whitespace` (SDK only, not StyleCop's own fixes) makes the StyleCop diagnostic disappear.

## Measuring a migration

`Measure-MigrationDelta.ps1 -Path <clone> -Solution <sln> -ResetTargetRepo` commits a plain `dotnet format` as the
baseline, runs `stylebro-migrate --write`, then `dotnet format` with StyleBro (StyleCop.Analyzers removed, like a real
migration). The remaining diff (`git diff --ignore-cr-at-eol`) is what switching changes. It resets the target to
`origin/HEAD`, so use a throwaway clone.
