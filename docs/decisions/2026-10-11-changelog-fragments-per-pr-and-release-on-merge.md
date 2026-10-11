# Changelog: fragments per PR and release on merge (2026-10-11)

## Question

How are release notes written and releases cut: by hand at release time, or the way the owner's other repository,
ShotDetector, does it?

## Choices

1. **ShotDetector's style, in full**: each PR with a user-visible change adds `changes/<branch>.md`; the release PR
   folds them into CHANGELOG.md (`scripts/changelog.py`), which holds the whole history; merging it releases
   (release.yml tags the newest CHANGELOG version when it isn't tagged yet, with that section as the notes).
2. Keep writing the notes by hand at release time and pushing the tag.

## Decision

**Choice 1** (owner, 2026-10-11). Sections: `### Breaking changes`, `### New`, `### Changed`, `### Fixed` (the
headings of StyleBro's hand-written 0.3.0-0.5.0 notes; no `### Faster` as in ShotDetector, speed goes under Changed)
and `### Upgrading`.

"Upgrading from <previous>" (agent's choice): a fragment's `### Upgrading` holds a step users must take because of
that change, since only its PR knows it; the fold script adds the section's title with the previous version and the
package/tool bump line every release needs. So the notes are complete without the release PR writing that section by
hand.

Hand-pushed `v*` tags still release. A prerelease merged to main needs the `prerelease` environment to allow the
branch `main` as well as `v*-*` tags (an owner setting); without it, the release job is refused before publishing and
pushing the tag by hand releases instead.
