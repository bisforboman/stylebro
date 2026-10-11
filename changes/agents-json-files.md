### New

- **`stylebro-migrate` for scripts and AI agents** ([Using StyleBro from scripts and AI
  agents](https://bisforboman.github.io/stylebro/agents/)):
  - `--json` on every command: one JSON object on stdout (settings written, conventions found, files changed per run,
    kept findings with their reason, exit code), the human text on stderr.
  - `stylebro-migrate format --files a.cs b.cs` formats only those files and loads only their projects: seconds instead
    of minutes on a large solution. Works with `--verify-no-changes`.
  - `--agents-md` on `init --write` and `--write` writes a short StyleBro section into AGENTS.md (replaced on a re-run,
    the rest of the file kept).
- **llms.txt** on the docs site: every page and rule with its first sentence (and `llms-full.txt`).
