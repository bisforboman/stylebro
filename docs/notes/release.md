# Release setup

How `release.yml` publishes, and the one-time setup behind it. The steps of a release itself are in CLAUDE.md and
CONTRIBUTING.md ("Changelog"); the decision is docs/decisions/2026-10-11-changelog-fragments-per-pr-and-release-on-merge.md.

## What release.yml does

On every push to `main` the `version` job reads the newest `## X.Y.Z – date` (or `X.Y.Z-alpha.N`) in CHANGELOG.md. If
`v<version>` isn't tagged yet, the `release` job runs the same checks as CI, packs with that version (`-p:Version`,
which overrides `<Version>` in the csproj files), pushes StyleBro.Analyzers and StyleBro.Tool to nuget.org through
Trusted Publishing (`NuGet/login@v1`, no stored API key) and creates the GitHub release and its tag on that commit, with
the version's CHANGELOG section as the notes (a prerelease if the version has a `-`). A hand-pushed `v*` tag still
releases that tag (notes from CHANGELOG if it has the section, else GitHub's list of pull requests). On most pushes the
version is already tagged and nothing else runs.

## One-time setup (done)

- A nuget.org Trusted Publishing policy: repository owner bisforboman, repository stylebro, workflow `release.yml`,
  environment `release`, packages `StyleBro.*`, new packages allowed.
- The repository *variable* `NUGET_USER` = `bisforboman` (the nuget.org profile name; a variable, not a secret, so logs
  aren't masked).
- The `release` environment requires the owner's approval.
- Prereleases (a `-` in the version) use the `prerelease` environment instead (owner's decision, 2026-10-01:
  "prereleases auto"), which has no reviewers. Its deployment policy accepts tags matching `v*-*`, and must also allow
  the branch `main` for a release on merge (the owner adds it; until then a prerelease merge fails at the environment
  and publishes nothing, and pushing the `vX.Y.Z-alpha.N` tag on the release commit releases it). It needs its own
  nuget.org Trusted Publishing policy (same repository and workflow, environment `prerelease`).

## When it failed

- The first publish failed with HTTP 401 "No matching trust policy": the policy for that environment was simply missing.
