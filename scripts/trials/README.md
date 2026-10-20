# Trial repositories

Repositories for first-run trials, chosen and checked before a trial needs them (owner's decision, 2026-10-19,
[decision](../../docs/decisions/2026-10-19-trial-repositories-are-picked-and-checked-ahead.md)). The procedure that uses
them is in [.trials/README.md](../../.trials/README.md).

- `used.tsv`: every public repository a trial, the real-world CI or a survey has used, and the ones checked and rejected
  (with why). Trials before 29 recorded mostly short names, so many owners there are inferred (the `note` column says so).
  A trial adds its repositories here in its PR.
- `pool/<owner>--<name>.json`: candidates for the targeted picks, one file per repository (parallel PRs don't conflict),
  tagged by what they exercise. An entry with `verified` null is a candidate, not a pick: check it first.
- `random.json`: the fixed list for the random pick. `random.Random(<trial number>).randrange(len(repos))` gives the same
  pick whenever it is rerun, so entries are never re-sorted or removed, only appended. A pick that is in `used.tsv` or
  fails the practical criteria moves to the next entry down (wrapping around); log why.

## A pool entry

```json
{
  "repo": "owner/name",
  "branch": "main",
  "commit": "the full sha the checks ran on",
  "csFiles": 240,
  "tags": ["stylecop", "csharpier", "sonar", "stylesharp", "multi-target", "if-in-bodies", "generator", "xaml", "twae", "library", "app"],
  "setups": "what the repository really has: analyzers, .editorconfig, TreatWarningsAsErrors, frameworks",
  "build": "ok | ok with workaround | fails",
  "workaround": "what a trial must do locally (-p:NuGetAudit=false, a feed removed, ...), or null",
  "tests": "the offline test command and filter, what is left out and why",
  "usedIn": "trial NN, or null",
  "verified": { "by": "session", "date": "yyyy-mm-dd", "sdks": "10.0.x, 8.0.x" },
  "notes": "why it is interesting"
}
```

Checking an entry: clone at the default branch, restore and build Debug with the trial machine's SDKs, run the tests that
need no network, Docker or database, count the `.cs` files outside `obj/`/`bin/`, and fill in every field. Wrong tags are
corrected, not kept; an entry that fails stays with `build: "fails"` and why, so nobody checks it again.
