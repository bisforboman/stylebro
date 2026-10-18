# Fewer PR conflicts (2026-10-18)

## Question

Do our PRs get conflicts often, and what could we change to have fewer?

Measured on PRs #173-#252: about a third merged main into their branch, but only 3 of 31 such merges had a real
textual conflict (FixOrderTests.cs, RepositoryNames.cs, InitCommand.cs, one changes/ fragment both
`fix/trial19` and `fix-trial19` wrote), all between sibling PRs of one trial open at the same time. 13 of the 15
recent merges were unnecessary, and 5 of the 7 commits that followed a merge only repaired stale mutation entries.
The merge=union files (CLAUDE.md, Unshipped.md, ...) never conflicted.

## Choices

1. **One PR per trial** (picked): or, when split, a fixed order where the PR touching the shared files merges first.
2. **Merge main only when needed** (picked): a real conflict GitHub keeps showing, or CI needing main's code; after any
   merge, run the mutation stale check before pushing.
3. **Unique changes/ names** (picked): `changes/<branch>.md` with the branch's `/` as a folder (changelog.py reads
   them recursively).
4. **Split docs/getting-started.md** (picked) along its H2 sections: touched by 55 % of PRs, never conflicted yet;
   preventive. Done after trial 31's PR, which edits the page.
5. Not chosen: "require branches to be up to date" (about 20 more merges and full CI runs in the window).

## Decision

1-4. Rules in CLAUDE.md ("Fewer conflicts") and changes/README.md; the page split follows in its own docs PR.
