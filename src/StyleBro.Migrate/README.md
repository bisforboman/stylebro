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

Then add the `StyleBro.Analyzers` package, remove `StyleCop.Analyzers`, and run `stylebro-migrate format` (below).

The report lists the StyleCop rules that nothing enforces after the switch, and why. Details:
[docs/migrating.md](https://github.com/bisforboman/stylebro/blob/main/docs/migrating.md).

## Starting without StyleCop

```
stylebro-migrate init [path] [--write]
```

Adds the severities of the built-in .NET rules StyleBro relies on (IDE0055 formatting, IDE0036 modifier order,
IDE0049 type aliases, ...) to the repository's `.editorconfig`. They have to be there: `dotnet format` ignores rule severities in
a package's global config, where StyleBro's preset is. Not needed after `--write`, which writes them too. When at least
three quarters of the private fields start with `_`, it keeps them (`stylebro_private_field_naming = _camelCase`). In a
repository with a StyleCop setup it stops and points to `--write` instead.

```
stylebro-migrate init [path] --modernize [--write]
```

Also adds the SDK's rules that rewrite code into newer C# and newer APIs (`new()`, collection expressions,
`ThrowIfNull`, ...), in their own block; in multi-targeted projects the newer-API rules are hidden where a target framework lacks the API
(StyleBro's multi-target guard) and the newer-C# rules are suggestions unless `LangVersion` is set. Works after `--write` too. Details:
[docs/modernizing.md](https://github.com/bisforboman/stylebro/blob/main/docs/modernizing.md).

## SonarQube

Both the migration and `init` also read a SonarQube/SonarAnalyzer setup: a `SonarAnalyzer.CSharp` reference (its
default rules), Sonar rule severities in rulesets (SonarLint's and the scanner's too), global configs and
`.editorconfig`, or a quality profile exported from the server (`--sonar-profile profile.xml`). The Sonar rules that
are on turn on the StyleBro and .NET rules that fix what they report (S2325 -> CA1822, S1659 -> BRO1142, S4136 ->
overloads kept together, ...), and the report says which Sonar rules stay Sonar's. Sonar keeps reporting its own ids.
Details: [docs/migrating.md](https://github.com/bisforboman/stylebro/blob/main/docs/migrating.md#coming-from-sonarqube).

## Formatting

```
stylebro-migrate format [folder, solution or project] [--all] [dotnet format options]
```

`dotnet format` with only StyleBro's rules and the built-in rules `init` or `--write` turn on (`--diagnostics ...`),
plus whitespace formatting. Plain `dotnet format` also applies every other analyzer's and the compiler's fixes, which
can break the build; `--all` does that too. Git submodules are never formatted. In a repository with multi-targeted
projects it runs once per target framework: plain `dotnet format` crashes there on the SDK's formatting fix (IDE0055),
because Roslyn can't merge the frameworks' copies of a file. Other options pass through (`--verify-no-changes`,
`--severity warn`, ...). `stylebro-migrate --help` lists every command.

## Preview

```
stylebro-migrate path/to/repo --diff[=file] [--keep]
stylebro-migrate init [path] --diff[=file] [--keep]
stylebro-migrate format [path] --diff[=file] [--keep]
```

Shows what the command would change without touching the repository: it runs on a temporary copy (`--write` for the
first two, then `stylebro-migrate format` until a run changes nothing), prints the settings it wrote, the files each
rule changed and a few sample hunks, and writes the full diff to `stylebro-preview.patch` (or `file`). A repository
without the StyleBro.Analyzers reference gets it in the copy. `--keep` keeps the copy. Example:
[docs/getting-started.md](https://github.com/bisforboman/stylebro/blob/main/docs/getting-started.md#preview-first).

## Baseline

```
stylebro-migrate baseline [path] [--project MySolution.sln]
```

Writes `stylebro.baseline` with every violation `dotnet format` would fix today. The StyleBro.Analyzers package then
hides those in the build, the IDE and `dotnet format`, so only new code has to follow the rules; a violation counts as
new once its line is edited. Whitespace formatting can't be baselined. Details:
[docs/baseline.md](https://github.com/bisforboman/stylebro/blob/main/docs/baseline.md).
