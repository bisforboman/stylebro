# The in-process engine (experimental)

`bro format` and `bro check` run `dotnet format` for their checks and fixes: one process per target framework,
configuration and pass, each loading and compiling the solution again. With `--engine host` the checks **and the fixes**
run inside the `bro` process instead: each configuration's workspace (Debug, Release, ...) is loaded once, with every target
framework of a multi-targeted project in it, and every check and fix of the command runs on it, in every run.

```
bro check --engine host
bro format --engine host
bro preview init --engine host   # or format, migrate: the preview's format runs
```

It is experimental: the default stays `dotnet format` (`--engine dotnet-format`) until the host has given the same results
on every trial repository. It is meant to do exactly what the default does, only faster: the same changed files, findings,
kept findings, gaps (`Not checked:`), verdict and exit codes. If you see a difference, please report it with both outputs.

## What it uses

The host runs the SDK's own `dotnet format` code, compiler, code-style analyzers and MSBuild: the SDK that `dotnet`
picks in the repository (its `global.json`), the same one `dotnet format` would use. They are loaded next to the tool's
own copy of Roslyn, so nothing in the tool decides which C# version or analyzers you get. Your projects' analyzers
(StyleBro.Analyzers included) load from their packages, as in a build.

That SDK's `dotnet format` needs that SDK's .NET runtime. When the command starts on an older runtime (`bro` targets
.NET 8, which it prefers when it is installed), it runs itself again once on the newest installed runtime. When the host
can't run (no runtime new enough, an SDK without `dotnet format`, or a .NET 8 SDK, whose `dotnet format` finds MSBuild
through MSBuildLocator), the command says so and uses `dotnet format`:

```
Engine: the host can't run here (the SDK's dotnet-format needs .NET 10, and 'bro' runs on .NET 8), so 'dotnet format' runs the checks and fixes.
```

## How it fixes

A fix run in the host does what a `dotnet format` run does, in the same order: the whitespace, line ending, charset and
import formatters as they are, then the code-style fixes, then the fixes of your projects' analyzers. Three things differ,
none of them in what a fix writes:

- **The ids are fixed in a fixed order**, ordinal (`BRO1001`, ..., `BRO1619`, `CA...`, `IDE...`, `xUnit...`), where
  `dotnet format` takes them in an order that changes from one run to the next. StyleBro's tests require the same result in
  every order for the rules whose fixes meet, so the order only makes runs repeatable.
- **Each fixer is called once per id.** `dotnet format` calls it once per project that references the analyzers (17 times
  per id on a library with 8 target frameworks); only the first call changes anything.
- **Only what changed is analyzed again.** Before each id `dotnet format` runs that id's analyzers again over every project
  that had it. The host keeps each analyzer's results per project, for as long as the project's content (its documents and
  those of the projects it references) stays as it was, and runs it again only on the projects a fix changed and on the
  projects that reference them.

## What it reuses

The checks run the same way: a check in the host runs `dotnet format`'s formatters and the analysis it reports, from the same
results as the fixes. So:

- the fix run after a framework's check analyzes again only what that run's earlier formatters changed;
- two runs that share a project (a library every framework's run references) analyze it once;
- the check after a run that changed files analyzes again only the projects a fix reached and the ones referencing them,
  as `dotnet format` would find them: a result is reused only for the same content it was computed on;
- a command's runs keep each configuration's workspace loaded, so run 2 loads nothing again.

Between two runs, and between the runs of a command's frameworks and configurations, the host reads back the files that
changed on disk (a run's own fixes, CSharpier, the indentation `format` keeps, a lone CR it converts), so every run sees
the files a new `dotnet format` process would. A fix in one framework's copy of a file reaches the other frameworks'
copies before their runs, as it does on disk today.

## What it changes

- **Time.** What it saves is the start, load and compile of each `dotnet format` process, and most of the analysis
  between two fixes. See the table below; it helps most where many processes ran (several frameworks or configurations)
  and where a run fixes many rules.
- **Memory.** The process holds every framework's compilations of a configuration at once, for the whole command: a
  repository with many projects and frameworks needs more memory than one `dotnet format` process did.
- **An in-solution analyzer or generator** is loaded once per command: a fix to its own code takes effect in the next
  `bro format`, not in the next run of the same command.
- **`--timings`** shows the workspace loads as `host: load workspace (<configuration>)` and each formatter of a fix run as
  `host: fix <formatter>`.

Options `bro` passes to `dotnet format` work the same way. A run with an option the host doesn't handle (for example
`--binarylog`) starts `dotnet format` for that run.

## Measured

On a 4-core Linux machine with SDK 10.0.113, nothing else running, on the trial repositories after `bro init` (or
`bro migrate`) and the package, before their first `bro format`. "Plain" is one `dotnet format` of the whole solution, which
`bro format` can't use on multi-targeted code (it merges the frameworks' copies of a file); the targets are a first run
within ~3x a plain `dotnet format` and a run on formatted code within ~1.5x a plain `dotnet format --verify-no-changes`.

| Repository | Plain `dotnet format` | `bro format --engine host`, first run | Plain verify of the result | `bro format --engine host` again | `bro check --engine host` |
|---|---:|---:|---:|---:|---:|
| CliFx (CSharpier, 3 frameworks) | 49 s | 57 s (1.2x) | 41 s | 25 s (0.6x) | 26 s (0.6x) |
| error-or (migrated from StyleCop) | 27 s | 31 s (1.2x) | 26 s | 16 s (0.6x) | 16 s (0.6x) |
| Serilog (5 frameworks) | 124 s | 106 s (0.9x) | 55 s | 35 s (0.6x) | 34 s (0.6x) |
| System.IO.Abstractions (7 frameworks, Release) | 207 s | 261 s (1.3x) | 107 s | 98 s (0.9x) | 97 s (0.9x) |
| PalCalc (WPF, 3 configurations) | 140 s | 266 s (1.9x) | 62 s | 135 s (2.2x) | 110 s (1.8x) |

The table was measured when the host started fixing (phase 2). Against the default engine, from the parity runs (same
machine, the two engines one after the other), `bro format` took 35-36 s instead of 116-118 s on CliFx, 22-29 s instead of
60-71 s on error-or, 78-83 s instead of 130-136 s on Serilog, 141-158 s instead of 283-334 s on System.IO.Abstractions,
221-255 s instead of 367-434 s on PalCalc and 489-629 s instead of 1,214-1,443 s on Newtonsoft.Json (8 frameworks). In phase 2's
parity runs the host took 45, 36, 97, 195, 255 and 569-640 s; the checks now share the fixes' analysis and a command's
runs keep their workspaces.

PalCalc's runs on formatted code miss the target: each of its three configurations is a workspace of its own (load and
compile, ~20-25 s each), and `bro check` formats a copy to tell kept findings apart, which loads them again.

The parity harness (`scripts/format-speed/Compare-Format.ps1`, `check`, `format`, `check` with both engines) gave the same
working trees, kept findings, exit codes and `--json` reports on all six, twice each, and the host the same trees in both
rounds. One note differs on Newtonsoft.Json and error-or: the default engine's net20 (net462) run warns about a project reference
it can't match, an effect of forcing one framework on the projects, which the host, loading every framework at once,
doesn't have
([decision](https://github.com/bisforboman/stylebro/blob/main/docs/decisions/2026-10-23-the-format-host-keeps-its-own-workspace-load-warnings.md)).

Trial 47 ran the harness and `bro check --build` with both engines on four more repositories (CSharpFunctionalExtensions,
zio, SmartFormat, fluentvalue). It found one difference, now fixed: in SmartFormat's net462 run the host's view also had the
libraries' netstandard2.0 instances (MSBuildWorkspace sends a project reference it can't match by output to a project's
first instance), and the analyzers reported their copies of the files, which the whitespace formatter had left alone, as
findings at the positions before formatting. A run's references now go to the instances the run names, as in a
`dotnet format` run of that framework. The `changesPerRule` counts of `--json` can differ by one or two between two runs of
the default engine on the same code (CSharpFunctionalExtensions: 104 and 105 BRO1505 changes), because `dotnet format` fixes
the ids in an order that changes from one run to the next, and another fix sometimes adds a blank line BRO1505 would have;
the host's order is fixed.
