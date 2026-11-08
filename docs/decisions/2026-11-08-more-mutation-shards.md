# More mutation shards (2026-11-08)

## Question

The required `mutation` check runs its shards with `timeout-minutes: 90`. Two runs outgrew that on 2026-11-08:

- PR #336 changes `tests/StyleBro.Tests/MigrationTests.cs`, so the runner selects every entry whose tests are in that
  class (873 of 2,241). Shard 2 of 4 (218 entries) was cancelled at the timeout twice, with no surviving mutation;
  shard 0 needed 87 minutes for the same share.
- Main's nightly run (all 2,241 entries, 8 shards) was cancelled at the timeout the same morning; the day before it
  took 89 of its 90 minutes.

How should the check stay under its timeout?

## Choices

Asked of the owner as a multiple-choice question (2026-11-08):

1. **More shards (Recommended)**: pull requests 8 shards instead of 4, the nightly 12 instead of 8. Same runner
   minutes, shorter wall time; fixes the nightly too. In PR #336.
2. **Longer timeout**: `timeout-minutes` from 90 to 150, shards unchanged. Simplest, but runs report later.
3. **Separate PR**: the CI change in its own PR first; #336 waits for it.
4. **The owner handles it.**

## Decision

The owner chose **more shards** (2026-11-08): `.github/workflows/mutation.yml` runs 8 shards on a pull request and 12
at night, `timeout-minutes` stays 90. The required check is still the summary job `mutation`, so branch protection
doesn't change.
