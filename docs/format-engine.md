# The in-process engine (experimental)

`bro format` and `bro check` run `dotnet format` for their checks and fixes: one process per target framework,
configuration and pass, each loading and compiling the solution again. With `--engine host` the **checks** run inside the
`bro` process instead: each configuration's workspace (Debug, Release, ...) is loaded once, with every target framework of a
multi-targeted project in it, and every check of the command runs on it. Fixes still run in `dotnet format` processes.

```
bro check --engine host
bro format --engine host
```

It is experimental: the default stays `dotnet format` (`--engine dotnet-format`) until the host has given the same results
on every trial repository. It is meant to say exactly what the default says, only faster: the same findings, kept
findings, gaps (`Not checked:`), verdict and exit codes. If you see a difference, please report it with both outputs.

## What it uses

The host runs the SDK's own `dotnet format` code, compiler, code-style analyzers and MSBuild: the SDK that `dotnet`
picks in the repository (its `global.json`), the same one `dotnet format` would use. They are loaded next to the tool's
own copy of Roslyn, so nothing in the tool decides which C# version or analyzers you get. Your projects' analyzers
(StyleBro.Analyzers included) load from their packages, as in a build.

That SDK's `dotnet format` needs that SDK's .NET runtime. When the command starts on an older runtime (`bro` targets
.NET 8, which it prefers when it is installed), it runs itself again once on the newest installed runtime. When the host
can't run (no runtime new enough, an SDK without `dotnet format`), the command says so and uses `dotnet format`:

```
Engine: the host can't run here (the SDK's dotnet-format needs .NET 10, and 'bro' runs on .NET 8), so 'dotnet format' runs the checks.
```

## What it changes

- **Time.** What it saves is the start, load and compile of each `dotnet format` process, so it helps most where those
  dominate. On a three-framework library (CliFx: six projects; net10.0, netstandard2.0, netstandard2.1) a `bro check` of
  formatted code took 25 s instead of 35 s (a plain `dotnet format --verify-no-changes`: 40 s). Where the analysis
  itself dominates (a 15-project WPF app in three configurations, a library for seven frameworks), it took about as long
  as the default on a 4-core machine. The fixes, and the copy `bro check` formats to tell kept findings apart, still
  start `dotnet format`.
- **Memory.** The process holds every framework's compilations of a configuration at once: a repository with many
  projects and frameworks needs more memory than one `dotnet format` process did.
- **`--timings`** shows the workspace loads as `host: load workspace (<configuration>)`.

Options `bro` passes to `dotnet format` work the same way. A run with an option the host doesn't handle (for example
`--binarylog`) starts `dotnet format` for that run.
