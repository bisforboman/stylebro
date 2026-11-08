---
name: stylebro
description: Work with StyleBro, the C# analyzers whose fixes dotnet format applies (rule ids BRO, the bro tool). Use when a C# repository references StyleBro.Analyzers, a build or format reports BRO warnings, after editing C# files in such a repository, or when asked to set StyleBro up or replace StyleCop.Analyzers with it.
---

# StyleBro

[StyleBro](https://bisforboman.github.io/stylebro/) is a set of Roslyn analyzers for C# (rule ids `BRO1001`...) that
replaces StyleCop.Analyzers. Every rule has a code fix, so `dotnet format` (or `bro format`) fixes what it reports. The
`bro` command line tool is the package StyleBro.Tool:

```
dotnet tool install -g StyleBro.Tool --prerelease
```

## In a repository that uses StyleBro

It does when a project, `Directory.Build.props` or `Directory.Packages.props` references `StyleBro.Analyzers`.

1. After editing C# files, format just those files: `bro format --files <the .cs files you changed>`. It loads only
   their projects, so it takes a fraction of a whole run.
2. Before finishing, run `bro check` (or `bro check --files <files>`). Exit code 0 means clean.
3. Exit codes of `bro format` and `bro check`: 0 clean; 2 formatting would still change a file; 3 not verified (part of
   the build wasn't checked: the `Not checked:` lines say what and why, so don't report the run as clean); 1 a usage
   error; anything else is `dotnet format`'s.
4. BRO warnings the format leaves are kept on purpose: a rename would break reflection, a `nameof`, generated code, a
   serialized name, public API, ... `bro format` lists each with its reason. Don't hand-fix them, and don't suppress them
   unless asked.
5. Settings live in `.editorconfig` (`stylebro_*` keys, and the block between the `stylebro-migrate` markers that `bro`
   writes). Don't add `#pragma warning disable` or `[SuppressMessage]` for StyleBro rules unless asked.
6. Use `bro format`, not plain `dotnet format`: plain `dotnet format` also applies every other analyzer's fixes (which
   can break the build), and `dotnet format --verify-no-changes` fails on the findings kept on purpose, so a loop "until
   clean" never ends.

Every command takes `--json`: one JSON object on stdout (`clean`, `exitCode`, `keptFindings` with each finding's
`reason`), the text for people on stderr.

## Setting StyleBro up

Only when asked. Preview first: `bro preview init` (or `bro preview migrate` coming from StyleCop) runs the command and
the format on a copy, prints a summary and writes the full diff to `.git/stylebro-preview.patch`. Nothing in the
repository changes.

- **No StyleCop**: add `<PackageReference Include="StyleBro.Analyzers" Version="..." PrivateAssets="all" />` to the
  root `Directory.Build.props` (with central package management, the version goes in `Directory.Packages.props`), then
  `bro init --write`, then `bro format`.
- **Coming from StyleCop** (a reference to `StyleCop.Analyzers` or `StyleCop.Analyzers.Unstable`): `bro migrate` (a dry
  run), `bro migrate --write`, replace the StyleCop reference with `StyleBro.Analyzers`, then `bro format`. The settings
  keep what the team changed in StyleCop, so StyleCop-clean code stays as it is.

Use the newest version on nuget.org (https://www.nuget.org/packages/StyleBro.Analyzers), the same as the tool's
(`bro --version`). `--agents-md` with `--write` also writes a short StyleBro section into `AGENTS.md`. In CI:
`bro check --severity warn`. A large codebase can start from a baseline: `bro baseline`; it's paid off a folder at a time
with `bro baseline --pay-off <folder>`.

## Docs

- For agents: https://bisforboman.github.io/stylebro/agents/ (`--json`, `format --files`)
- Index of every page and rule: https://bisforboman.github.io/stylebro/llms.txt
- A rule: https://bisforboman.github.io/stylebro/rules/BRO1001/ (any id)
- Findings kept on purpose: https://bisforboman.github.io/stylebro/kept-findings/
