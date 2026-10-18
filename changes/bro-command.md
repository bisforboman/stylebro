### Breaking changes

- **The tool is now `bro`**, in the package **StyleBro.Tool** (was `stylebro-migrate` in StyleBro.Migrate). The
  commands are `bro init`, `bro migrate` (the StyleCop migration, before `stylebro-migrate [path]`), `bro format`,
  `bro check` (= `bro format --verify-no-changes`), `bro preview <init|migrate|format>` (= the command with `--diff`)
  and `bro baseline`. The old forms still work after `bro`: `bro [path] --write`, `--diff`, `--verify-no-changes`.

### Upgrading

Swap the tool: `dotnet tool uninstall -g StyleBro.Migrate`, then `dotnet tool install -g StyleBro.Tool --prerelease`
(in a tool manifest: `dotnet tool uninstall StyleBro.Migrate` and `dotnet tool install StyleBro.Tool --prerelease`).
Replace `stylebro-migrate` with `bro` in CI scripts and hooks; `stylebro-migrate [path]` becomes `bro migrate [path]`.
The settings blocks in your `.editorconfig` keep their `# BEGIN stylebro-migrate` markers and need no change.
