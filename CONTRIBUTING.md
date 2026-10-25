# Contributing

## Repository layout

```
src/StyleBro.Analyzers     netstandard2.0, Microsoft.CodeAnalysis.CSharp only (no Workspaces, RS1038)
src/StyleBro.CodeFixes     netstandard2.0, code fixes + Fix All providers
src/StyleBro.Package       packs both DLLs into analyzers/dotnet/cs + build/ preset
src/StyleBro.Migrate       bro: StyleCop setup -> .editorconfig settings, init, format, baseline
tests/StyleBro.Tests       Microsoft.CodeAnalysis.Testing: diagnostics, single fix, Fix All, fix order, doc examples
samples/Messy              dotnet format integration sample (Input -> Expected)
samples/MultiTarget        the same for a project with two target frameworks
samples/StyleCopMigration  a StyleCop project to migrate step by step (see its README)
scripts/                   verify-format, verify-package, benchmark, mutation, realworld, stylecop-survey
docs/rules/                one page per rule, index in docs/rules/README.md
```

Roslyn is pinned to 4.8.0 so the package loads in the .NET 8 SDK / VS 17.8 and newer.

## Building and testing

```
dotnet test StyleBro.slnx -p:StyleBroSelf=none
pwsh scripts/verify-format.ps1
pwsh scripts/verify-package.ps1
dotnet pack src/StyleBro.Package -c Release -o artifacts
```

StyleBro checks its own code with its latest published release. Run the same check as CI before pushing:

```
dotnet format StyleBro.slnx --verify-no-changes --severity warn --exclude samples
```

## Documentation site

`docs/` is published as [bisforboman.github.io/stylebro](https://bisforboman.github.io/stylebro/) with MkDocs Material
(`mkdocs.yml`; the `Docs` workflow builds it on every pull request and deploys it from `main`). Rule help links point
there. To preview it locally (Python 3):

```
python -m venv .venv
.venv/bin/pip install -r .github/docs-requirements.txt      # Windows: .venv\Scripts\pip
.venv/bin/mkdocs serve                                       # http://127.0.0.1:8000, reloads on save
.venv/bin/mkdocs build --strict                              # what CI runs: fails on broken links
```

A new page needs an entry in `nav` in `mkdocs.yml` (a new rule page under its area, in the order of
`docs/rules/README.md`; `DocExamplesTests` checks that). Links that leave `docs/`, or point to the internal pages the
site leaves out (`decisions.md`, `decisions/`, `backlog.md`, `beyond-stylecop.md`, `proposals/`), are absolute GitHub links.

## What every rule needs

- **A code fix, and Fix All works.** `dotnet format` applies fixes through Fix All. Use
  `LinkedFileFixAllProvider.Create(...)`, never plain `FixAllProvider.Create`: in multi-targeted projects every target
  framework has its own copy of each file, and the plain provider writes merge conflicts into them.
- **Fixes converge in one run, in any order.** After a fix the analyzer reports nothing, and a second `dotnet format`
  run changes nothing, whichever order `dotnet format` runs the rules in (`FixOrderTests`). The analyzer and the fix
  share one piece of logic. A new rule, or a new `FixOrderTests` case, also goes into
  `src/StyleBro.FormatHost/FixInteractions.txt` (which rules' fixes `bro format`'s in-process engine may apply together):
  `STYLEBRO_WRITE_INTERACTIONS=1 dotnet test --filter FixInteractionsTests` writes it.
- **Skip what can't be fixed safely.** No diagnostic is better than a wrong fix (for example code between `#if`
  directives).
- **Configuration through `.editorconfig`**, with keys prefixed `stylebro_`.
- **Docs and tests:** a `docs/rules/BROxxxx.md` page (Example with Before/After, Why, then as needed How the fix works,
  Configuration, Not reported, Compared with StyleCop), a row in `docs/rules/README.md` and
  `AnalyzerReleases.Unshipped.md`, an entry in `docs/differences-from-stylecop.md`, unit tests for the single fix and
  Fix All, and a case in `samples/Messy`. `DocExamplesTests` runs every rule page's example.
- **A mutation entry for every guard**, one file per entry: `scripts/mutation/entries/<Area>/<file>-<hash>.psd1`. Write
  it with `scripts/mutation/New-Mutation.ps1 -File <path> -Find <text> -Replace <text> -Tests <filter> [-Comment <text>]`,
  which picks the folder (the guarded file's source folder: `Readability/` for `src/StyleBro.Analyzers/Readability/` and
  `src/StyleBro.CodeFixes/Readability/`, `Migrate/` for `src/StyleBro.Migrate/`, else `Common/`; an entry in the wrong
  folder fails the run) and a name no other pull request picks by accident (the guarded file's name and a hash of the
  mutation). CI breaks each guard and expects a test to fail.

## Pull requests

`main` is protected. Changes go through pull requests, which run:

- `build` and `parity` (every StyleCop parity set);
- `mutation`;
- `performance` (a benchmark against `main`);
- one `Real world` job per repository in `scripts/realworld/repos.psd1`. Each runs every rule, then checks for crashes,
  new compile errors, conflict markers, a second run that still changes files, and new test failures. A repo runs on
  Windows unless its `Os` in repos.psd1 says otherwise (only when the Linux run gives the same results). The tests that
  fail on the untouched code are cached between runs; the job log prints how long each phase took. The `dotnet format`
  runs together must stay under the repo's `MaxFixSeconds` (about twice its usual time on its runner), or the job fails;
  `FixTimeWarnOnly = $true` in repos.psd1 makes that a warning for one repo.

A merge to `main` doesn't run the real-world jobs or the full mutation set again: they run nightly on `main` (and on
demand from the Actions tab), and a failed nightly run opens or comments on an issue named "Nightly real-world run
failed" or "Nightly mutation run failed".

Every script also runs locally.

## Changelog

A pull request with a change users notice (a rule, a setting, a fix, `bro` behaviour) adds
`changes/<branch>.md` with `### New` / `### Changed` / `### Fixed` (or `### Breaking changes`, `### Upgrading`) and
short bullets written for the release notes' reader; [changes/README.md](changes/README.md) has the details. Never edit
CHANGELOG.md itself: the release pull request folds the fragments into it (`python scripts/changelog.py X.Y.Z "..."`),
and merging that pull request publishes the release with that section as its notes. CI checks every fragment. Changes
nobody using the packages would notice (CI, tests, internal docs) need none.

## Plans and decisions

- [Backlog](https://github.com/bisforboman/stylebro/blob/generated/docs/backlog.md): every rule StyleBro has or plans, and
  open work. `python scripts/New-Backlog.py` generates it into `docs/backlog.md` (not committed; CI publishes it to the
  `generated` branch after every merge to `main`). Its planned rules, work items and candidates are one JSON file each in
  `scripts/backlog/<planned|work|maybe>/<NNN>-<slug>.json`, read in file-name order: a new item takes the next free number.
- [docs/decisions/](docs/decisions/): design questions, the choices and what was decided, one file per decision named
  `<yyyy-mm-dd>-<slug>.md`. `pwsh scripts/New-Decision.ps1 -Title "<the question in a few words>"` writes one.

## Issues, versions and security

Issues use the forms in `.github/ISSUE_TEMPLATE/` (bug, false positive, migration problem, feature request).
[docs/versioning.md](docs/versioning.md) says what a patch, minor and major release may change (a patch never makes a
rule report more; settings and rule ids are kept). Vulnerabilities: [SECURITY.md](SECURITY.md).

## Avoiding merge conflicts

Lists that every pull request adds to are one file per entry (decisions, backlog items, mutation entries), so parallel
pull requests don't touch the same file. The lists whose format is fixed (`AnalyzerReleases.Unshipped.md`,
`DiagnosticIds.cs`, the preset, `docs/rules/README.md`, `docs/differences-from-stylecop.md`, `mkdocs.yml`, the StyleCop
survey data) are `merge=union` in `.gitattributes`: a local `git merge origin/main` keeps both sides' new lines. GitHub's
own merge ignores that, so when a pull request shows a conflict, merge `main` locally and check the result for doubled
lines. Keep `AnalyzerReleases.Unshipped.md`'s `### New Rules` table even when it's empty, and rows there and in
`docs/rules/README.md` in id order (a test checks it).
