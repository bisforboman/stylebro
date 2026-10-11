# The AGENTS.md section is opt-in (2026-10-10)

## Question

#106 made `init --write` and `--write` write a StyleBro section into AGENTS.md by default (`--no-agents-md` to opt out).
Should a style tool add instructions to a repository's agent file without being asked?

## Choices

1. Opt-in: `--agents-md` writes it; without it the commands print one tip line.
2. Keep it on by default with `--no-agents-md`.

## Decision

1 (owner). Without `--agents-md` nothing is written (also next to a CLAUDE.md), dry runs and `--diff` show the section
only with it, and `--json`'s `agentsMd` is null. `--no-agents-md` is removed: no release had it (0.5.0-alpha.1 was
cut before #106).
