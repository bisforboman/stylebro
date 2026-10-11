# StyleBro for scripts and AI agents: --json, format --files, AGENTS.md, llms.txt (2026-10-10)

## Question

The owner asked for the four "Agents" ideas of the backlog: a machine-readable result for `init`, the migration,
`--diff` and `format`; a fast format of just the files an agent edited; a StyleBro section in the repository's agent
instructions; and an llms.txt for the docs site. Open details: where the human text goes with `--json`, which file gets
the agent section when a repository has a CLAUDE.md, which projects `--files` loads, and how llms.txt is built.

## Choices

1. `--json`: (a) the object on stdout, the text on stderr; (b) the object on stdout, no text at all.
2. Agent instructions: (a) AGENTS.md always, plus a hint to import it from CLAUDE.md when only that exists; (b) append to
   CLAUDE.md when it exists and AGENTS.md doesn't.
3. `--files`: (a) each file's nearest project file (or the solution/project named), with `dotnet format --include`;
   (b) the whole solution with `--include`.
4. llms.txt: (a) an MkDocs hook in the repository (MkDocs 1.6 has `hooks:`, no new dependency); (b) a third-party plugin.

## Decision

1a: scripts redirect stdout and still see progress and errors; the object has a `schemaVersion` (1) and every field is
always there (docs/agents.md documents them). 2a (the owner prefers AGENTS.md as the cross-tool standard): between
`<!-- BEGIN stylebro` / `<!-- END stylebro -->` markers like the .editorconfig block, written by `init --write` and
`--write`, shown by dry runs and `--diff` (made opt-in the same day, see the entry above). 3a: loading one project instead of the solution is
where the time goes; multi-targeted projects still get one run per target framework, and the repeat-until-clean and
kept-findings list work as for a whole format. 4a: `scripts/mkdocs_llms.py` writes llms.txt (every page and rule page
with its first sentence) and llms-full.txt; the build fails when a rule page is missing from llms.txt.
