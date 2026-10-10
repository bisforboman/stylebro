# Using StyleBro from scripts and AI agents

`stylebro-migrate` has three things for scripts and AI coding agents: a JSON result for every command (`--json`), a
fast format of just the files an edit touched (`format --files`), and, on request (`--agents-md`), a section in the
repository's `AGENTS.md` that tells an agent how to work with StyleBro. The site also has an index for agents:
[llms.txt](https://bisforboman.github.io/stylebro/llms.txt) (every page and rule with one line each) and
[llms-full.txt](https://bisforboman.github.io/stylebro/llms-full.txt) (all pages in one file).

## The loop for an agent

```
stylebro-migrate format --files src/Orders/OrderService.cs src/Orders/Order.cs   # after an edit: seconds
stylebro-migrate format --verify-no-changes                                      # before finishing: exit 0 = clean
```

- `format --files` fixes StyleBro's findings (and the built-in rules `init` turns on, plus whitespace) in the named
  files only, loading just the project each file belongs to. It repeats until a run changes nothing, like `format`.
- `--verify-no-changes` changes nothing and exits 0 when formatting would change no file. Findings StyleBro's fixes
  keep on purpose don't fail it ([Findings kept on purpose](getting-started.md#findings-kept-on-purpose)); plain
  `dotnet format --verify-no-changes` does fail on them. It takes `--files` too.
- Don't hand-fix the BRO warnings the format leaves: each is kept for a reason (a rename would break reflection, a
  `nameof`, generated code, ...). `--json` lists them with the reason.

Exit codes: 0 clean (only kept findings left, if any); 2 a run still changed files after 3 runs, or with
`--verify-no-changes`, formatting would change a file; 1 a usage error (a file not found, no project above a file);
anything else is `dotnet format`'s.

## `format --files`

```
stylebro-migrate format --files <file> [<file> ...] [--verify-no-changes] [dotnet format options]
stylebro-migrate format <folder, solution or project> --files <file> [<file> ...]
```

Paths are relative to the current folder, or else to the git repository's root. Without a solution or project named,
each file is formatted in the nearest project file above it (files of several projects: one run per project); with one
named, every file must be under its folder. A file a project includes from somewhere else (`<Compile Include="..\x.cs"
/>`) is looked up by folder only: name the project.

Measured (2026-10-10) right after setting StyleBro up, each run until clean (two `dotnet format` runs):

| Repository | Whole solution | `--files`, 2 files | `--files --verify-no-changes` |
|---|---|---|---|
| Jellyfin (41 projects, one target framework) | 169 s | 20 s | 10 s |
| Serilog (the library targets 7 frameworks: one `dotnet format` per framework) | 153 s | 83 s | |

Most of the time is loading projects: a file of a project with many target frameworks costs one load per framework.

## `--json`

Every command takes `--json`: `init`, the StyleCop migration (`stylebro-migrate [path]`, dry run and `--write`), the
`--diff` previews and `format` (with `--verify-no-changes` and `--files` too). The result is one JSON object on stdout;
everything the command would print for people goes to stderr instead, so `stylebro-migrate format --json > result.json`
keeps the text on the screen and the object in the file.

```json
{
  "schemaVersion": 1,
  "command": "format",
  "previewOf": null,
  "path": "C:\\src\\shop",
  "write": false,
  "settings": [],
  "conventions": [],
  "sonar": null,
  "agentsMd": null,
  "runs": [
    { "run": 1, "filesChanged": [ "src/Orders/Order.cs" ], "changesPerRule": { "BRO1303": 2, "IDE0055": 1 } },
    { "run": 2, "filesChanged": [], "changesPerRule": {} }
  ],
  "changesPerRule": { "BRO1303": 2, "IDE0055": 1 },
  "filesPerRule": null,
  "keptFindings": [
    {
      "path": "src/Orders/Order.cs",
      "line": 12,
      "column": 17,
      "rule": "BRO1303",
      "reason": "NameInString",
      "reasonText": "the name is in a string in the solution (reflection, serialization): renaming would compile but break at run time",
      "message": "Rename '_total' to 'total'"
    }
  ],
  "patch": null,
  "clean": true,
  "exitCode": 0
}
```

Every field is always there (empty or `null` where it doesn't apply). Paths in the object are relative to `path`, with
`/`.

| Field | Type | Meaning |
|---|---|---|
| `schemaVersion` | number | `1`. Raised when a field changes meaning or goes away; new fields don't raise it. |
| `command` | string | `init`, `migrate`, `format`, `preview` (`--diff`) or `baseline`. |
| `previewOf` | string or null | For `preview`: `init`, `migrate` or `format`. |
| `path` | string | The folder the command ran on (full path); with `format --files`, the git repository's root. |
| `write` | bool | `init`/`migrate`: whether files were written (`--write`). Always `false` for a preview (it writes to a copy). |
| `settings` | array | `init`/`migrate`/`preview`: every setting of the blocks written (or that `--write` would write): `file` (relative), `section` (`*.cs`, a folder's glob, ...), `key`, `value`, `why` (the comment above it, or `null`). |
| `conventions` | array | `init`: each convention found in the code: `key`, `what`, `counts` (value -> places), `verdict` (`kept`, `default`, `mixed`, `off`, `set`, `tooFew`), `result` (the text after the verdict in the report: the value, the rule turned off, or why the default stays). |
| `sonar` | object or null | With a SonarQube setup: `sources`, `rulesOn`, `fixed` (`sonar`, `by`), `notApplied` (`sonar`, `reason`), `notes`. |
| `agentsMd` | object or null | `init`/`migrate`: `file`, `created`, `changed`, `written` (null without `--agents-md`). |
| `runs` | array | `format`/`preview`: each `dotnet format` run: `run` (1-based), `filesChanged`, `changesPerRule` (`format` only; `{}` in a preview). With `--verify-no-changes`: at most one entry, the files formatting would change. |
| `changesPerRule` | object | Rule id -> changes, all runs together (`format`: from `dotnet format`'s report; `preview`: what the check before the fixes found). Whitespace counts as `IDE0055`. |
| `filesPerRule` | object or null | `preview`: rule id -> the files it changed (`IDE0055` for whitespace-only files, `other` when nothing was reported). |
| `keptFindings` | array | `format`: findings StyleBro's fixes keep on purpose: `path`, `line`, `column` (1-based), `rule`, `reason` (one of the names below), `reasonText`, `message`. |
| `patch` | string or null | `preview`: the patch file (full path). |
| `clean` | bool or null | `format`: no file is left to change (exit code 0). `preview`: the last run changed nothing. `null` for `init`/`migrate`. |
| `exitCode` | number | The process's exit code. |

`reason` is one of: `NameInString`, `NameInNameof`, `OtherPropertyKept`, `PublicApi`, `ImplementsAnotherMember`,
`DerivedMemberHasNewName`, `NewNameTaken`, `NewNameMeansSomethingElse`, `GeneratedReference`, `ReferenceNotByName`,
`AddsErrors`, `DisabledCode`, `NamespaceFromOutside`, `DerivedTypeInAnotherProject`, `NothingToRename`
(what each means: `reasonText`).

## AGENTS.md

It's opt-in: with `--agents-md`, `stylebro-migrate init --write` and `stylebro-migrate --write` put a short StyleBro
section into the repository's `AGENTS.md` (created when there's none), between `<!-- BEGIN stylebro` and
`<!-- END stylebro -->`; running the command again with `--agents-md` replaces it, and leaves the rest of the file
alone. Without `--write` the commands print the section, and `--diff --agents-md` includes it in the patch. Without
`--agents-md` nothing is written; the commands print one tip line:

```
Tip: --agents-md writes a StyleBro section into AGENTS.md for AI coding agents.
```

`AGENTS.md` is the file most coding agents read. With `--agents-md`, a repository with only a `CLAUDE.md` gets an
`AGENTS.md` too, and the command says to add a line `@AGENTS.md` to `CLAUDE.md` (Claude Code reads `CLAUDE.md` and imports what it names).

The section:

```markdown
<!-- BEGIN stylebro (written by stylebro-migrate; edits inside are replaced) -->
## StyleBro (C# style)

This repository uses [StyleBro](https://bisforboman.github.io/stylebro/): analyzers whose fixes `dotnet format` applies.

- After editing C# files, run `stylebro-migrate format --files <the .cs files you changed>` (seconds; the tool is the
  StyleBro.Migrate package: `dotnet tool install -g StyleBro.Migrate`).
- Before finishing, `stylebro-migrate format --verify-no-changes` must exit 0. It does when only findings kept on
  purpose are left.
- Don't hand-fix BRO warnings the format leaves: they are kept on purpose (a rename would break reflection, `nameof`,
  generated code, ...); the format output gives each one's reason. Leave them unless asked.
- Settings live in `.editorconfig` (the `stylebro-migrate` block); don't add `#pragma` or `[SuppressMessage]` for
  StyleBro rules unless asked.
- `--json` gives machine-readable output. Docs for agents: https://bisforboman.github.io/stylebro/agents/ (index:
  https://bisforboman.github.io/stylebro/llms.txt).
<!-- END stylebro -->
```
