# A StyleBro skill for AI agents (2026-11-04)

## Question

How does StyleBro reach AI coding agents beyond the opt-in AGENTS.md section (`--agents-md`), `--json`,
`format --files` and llms.txt? And what does the repository itself offer agents working on it?

## Choices

For the skill (an [Agent Skills](https://agentskills.io/) SKILL.md):

1. A Claude Code plugin marketplace in this repository (`/plugin marketplace add bisforboman/stylebro`): works before
   StyleBro is set up in a repository, follows `main`, needs no release.
2. `bro init/migrate --write --skill`: writes the skill into the user's repository for everyone working on it.
3. The SKILL.md on the docs site, to copy by hand.

For this repository: a README section, AGENTS.md for every agent (CLAUDE.md importing it), and Copilot instructions.

## Decision

The owner chose all of them (2026-11-04). How:

- One source: `plugins/stylebro/skills/stylebro/SKILL.md`. The plugin serves it, the tool embeds it (`SkillFile`), the
  docs build publishes it as `SKILL.md` on the site. A test checks the tool's copy is the plugin's. `.github/changes.sh`
  counts `plugins/` as code, since the tool embeds it.
- `--skill` writes `.claude/skills/stylebro/SKILL.md`: Claude Code reads it, and GitHub Copilot and Cursor read
  `.claude/skills` too. One folder rather than one per agent (`.agents/skills` for Codex, `.github/skills`), so a
  repository gets one copy; the docs name the other folders for copying by hand.
- The whole file is bro's: a marker line after the front matter, and a second run replaces only a file that has it,
  like the AGENTS.md section's markers. Opt-in, like `--agents-md`, and no tip line: one tip about agents is enough.
- The repository's agent instructions moved from CLAUDE.md to AGENTS.md; CLAUDE.md imports it and keeps only what is
  Claude Code's (the advisor, AskUserQuestion). `.github/copilot-instructions.md` points at AGENTS.md.
