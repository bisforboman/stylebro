# Debug and Release

`dotnet format` loads every project in its default configuration, usually Debug, so code under `#if !DEBUG` (or the
`#else` of `#if DEBUG`) was never fixed or checked, while a CI that builds Release with warnings as errors reports it.
Where a project's code tests a symbol a configuration may set (`DEBUG`, `RELEASE`, `TRACE`, or one of its own
`DefineConstants`), `format`, `--verify-no-changes`, the preview and `baseline` also run the other configurations its
`Configurations` lists that the solution builds (Release; at most four), for that project only, and only where its
code compiles differently than in a run already made: the comparison that lets frameworks share a run. A solution
platform other than Any CPU counts as a configuration of its own (`Release|x64`, also `Debug|x64` in a solution with
only x64), so a project's `'$(Configuration)|$(Platform)' == 'Release|x64'` code is run too. The `Configurations:` line
names them; their runs show up as `== Release` (or `== net8.0, Release`). A repository whose code tests only framework
symbols (`NET8_0_OR_GREATER`) gets no extra run, and neither do the files of a NuGet package a project compiles (a
source package's `#if DEBUG`): `format` never changes them. A project whose project file (or a
`Directory.Build.props`/`.targets` above it) picks its `Compile` items by `$(Configuration)` gets those runs too, `#if`
or not: a file only Release compiles is formatted in the Release run. So does one that generates its documentation file
(`GenerateDocumentationFile`, `DocumentationFile`) only in some configurations: BRO1601 reports only where docs are
generated, and a Release-only setting left its findings to the Release build. A project that runs an in-solution source
generator gets them as well: `format` builds the generator in that configuration first (`Generators: building Gen
first (in Release, ...)`). `--verify-no-changes` also builds such a project in its other configurations at the end
(`Building UsesGen in Release to check it ...`) and is not clean when that build reports one of the rules `format`
fixes (`Release build of UsesGen: 2 findings`), or when that build fails (`Release build of UsesGen failed`, with its
first errors, also next to findings: baselining them doesn't fix the build): then it checked nothing.

## Lines whose if/else body `#if` decides

In code like this, `#if` decides whether a line is the body of an `if`, `else` or loop:

```csharp
#if NETFRAMEWORK
            if (v < int.MaxValue)
                builder.AppendInvariant((int)v);
            else
#endif
            builder.Append(v);
```

Where the symbol is defined the last line is the `else` body, and the formatter indents it; where it isn't, it's a
statement of its own, and the formatter un-indents it. Each framework's or configuration's run undid the other's, so
`format` never settled. It leaves the indentation of such lines as it is (`Indentation kept: ...` names them; it compares
the parses of the symbols its runs define), formats the rest of the file and of those lines (`v+1` becomes `v + 1`), and
their indentation findings don't count in `--verify-no-changes`. A build with IDE0055 as an error still reports one of
them in the configuration whose indentation the line doesn't have: braces (`else { ... }` around both branches' bodies)
settle it for every build.

The same goes for a block that `#if` opens in one build only:

```csharp
#if DEBUG
            try
            {
#endif
            Handle = AsyncSocket.Create(family, type, protocol);
#if DEBUG
            }
            catch (SocketException x) { ... }
#endif
```

Debug indents the statement as the `try` block's content, Release as a statement of the method. `format` keeps its
indentation as it is (`Indentation kept: ...`).

And for a `#region` or `#endregion` line that one build compiles and another leaves out:

```csharp
#if !NET20
        public void Add()
        {
            #region Add
            r = d.Integer + 1;
#if NET6_0_OR_GREATER
            r = d.BigInteger + 1;
#endif
            #endregion
        }
#endif
```

Where the outer `#if` is compiled, the formatter indents `#endregion` with the code; in net20 everything in it is left
out, and the formatter puts a `#endregion` right below another directive in the column of the next compiled code.
`format` keeps its indentation as it is (`Indentation kept: ...`).

## Documentation comments indented with tabs and spaces

`dotnet format` leaves a documentation comment's other `///` lines, and the line of the code below it, when they're
indented with tabs where the first `///` line has spaces (or the other way around) and reach the same column, while the
build's IDE0055 (`EnforceCodeStyleInBuild`) reports them. `format` rewrites the indentation of exactly those lines as
`indent_style` and `tab_width` ask, and `--verify-no-changes` reports them (`IDE0055: Fix formatting: indent this line
with spaces, like the first line of the documentation comment above it`) in projects without
`GenerateDocumentationFile`: with documentation generated the build parses the comments and reports none or only some
of those lines, so verify leaves them to `format` there. Nothing else on those lines changes.
