# Changelog fragments

What's changed since the last release, one file per pull request, so parallel PRs never edit the same lines of
CHANGELOG.md. Name it after the branch (`changes/fix-trial7-rules.md`); write it for the release notes' reader:
short, concrete, what a user sees, no internal jargon and nothing about private code bases.

```markdown
### Fixed

- **BRO1603** no longer adds a period after text that ends in code (`x = 1`, `a/b`).
```

Sections, in the release notes' order: `### Breaking changes` (words may follow, e.g. `(bro only)`),
`### New`, `### Changed`, `### Fixed`, `### Upgrading`. A fragment can have several. `### Upgrading` is for a step a
user has to take because of this change ("run `bro init` again to ..."); the release adds the package
and tool bump line itself and titles the section "Upgrading from <previous version>". Changes nobody using the
packages would notice (CI, tests, internal docs) need no fragment. CI checks every fragment
(`python scripts/changelog.py --check`).

The release PR folds them into CHANGELOG.md and deletes them, and merging it releases (release.yml):

```bash
python scripts/changelog.py 0.6.0-alpha.1 "130 rules, each fixed by \`dotnet format\`. Docs: https://bisforboman.github.io/stylebro/"
```
