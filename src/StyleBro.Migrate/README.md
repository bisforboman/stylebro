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

## Baseline

```
stylebro-migrate baseline [path] [--project MySolution.sln]
```

Writes `stylebro.baseline` with every violation `dotnet format` would fix today. The StyleBro.Analyzers package then
hides those in the build, the IDE and `dotnet format`, so only new code has to follow the rules; a violation counts as
new once its line is edited. Whitespace formatting can't be baselined. Details:
[docs/baseline.md](https://github.com/bisforboman/stylebro/blob/main/docs/baseline.md).
