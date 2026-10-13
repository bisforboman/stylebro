# New rules after 1.0 (2026-10-11)

## Question

After 1.0, a minor release adds rules. Today every new rule is on in the preset, so upgrading the package can add
warnings (and `dotnet format` changes) to a build that was clean. How should new rules reach existing users?

## Choices

1. **Off until opted in through a preset version**, like .NET's `AnalysisLevel`: `<StyleBroPreset>1.1</StyleBroPreset>`
   (or `latest`) turns on the rules new in 1.1, so upgrading the package never breaks a build by itself.
2. New rules as suggestions (on, but below warning) until the next major release.
3. On by default, as today.

## Decision

**Choice 1** (owner, 2026-10-11). Details decided and implemented 2026-10-13: [preset versions](2026-10-13-preset-versions-default-init-start-values.md).
