# Versioning

StyleBro.Analyzers and StyleBro.Tool share one version number. What changed in each release:
[CHANGELOG.md](https://github.com/bisforboman/stylebro/blob/main/CHANGELOG.md) (the same text as the
[GitHub Releases](https://github.com/bisforboman/stylebro/releases)).

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

Rules added after 1.0 stay off until you opt in through a preset version, like .NET's `AnalysisLevel`:

```xml
<StyleBroPreset>1.1</StyleBroPreset>   <!-- the rules up to 1.1 -->
<StyleBroPreset>latest</StyleBroPreset> <!-- every rule in the package -->
```

So upgrading the package never breaks a build by itself: you raise the preset version when you're ready for the new
rules (each release's notes list them).

- Unset means the package's major version: `1.0` in all of 1.x.
- `bro init --write` pins the newest version it knows in `Directory.Build.props`, so a new setup gets
  every rule at once.
- With `<StyleBroPreset>none</StyleBroPreset>` (also after `bro migrate --write`), rules added after 1.0 are off
  too: they are off by default, and only the preset turns them on. To turn on a single one,
  `dotnet_diagnostic.BROxxxx.severity = warning` in `.editorconfig`.
- A version newer than the package warns (STYLEBRO002) and uses the package's newest; anything else fails the build
  (STYLEBRO001).

Before 1.0 every rule counts as a 1.0 rule: `1.0`, `latest` and unset are the same.

### Styles

A [style](styles.md) (`<StyleBroStyle>microsoft</StyleBroStyle>`) counts as part of the preset's defaults:

- **Patch**: a style setting that contradicts its source and makes a rule report more is corrected (turned off).
- **Minor**: the style's values for that minor's new rules and settings, behind the preset version like the preset's.
- **Major**: the style's value for an existing rule or setting changes, also to follow a change in its source (runtime's
  guide). Until then such a change is listed as a known difference on the style's page.

## When 1.0 ships

After two clean trials in a row: first runs on new kinds of repositories with no high-severity bugs.
