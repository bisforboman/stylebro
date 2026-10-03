# stylebro-migrate

Moves a repository from StyleCop.Analyzers to [StyleBro](https://github.com/bisforboman/stylebro) without changing
its code style. It reads the repository's StyleCop setup (rulesets, global configs, `.editorconfig` files,
`stylecop.json`, the StyleCop version) and writes the StyleBro and .NET SDK settings that enforce the same things.

```
dotnet tool install --global StyleBro.Migrate --prerelease
stylebro-migrate path/to/repo           # dry run: what StyleCop enforces, what carries over, the settings
stylebro-migrate path/to/repo --write   # writes them
```

With `--write` it:

- writes the settings into the repository's `.editorconfig` files, between `# BEGIN stylebro-migrate` and
  `# END stylebro-migrate` (running it again replaces the block);
- turns StyleBro's preset off in `Directory.Build.props`, since the generated settings replace it;
- adds the replacing rule ids to existing StyleCop suppressions (`#pragma warning disable`, `[SuppressMessage]`,
  `<NoWarn>`).

Then add the `StyleBro.Analyzers` package, remove `StyleCop.Analyzers`, and run `dotnet format`.

The report lists the StyleCop rules that nothing enforces after the switch, and why. Details:
[docs/migrating.md](https://github.com/bisforboman/stylebro/blob/main/docs/migrating.md).

## Starting without StyleCop

```
stylebro-migrate init [path] [--write]
```

Adds the severities of the built-in .NET rules StyleBro relies on (IDE0055 formatting, IDE0036 modifier order,
IDE0047 parentheses, ...) to the repository's `.editorconfig`. They have to be there: `dotnet format` ignores rule severities in
a package's global config, where StyleBro's preset is. Not needed after `--write`, which writes them too. In a
repository with multi-targeted projects, IDE0047, IDE0048, IDE0055, IDE2000, IDE2002 and IDE2003 are
written as suggestions: their `dotnet format` fixes break such projects (a crash or merge conflict markers).

## Baseline

```
stylebro-migrate baseline [path] [--project MySolution.sln]
```

Writes `stylebro.baseline` with every violation `dotnet format` would fix today. The StyleBro.Analyzers package then
hides those in the build, the IDE and `dotnet format`, so only new code has to follow the rules; a violation counts as
new once its line is edited. Whitespace formatting can't be baselined. Details:
[docs/baseline.md](https://github.com/bisforboman/stylebro/blob/main/docs/baseline.md).
