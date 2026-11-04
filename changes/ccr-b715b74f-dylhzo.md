### New

- **A skill for AI coding agents.** StyleBro's skill tells an agent how to work in a repository that uses StyleBro
  (format the files it edited, `bro check` before finishing, leave the findings kept on purpose) and how to set
  StyleBro up or move from StyleCop. In Claude Code: `/plugin marketplace add bisforboman/stylebro`, then
  `/plugin install stylebro@stylebro`. `bro init --write --skill` (and `bro migrate --write --skill`) writes it into the
  repository as `.claude/skills/stylebro/SKILL.md`, which Claude Code, GitHub Copilot and Cursor read. `--json`
  reports it as `skill`. Details: [Scripts and AI agents](https://bisforboman.github.io/stylebro/agents/#the-skill).
