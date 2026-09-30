# StyleCop survey

Scripts and data behind [docs/stylecop-mapping.md](../../docs/stylecop-mapping.md): what happens to each StyleCop rule
when a project moves to StyleBro.

| Step | Script | Writes |
|---|---|---|
| 1. List every StyleCop rule, and whether StyleCop has a fix for it | `Get-Inventory.ps1` | `data/inventory-<version>.csv` |
| 2. Survey a repo that uses StyleCop: its settings, and counts with every rule on | `Invoke-RepoSurvey.ps1` | `data/<name>-configs.csv`, `data/<name>-counts.csv` |
| 3. Check which rules `dotnet format` already fixes with SDK settings alone | `Test-SdkCoverage.ps1` | `data/sdk-check.csv` |
| 4. Generate the mapping | `New-Mapping.ps1` | `docs/stylecop-mapping.md` |

The measured data is committed, so step 4 works on its own. Hand-written parts live in two files:

- `decisions.psd1`: the proposal per rule (SDK / StyleBro / drop), which SDK setting covers a rule, and notes.
- `repos.psd1`: the surveyed repos, and which of their config files apply to production code.

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
did the fix in `decisions.psd1`, and regenerate. A case is "fixed" when `dotnet format style` plus
`dotnet format whitespace` (SDK only, not StyleCop's own fixes) makes the StyleCop diagnostic disappear.
