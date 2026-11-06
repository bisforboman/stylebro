# Vulnerable packages fail every build (2026-11-06)

## Question

How strictly does the repository keep vulnerable NuGet packages, direct or transitive, out of what it builds and ships?
Until now NuGet's audit only warned (NU1901-NU1904), and only for direct packages by default; a warning in a restore
log goes unread, and the test project's transitive `System.Security.Cryptography.Pkcs` (GHSA-846f-f764-76mx) had to be
noticed by hand.

## Choices

Asked of the owner as two multiple-choice questions (2026-11-06).

Q1, "A vulnerable NuGet package (direct or transitive) should fail which builds?":

1. **Every build (Recommended)**: `NuGetAudit` on, `NuGetAuditMode` all (transitive packages too), `NuGetAuditLevel`
   low, NU1901 to NU1904 as errors, in Directory.Build.props, so a local build, CI, the pack and the release all stop
   on a finding.
2. **CI and release only** (`-p:WarningsAsErrors=...` on CI's commands): local builds keep working offline or behind
   an advisory, but every other workflow and the release need the flag too.
3. **Release only**: the warnings stay everywhere else; only the release stops.

Q2, "Add the weekly check that opens an issue when main has a vulnerable package?" (a build fails only when something
builds; this looks for advisories published against packages main already uses):

1. **Yes, weekly issue (Recommended)**
2. **No**

## Decision

The owner answered "Every build" to Q1 and yes to Q2 (2026-11-06, multiple-choice prompt):

- Directory.Build.props sets the four properties and appends NU1901;NU1902;NU1903;NU1904 to `$(WarningsAsErrors)`.
  NU1905 (the audit source can't be read) stays a warning, so an offline restore still works. It reaches only this
  repository's projects: samples/StyleCopMigration and `.trials/` have props of their own that stop it, and the
  fixture, real-world and verify-package runs work outside the repository (they already must: StyleBro's props would
  leak in), so third-party repositories are audited by their own settings, not ours.
- A finding is fixed with a bump or a pin in Directory.Packages.props whose comment names the advisory, never with a
  suppression (`NuGetAuditSuppress`, `NoWarn`). Turning it on found two, both in the format host's compile-only graph
  (it excludes their runtime assets and runs on the SDK's copies, so no shipped package contained them):
  System.Text.Json 7.0.3 (GHSA-hh2w-p6rv-4g7w, from Workspaces.MSBuild 4.8.0) and System.Drawing.Common 4.7.0
  (GHSA-rxg9-xrhp-64gj, from Microsoft.Build.Framework 16.10 through System.Security.Permissions), pinned to 8.0.x.
- `.github/workflows/vulnerable-packages.yml` runs weekly (Monday 06:00 UTC) and on dispatch:
  `.github/vulnerable-packages.py` restores every solution and project StyleBro builds or ships with the errors off,
  runs `dotnet list package --vulnerable --include-transitive --format json`, and writes package, version, severity,
  advisory and projects to the summary. A finding fails the job and, on main, opens or comments on the issue
  "Vulnerable packages found" (`.github/nightly-issue.sh`, one issue per streak). Not a required check.
- The owner's step: turn on Dependabot alerts and Dependabot security updates in Settings, Code security, so GitHub's
  own advisories reach the repository between the weekly runs and open fix PRs.
