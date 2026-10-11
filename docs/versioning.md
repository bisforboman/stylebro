# Versioning

StyleBro.Analyzers and StyleBro.Migrate share one version number. What changed in each release:
[GitHub Releases](https://github.com/bisforboman/stylebro/releases).

## Before 1.0

The current releases are alphas (`0.x.y-alpha.n`). Anything may change between them: rules, settings, defaults and
what a fix writes. Read the release notes before upgrading.

## From 1.0

| Release | May | Never |
|---|---|---|
| Patch (`1.0.x`) | fix bugs, including false positives and wrong fixes; make a rule report less | make a rule report more |
| Minor (`1.x`) | add rules and settings | turn a new rule on by itself (see [New rules](#new-rules)) |
| Major (`x.0`) | change defaults, remove settings, change what existing rules fix | |

### Settings and rule ids

- `stylebro_*` keys are kept. A renamed key keeps working under its old name, with a deprecation note, for at least one
  minor release.
- Rule ids (`BROxxxx`) are never reused, also when a rule is removed.

### New rules

!!! note "Planned, not yet implemented"
    Today every rule is on in the preset, so a new rule takes effect as soon as you upgrade.

Rules added after 1.0 stay off until you opt in through a preset version, like .NET's `AnalysisLevel`:

```xml
<StyleBroPreset>1.1</StyleBroPreset>   <!-- the rules up to 1.1 -->
<StyleBroPreset>latest</StyleBroPreset> <!-- every rule in the package -->
```

So upgrading the package never breaks a build by itself: you raise the preset version when you're ready for the new
rules.

## When 1.0 ships

After two clean trials in a row: first runs on new kinds of repositories with no high-severity bugs.
