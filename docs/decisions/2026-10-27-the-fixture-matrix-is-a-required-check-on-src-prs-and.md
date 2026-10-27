# The fixture matrix is a required check on src PRs and nightly (2026-10-27)

## Question

The fixture matrix (scripts/fixtures: tiny repositories in the shapes that kept breaking first-run trials, each run through
preview, init/migrate, format, check, check --build on both engines, a TreatWarningsAsErrors build and its tests) runs as a
sharded CI job. How should it gate pull requests?

## Choices

- Required, on pull requests that change src/ (or the fixtures, the build settings) plus a nightly full run on main that
  opens an issue on failure; the check always reports (step-level ifs, like mutation).
- Not required, on the same pull requests plus nightly.
- Nightly only: no pull request run.

## Decision

"Required, src/ PRs + nightly". `.github/workflows/fixtures.yml`: the job `fixtures` is the required check (it passes only
when every `fixtures shard N` did); `.github/changes.sh` reports `fixtures=true` for src/, scripts/fixtures/, Directory.*,
global.json, nuget.config, the solution and the workflow itself; a docs-only or tests-only pull request reports the check
without the work. The nightly run (04:00 UTC) opens or updates "Nightly fixture run failed". The owner adds `fixtures` to
the branch protection's required checks.
