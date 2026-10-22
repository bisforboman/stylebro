# check --build counts its builds as checking held-back IDE fixes they enforce (2026-10-22)

## Question

Trial 42, sbom-tool (StyleCop, migrate, SDK 10.0.113): format and check say "Not verified" only because IDE0049's fixes are
held back (this SDK's `dotnet format` ignores the multi-target guard). `bro check --build` then built Debug and Release
with warnings as errors, "no findings, no new errors"; sbom-tool sets EnforceCodeStyleInBuild=true and IDE0049 = warning,
so those builds ran IDE0049 with the guard applied. check --build still ended with exit 3. Should it count its builds as the
check of the held-back ids when the builds enforce them?

## Choices

1. **Build covers it** (owner's pick): when every built project sets EnforceCodeStyleInBuild and every C# file has the
   held-back ids at warning or error, clean builds remove that gap: Clean, exit 0. Elsewhere the gap stays.
2. Keep as is: exit 3 on SDK 10.0.113 wherever IDE0049 is held back.

## Decision

Build covers it. When the held-back IDE fixes are the only gaps and the builds report nothing, `bro check --build` says
which builds checked them and ends with "Clean." (exit 0) where `FormatCommand.BuildEnforces` holds.
