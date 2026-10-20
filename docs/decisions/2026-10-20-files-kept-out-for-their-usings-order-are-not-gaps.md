# Files a migration keeps out for their usings order aren't gaps (2026-10-20)

## Question

`bro format` leaves out the files whose usings follow the team's enforced ordering rule but not the sort key
(`Migration.KeptUsingsFiles`, Splat's StyleSharp SST1210), because `dotnet format` sorts usings wherever the key is set.
The build still analyzes them with StyleBro. Should they count as gaps ("Not checked:", "Not verified", exit code 3)?

## Choices

1. **Not a gap** (owner's pick): a deliberate migration setup; its `Leaving alone:` line stays, and such repositories
   can still end clean.
2. A gap: those migrated repositories always end "Not verified" until the files are sorted.

## Decision

Not a gap.
