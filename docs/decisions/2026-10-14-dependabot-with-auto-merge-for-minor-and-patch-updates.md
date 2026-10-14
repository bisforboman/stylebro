# Dependabot with auto-merge for minor and patch updates (2026-10-14)

## Question

Should dependency updates come from Dependabot, and which of them may merge without a person?

## Choices

- Dependabot, every update reviewed by hand.
- Dependabot, minor and patch updates merge themselves once the required checks pass; majors wait.

## Decision

The second (owner's request, 2026-10-14). `.github/dependabot.yml` checks NuGet, GitHub Actions and the docs' pip
packages weekly, minor and patch updates grouped per ecosystem; `.github/workflows/dependabot-auto-merge.yml` turns on
squash auto-merge for them. Ignored: the Roslyn packages (pinned to 4.8.0 for older SDKs and Visual Studio),
StyleCop.Analyzers (the version the migration sample compares against) and StyleBro.Analyzers (the self-check version,
bumped by hand after a release).
