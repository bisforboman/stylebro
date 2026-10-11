# SonarQube setups (2026-10-09)

## Question

Many teams run SonarAnalyzer.CSharp (in the build or through SonarQube's scanner). A survey of the 12 reference repos
with Sonar's default "Sonar way" profile (SonarAnalyzer.CSharp 10.35, 8,170 diagnostics on 176 rules) showed which of
its findings StyleBro or the SDK already fixes and which have no fix anywhere. What should StyleBro do for such teams?

## Choices

1. `stylebro-migrate` reads a Sonar setup (rule severities in .editorconfig, rulesets, globalconfigs) and turns on the
   StyleBro and SDK rules that fix what the enabled Sonar rules report.
2. A StyleBro rule for S1066 (mergeable `if` statements: 287 findings in 11 repos, no fix in Sonar).
3. StyleBro rules for S2971 (`Where(p).Count()` -> `Count(p)`, 61 findings) and S3878 (an array created for a
   `params` parameter, 91 findings), neither with a Sonar fix.
4. Code fixes registered for Sonar's own ids (a probe showed `dotnet format` applies a third-party fixer to a Sonar
   diagnostic), so `dotnet format` fixes what Sonar reports where Sonar has no fix itself.

## Decision

All four. The rules are BRO1149 (S1066), BRO1150 (S2971) and BRO1151 (S3878): on in the preset, off after
`stylebro-migrate` (no StyleCop counterpart), written from Sonar's public rule descriptions and StyleBro's own
reasoning (SonarAnalyzer.CSharp is under the Sonar Source-Available License; its source isn't used). Their fix logic
works on the reported syntax alone (`NestedIfs.GetChange`, `WhereCalls.GetChanges`, `ParamsArrays.GetChanges`), so a
later PR can register the same fixes for S1066, S2971 and S3878 (never for an id Sonar fixes itself: `dotnet format`
would pick either fixer). The migration reading Sonar setups comes in its own PR.

Choice 4 as built: BRO1149-BRO1151's fix providers list S1066, S2971 and S3878 too (Sonar 10.35.0.4138 reports them on
the inner `if` keyword, the `Where` name and the array: the places the StyleBro rules use) and register an action only
where the shared logic gives edits, so Sonar's warning stays wherever StyleBro skips the code. Tests use a stub analyzer
that reports the ids where Sonar does (no Sonar package in the solution, so it can never become a dependency of
StyleBro's packages); `scripts/sonar-interop.ps1` (weekly workflow) checks the real, pinned package end to end.
