# Proposal: setup for agents in repositories that use StyleBro

Status: **proposal, for the owner's review** (owner's question 2026-11-07: is there a setup we could define, like the
CSharpier and StyleCop setups, that makes work easier for agents in a repository using StyleBro, such as fewer merge
conflicts). Nothing in `src/` was changed. Backlog: 084-087.

## What those repositories get today

`bro init --agents-md` (a StyleBro section in AGENTS.md), `--skill` (`.claude/skills/stylebro/SKILL.md`), the Claude Code
plugin, `format --files`, `--json`, and tested pre-commit recipes in [pre-commit.md](../pre-commit.md). All of it tells an
agent what to do; none of it enforces anything, and none of it addresses the merge conflicts StyleBro itself causes.

## 1. Open branches over the first big format commit (084)

[getting-started.md](../getting-started.md#3-run-bro-format) says to land the first `bro format` commit "when few pull
requests are open (they will conflict with it)" and stops there. Because StyleBro's fixes are deterministic and
idempotent, an open branch can catch up mechanically:

1. merge the commit before the format commit (the real conflicts, if any, are solved here);
2. take the settings the format commit brought (`.editorconfig`, the package reference, the props);
3. `bro format`, commit;
4. merge the format commit with `-X ours`: the conflicts left are formatting only, and the branch's side is already
   formatted;
5. `bro check` (exit 0).

Proposed: the recipe in getting-started.md ("Open branches"), then maybe a command, `bro catch-up <format commit>`,
which does steps 1-5 and stops at step 1 when a real conflict needs a person. To decide: recipe only, or the command
too.

## 2. stylebro.baseline is a shared file every format rewrites (085)

**Done 2026-11-08** (owner's choice: on every baseline write, `bro format`'s too, not only `bro baseline`): see
[baseline.md](../baseline.md#merging-branches).

`bro format` adds kept findings to the baseline and removes entries whose line is gone, so parallel pull requests that
run it edit the same file. The entries are sorted by path, so only nearby entries conflict, but they will. Not seen in
a trial yet: a prediction.

Proposed: `bro init` and `bro baseline` add `stylebro.baseline merge=union` to `.gitattributes` (GitHub's own merges
ignore it; local merges and rebases, which agents run, honor it). First, duplicate lines must take the larger count:
today they add up (`Baseline.Parse`, `src/StyleBro.Analyzers/Baseline/Baseline.cs`), so a union merge of two edits to
one entry would allow one violation too many. With the larger count, a union merge is exact: a stale entry left by it
is harmless and the next format removes it.

## 3. Does format honor the baseline? Paying it off (086)

Found while answering "how does one reduce the baseline": on a one-project repository with one baselined BRO1401
(SDK 10.0.114 and 8.0.133, in a cloud container), the build hid it, but:

- `bro format`, both 0.8.0-alpha.1 and main's host engine, fixed it and removed its entry;
- `bro check` failed on it;
- plain `dotnet format --include <file>` fixed it too.

[baseline.md](../baseline.md) says `dotnet format` leaves a baselined violation alone. The likely cause is the SDK:
10.0.1xx's `dotnet format` loads no DiagnosticSuppressors (the baseline is one), and FormatHost's comments describe the
suppressor working on 10.0.4xx. Nothing checked it: verify-package's check 4 tested the build only, and check 8's
baselined finding is a kept one, which no format fixes anyway. Check 4 now also checks that `bro check` passes and `bro
format` changes nothing with a fixable baselined violation, and that `StyleBroBaseline=none bro format` fixes it.
**Result (PR #334, 2026-11-07): all three pass on CI's SDK 10.0.403**, so on a current SDK format honors the baseline.
On an older SDK (10.0.1xx, and plain `dotnet format` on 8.0.1xx) one whole-repository `bro format` pays off the whole
baseline and `bro check` fails on it. baseline.md now says so. Open: whether `bro` should warn on such an SDK when a
baseline exists (backlog 086).

Paying it off: baseline.md's recipe was "fix a folder (`dotnet format --include src/Orders`) and regenerate", which only
worked where `dotnet format` ignores the baseline. It now says `StyleBroBaseline=none bro format <project>`, then `bro
baseline` (checked in check 4). Proposed next: `bro baseline --pay-off <folder or files>`, which formats those files
with the baseline off and rewrites the baseline, in one step an agent can be given ("pay off src/Orders"). Unlike the
variable, it would also work where a `Directory.Build.props` sets `<StyleBroBaseline>` without a condition.

## 4. Enforce the loop instead of describing it (087)

Unformatted code that reaches main makes the next agent's format change lines it never edited: diffs that conflict
with other open branches for no reason.

- `bro init --pre-commit [plain|lefthook|husky]` writes the hook from pre-commit.md's tested recipes (check only for a
  plain hook). A git hook covers every agent and every person, not only Claude.
- The Claude Code plugin gets a Stop hook: `bro check --files` on the C# files the session changed (from `git diff
  --name-only`), telling the agent to run `bro format --files` when it fails. Not after every edit: a run takes 10-20 s.

To decide: whether `bro init` writes a hook unasked or only with the flag (proposed: only with the flag, like
`--agents-md`).

## 5. Rules that keep diffs to one line (no item)

Trailing commas in multi-line initializers (BRO1401), parameters on separate lines (BRO1108), enum values on separate
lines (BRO1121), one call per line in a split chain (BRO1523): each makes adding an item a one-line change that doesn't
touch its neighbors. One sentence in the skill and [styles.md](../styles.md) would let teams pick them for that reason.

## Not proposed

- A merge driver that formats both sides before merging: it needs a project load per side (10-20 s each, more with
  several target frameworks). For syntax-aware merges (two branches adding usings at one spot), an existing merge
  driver such as Mergiraf may cover C#; to check before the docs mention it.
- `git rerere` and similar local state: agents' cloud containers are thrown away.
