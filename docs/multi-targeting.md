# Multi-targeted repositories

If any project sets several `<TargetFrameworks>`, plain `dotnet format` doesn't work at all; `bro format`
does. It takes the same arguments and options:

```
bro format
bro format MySolution.sln --verify-no-changes --severity warn
```

Plain `dotnet format` crashes there on the SDK's formatting fix (IDE0055) without writing anything (reported upstream as [dotnet/roslyn#86000](https://github.com/dotnet/roslyn/issues/86000) and [dotnet/roslyn#86001](https://github.com/dotnet/roslyn/issues/86001)). The command runs
`dotnet format` once per target framework instead; without multi-targeted projects it's one run. A project the
solution doesn't list but one of its projects references (so the build compiles it) is formatted in the run of its
framework; one with several frameworks is listed under "Leaving alone" (add it to the solution). So is a project that
needs a workload this machine doesn't have (an Android, iOS or Browser head: `dotnet restore` fails on it with
NETSDK1147); the rest is formatted, and after `dotnet workload restore` the next run formats it too. A diagnostic
every framework sees is printed once, and `--report` writes one `format-report.json` covering all runs. Spellings of
one framework share a run (`net6` and `net6.0`, NuGet's short names). A run loads only the projects that target its
framework, so the renaming fixes also check the names in every other C# file of the repository: a field the tests
read with `GetField("count")` keeps its name in the library's run too, and a name a project outside the run uses
(`UsedInProjectNotLoaded`) is left to a run that loads both, or kept. Files a project compiles from outside its own
folder (`<Compile Include="..\Shared\*.cs" />`) are formatted in the runs of the projects that compile them, and
`--files` finds such a file's project too. Projects that compile the same files with other settings (a library and its AOT variant
compiling `..\Lib\*.cs` with other `DefineConstants`, language version or nullable context for that framework) run in
separate parts of a framework's run (`== net8.0, part 1 of 2`), each with what it references: loaded together, `dotnet
format` crashes on their different edits of one file ([dotnet/roslyn#86001](https://github.com/dotnet/roslyn/issues/86001)).
Projects that compile a shared file with the same settings (a polyfill every library links) share a run: they parse it
the same, so its copies get the same edits (`<LangVersion>latest</LangVersion>` counts as the version it means). This
applies without multi-targeted projects too. Why: [ci.md](ci.md#notes). `bro init` tells you when it finds such projects.

A framework whose code compiles exactly like another's in a project gets no run of its own there: when no `#if` tests a
symbol that tells them apart (say the code tests `NET8_0_OR_GREATER` but nothing newer), `net8.0`, `net9.0` and `net10.0`
see the same files, settings and branches, and one run formats them all. Only frameworks of one kind share a run (.NET,
.NET Standard or .NET Framework, and the same platform such as `-windows`), since their APIs differ more. The
`Multi-targeted:` line names the frameworks that ran and those that didn't need to. A file several projects compile
(`<Compile Include="..\Tests\Helpers.cs" />`) doesn't split them into separate parts when they only define other
symbols it doesn't test.

After a run that changed files, the next one checks every framework at once first: a run whose check is clean isn't
made again, and one whose check found something formats only the files with findings, plus those an earlier run of
the same round changed. A run that changed only line endings or byte order marks (`end_of_line`, `charset`) is the
last one: nothing is left for another to fix.

`dotnet format` may warn that it couldn't load part of the workspace in one framework's run (`'dotnet format' had
warnings loading the workspace in its net6.0 run`). To see the warnings, add `--verbosity diagnostic` to the same
`bro format` command: a plain `dotnet format` loads the projects differently and may not show them.

A multi-line string can hold lines starting with `#` (`#pragma warning disable` in a source generator's test input)
inside an `#if` branch some framework leaves out: there those lines read as directives, and the SDK's formatter moves
them to column 0, breaking the string where the branch is compiled (CS8999). `format` names such files (`Protected:
...`) and, while `dotnet format` runs, swaps those lines' `#` for a character the file doesn't have, then puts it back,
so the rest of the file is formatted as usual. If `format` is killed before it puts the `#` back (a closed terminal, a
crash), the next `bro` command does: it keeps a record of the swap in `%TEMP%/stylebro-mask` (`$TMPDIR` on
Linux and macOS) and says `Restored: <file>`. A file changed in a way that moved the swapped characters is left alone
and named (`Not restored: ...`); `format` and `--verify-no-changes` then stop with `Stopped: ...` until the `#` is back.
A `dotnet format` that outlives a killed `format` still writes those files: they're put back once it has ended, and
until then `format` stops with `Stopped: another 'bro format' ...`. A `#` line in an `#elif` branch or
under a condition like `NET8_0_OR_GREATER && !NET10_0_OR_GREATER`, and a string's own `#if`/`#endif` lines, are
protected too.

A project `dotnet format` can't load at all (F#, `.fsproj`) makes it skip the C# projects that reference it, for the
frameworks where the reference matters. `format` names them once, with that cause ("Partly incomplete: ... Likely
cause: they reference TestFSharp.fsproj"); a restore doesn't change it, and the rest of the repository is formatted.
Any other project skipped for some frameworks leaves the code only those frameworks compile unchecked: when another
framework compiles the same code (one `format` merged into the skipped one's run, e.g. netstandard2.0 into
netstandard1.3), that one runs instead (`== netstandard2.0, instead of netstandard1.3`); otherwise
`--verify-no-changes` says `Not verified` rather than clean.
