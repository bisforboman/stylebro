# The in-process engine (experimental)

`bro format` and `bro check` run `dotnet format` for their checks and fixes: one process per target framework,
configuration and pass, each loading and compiling the solution again. With `--engine host` the checks **and the fixes**
run inside the `bro` process instead: each configuration's workspace (Debug, Release, ...) is loaded once, with every target
framework of a multi-targeted project in it, and every check and fix of the command runs on it, in every run.

```
bro check --engine host
bro format --engine host
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
can't run (no runtime new enough, an SDK without `dotnet format`), the command says so and uses `dotnet format`:

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
  that had it. The host keeps each analyzer's results per compilation and runs it again only on the projects a fix
  changed, and on the projects that reference them; the others keep their compilation and their results. The check of the
  run after a run that changed files analyzes everything again, so the verdict never rests on a kept result.

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

TIMINGS
