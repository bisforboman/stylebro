# First-run trials

One section per trial (repositories, what broke, what was fixed, the owner's verdict and the clean-trial tally, environment notes). A new trial adds its `## Trial NN` section at the end of this file. Moved from CLAUDE.md on 2026-10-23.

## Trial 22 init/migrate fixes (2026-10-15)

- `Program.RepositoryFolder`: init and `--write` run from a subfolder move to the git root when the build reaches past the
  folder (props/Packages.props/.editorconfig/.globalconfig above it, unless the folder's own props or `root = true`
  shadows them; a solution in it listing `..` projects). `Migration.SetPreset(importParent)`: a new props below another
  imports it. init reads the root `.globalconfig` under the root `.editorconfig` (`Migration.WithGlobalConfig`; not for
  bulk severities: format ignores a global config's); `AnalysisMode` All + EnforceCodeStyleInBuild = Style at warning
  for the `this.` check. A nested `category-Style` none gets the block's IDE ids `none` (`InitCommand.NestedStyleOff`).
- Owner's choices (docs/decisions/2026-10-15-*): `stylebro_allow_leading_multiline_lambda` (ParameterLayout, default
  false; init counts it); migrate maps StyleCop spacing rules that are off to SDK options from the code
  (`Migration.AddSpacingOfRulesOff`, probed: control-flow keyword option covers `catch`; `new ()` has no own option).
- Migrate: 1.1.x + switch expressions without comma -> BRO1401 off; no block in `root = true` folders without C#;
  suppressions only under `setup.Folders` (no Sonar); usings held back only when the sorted order breaks the enforced rule
  (`Migration.AcceptsSorted`). init conventions: BRO1007 partial parts, empty `{ }` knob, BRO1509 one-line properties,
  BRO1510 mixed accessors, leading lambdas.
- GOTCHA: in this environment `\\n` inside a bash heredoc python script became a real newline; write such scripts with
  the Write tool (there `\\n` stays a backslash-n in the output).

## Trial 23 format fixes (2026-10-15)

- Owner's choices (docs/decisions/2026-10-15-format-*): a clean `format` writes its kept findings into the baseline the
  build reads (`BaselineKept`/`BaselineFile`: `<StyleBroBaseline>` > nearest `stylebro.baseline` above > new at the root;
  other entries stay unless their line is gone; written before `SaveKept`, whose key hashes `.baseline`); Visual Basic
  left alone (`VisualBasicProjects`: no plain whole run, `--exclude **/*.vb`, `UntouchedFiles` puts back a .vb a fix
  reached, VB identifiers in the names file under `<path> (Visual Basic, left alone)` so a loaded VB project still counts
  as outside); generators built first (`BuildGenerators`: `RunAnalyzers=false`, no TWAE; once per process; built =
  TargetPath exists (a rebuild between runs, after format changed the generator's code, made Mediator need a 3rd run); also baseline; verify/preview copies get outputs stamped now).
- `DirectiveStringOffsets`: every subset of the tested symbols (<= 8, else singles and all-but-one); any directive kind.
  The mask record lists the `dotnet format` children (`Track`): Recover waits for them, `Busy` stops another run.
- Speed: `Separate` keys a shared file by the symbols IT tests (`CompileView.Key([file])`; jint net10.0: 4 parts -> 3).
  Tried and dropped: later frameworks' runs on only the files that parse differently. It saved ~1-2 of 30 minutes (each
  run's time is workspace load + compilation, not analysis) and changed results: a rename kept in the run that loads its
  users was no longer done by a later run that doesn't, and a finding the net10 run never reported (build did) stayed.
  jint's "40 minutes" was machine load: old and new tool side by side both take ~30. Unset LangVersion differs per
  framework (C# 12 vs 14), so those frameworks still don't share a run: by design.
- `Baseline.RelativePath` drops `x/..` segments: the compiler names a linked `..\Lib\A.cs` as `Tests\..\Lib\A.cs`.
- Fix-order bug found (analyzer side): `}` + `// comment` + blank + code gives two outputs (BRO1504 vs BRO1506 first).

## Trial 23 init/migrate fixes (2026-10-15)

- `StyleCopSetup.EvaluateStyleCop` evaluates a multi-targeted project per framework when the outer evaluation has no
  StyleCop item (`UsesStyleCop`; fluentassertions: StyleCop under `'$(TargetFramework)' == 'net6.0'`, cached per process).
- init at the root: Style category / bulk off -> the template's IDE rules on by id leave the block (`StyleOffIds`, shared
  with `NestedStyleOff`); StyleBro's rules stay on (decision 2026-10-15). Own path sections are repeated at the block's
  end (`PathSectionOverrides`). Opt-out folders (not root = true) get the sort keys moved into a section for the other
  code (`UsingsAround`: format sorts usings wherever a key is set). Kept one-line empty blocks write
  `csharp_preserve_single_line_statements = true` (probed: IDE0055 splits `else { }` otherwise).
- New conventions: BRO1504 (comments below code, counted on a copy without the blank lines), BRO1603 periods, BRO1618
  `<c>keyword</c>`, non-private fields vs an enforced naming rule (BRO1306 off; `CountNonPrivateNaming` compiles the
  trees without references). BRO1306's off line is shared with the static casing: KeptLines takes a topic for both.
  Leading-lambda calls count apart for BRO1107/BRO1108 (`FoldLeadingLambdas`). Usings are sorted per run between
  directives. PinPreset without a root solution uses the shallowest solutions (stryker's src/).

## Trial 28 analyzer fixes (2026-10-17)

- A member declared `new` that hides a base member by its name isn't renamed (`PascalCaseNamingAnalyzer.HidesBaseMember`,
  part of `InheritsName`; fields in FieldNamingAnalyzer): renamed, the `new` was CS0109 (Jackett, 152 errors). The renamer
  keeps a base member a derived `new` member hides (`KeptReason.HiddenByNewMember`; private/sealed skipped for speed).
- Log keys (owner): a reference inside an interpolation hole whose handler's AppendFormatted has `[CallerArgumentExpression]`
  on its value parameter keeps the name (`CamelCaseRenamer.GetLogKeyHandler`, also TupleElementRenamer;
  `KeptReason.NameIsLogKey`, Where = `path(line) in Handler`). Roslyn 4.8: `GetOperation(InterpolationSyntax)` is null;
  bind the interpolated string and match its `Parts` by syntax.
- Renaming a local/parameter drops a `this.X` that only it made necessary where `dotnet_style_qualification_for_*` is
  false (`GetUnneededMemberThisAsync`, BaseCalls.PrefersNoQualifier): BRO1131 and BRO1301 converge in either order.
- BRO1305 renames the type parameter in its own type's/method's crefs too (cref type arguments are declarations, which
  FindReferences doesn't return).
- XAML: values count only where they can name code (`SnapshotFiles.NamesCode`: markup extensions, `x:` directives,
  `Name`, `*Path`, a one-name value unless the attribute is free text); prefixes are blanked; element/attribute names are
  `Words.XamlMarkup` (`KeptReason.NameInXamlMarkup`, "breaks the XAML build").
- T4 outputs (DependentUpon `*.tt` / AutoGen) join `StyleBroGeneratedFolders` (files listed by themselves); the suppressor
  now also hides `SdkIds` there; `format` excludes the `.cs` next to each `.tt` (`Migration.TextTemplateOutputs`). Not
  `generated_code = true`: that also makes the compiler treat nullable annotations as oblivious (CS8669).
- Submodules: entries at or above `StyleBroRepositoryRoot` are dropped from `StyleBroSubmoduleFolders` (a submodule's own
  checkout read the superproject's .gitmodules and hid everything). verify-package checks 12 and 13.

## Trial 29 fixes (2026-10-17, first trial run as a cloud session)

- Repos: Moq (`v4` branch), Ulid, error-or (StyleCop, migrate), Harmony (random, seed 29). error-or and Ulid clean.
- Harmony (owner: "Both"): BRO1302/BRO1310/BRO1313 never report a `__x` parameter or any parameter of a Harmony patch
  method (`BoundParameterNames.IsBound`, syntax: a `Harmony*` attribute on the method or a parameter, or
  Prefix/Postfix/Transpiler/Finalizer/Prepare/Cleanup/TargetMethod(s) in a `Harmony*`-attributed type). Renamed
  `__result`/`__instance` compiled and Harmony's injection failed at run time.
- Harmony (owner: "Underscore names anywhere"): a private field whose name only a field has (`_x`, `m_x`/`s_x`/`t_x`) is
  kept by ANY string equal to it in the solution, not only a reflection-shaped one (`CamelCaseRenamer.LooksLikeFieldName`;
  tests listed `"_root", "_type"` and read them in a loop). Cost: a private `_id` next to Mongo-style `"_id"` strings stays.
- Harmony (owner: "Count it, BRO1306 off"): `init` counts public/internal non-const, non-static-readonly fields' casing
  when no enforced naming rule covers them (`Conventions.CountNonPrivateNaming`, shares BRO1306's severity convention).
- Moq: `format` excluded the `.cs` next to each `.tt`, but `<#@ output extension=".Generated.cs" #>` writes another file;
  the hand-written `ReturnsExtensions.cs` was left unformatted and the TWAE build failed (1,216 errors after a clean
  verify). `Migration.TextTemplateOutput` reads the directive; `FormatCommand.IsGenerated` uses it too.
- Moq: `Issue1648Harness` isn't in Moq.sln but Moq.Tests references it; the build compiled and reported it, no run's
  `--include` named it. `FormatCommand.OutsideProjectRuns` gives each such single-framework project (transitively
  referenced, under the root) to ONE default-configuration run of its framework (else the framework-less run); a
  multi-targeted one is logged under "Leaving alone" (a framework run would merge its copies). Not in configuration runs.
- Environment (cloud container): the .NET SDK comes from Ubuntu's packages (builds.dotnet.microsoft.com is blocked by the
  proxy; `apt-get install dotnet-sdk-10.0 dotnet-sdk-8.0`), pwsh via `dotnet tool install -g PowerShell`. The container
  runs as root, which ignores read-only files: `Migrate_WithALockedFile_WritesNothing` fails there (and on main).
  Untouched-code build failures seen with TWAE: NuGet audit (NU1902) in Ulid and Harmony (`-p:NuGetAudit=false`),
  Ulid's CS0618/CS8002, Harmony's own IDE0054 and a compiler crash ("PromiseNotToEnqueue") with EnforceCodeStyleInBuild.

## Trial 30 (2026-10-18, clean: owner "Count it clean", tally 1 in a row)

- Repos: rwmt/Multiplayer (RimWorld mod, Harmony patches, Krafs.Rimworld.Ref), mayuki/Chell (T4 `.Generated.cs` next to a
  hand-written `.cs`, multi-targeted), BAndysc/AvaloniaVisualBasic6 (Avalonia, net8.0;net9.0, mobile heads), QuestPDF
  (random, seed 30: duplicati drew first, too large). Kind 4 (projects the solution doesn't list): no buildable repo among
  ~60 checked; a spot copy of Chell without the library in its .sln (owner) exercised "Leaving alone", `--files` on that
  project and `format` on it: identical to the main run.
- Fixed: projects whose evaluation has `MissingWorkloadPack` items (Android/iOS/Browser heads, NETSDK1147) stopped format at
  the restore; now "Leaving alone" with the packs, left out of the restore filter and the runs (`MissingWorkloadPacks`,
  `MissingWorkloadsLine`; no `whole` plain run then). Not covered: a multi-targeted project's outer evaluation might not
  list a platform framework's packs (restore fails as before).
- Measured: init 3-16 s on the four, LiteDB (1,426 files) 15 s, duplicati (1,574 files, 108 projects) 43 s. The `_x` string
  guard in LiteDB (1,911 `"_id"` strings, `_camelCase` repo): 1 coincidental hold (`_doc` = an Expression.Parameter name), 1
  real (`_cacheCtor`, GetField). Harmony: no `__x` or patch parameter renamed; the game can't run here (tests only).
- Environment: untouched Multiplayer/QuestPDF/VB6 fail TreatWarningsAsErrors builds on nullable warnings (compare error
  counts); Multiplayer needs `-p:WarningsNotAsErrors=NU1701`; Chell's net5.0 tests run with `DOTNET_ROLL_FORWARD=Major`.
  The trial feed goes into the user NuGet config before `init --diff` (the preview adds the tool's version). `pgrep -f`
  in a wait loop matches the loop's own command line.

## Trial 31 (2026-10-18, not clean: owner "Not clean", tally 0)

- Repos: irihitech/Ursa.Avalonia (Avalonia/XAML, Android/iOS/Browser heads; `init --style stylecop --write`: pins
  `<StyleBroStyle>stylecop</StyleBroStyle>` in both props files, same .editorconfig as plain init), AArnott/Nerdbank.MessagePack
  (StyleCop, in-solution generator, migrate), Tyrrrz/CliWrap (CSharpier, clean), HangfireIO/Hangfire (random, seed 31:
  bitwarden/server, HandyControl, Radarr, BenchmarkDotNet skipped; also `--style microsoft` on a copy).
- Hangfire's root has COPYING (GPL text: "Copyright (C) 2007 Free Software Foundation") next to LICENSE.md: the first root
  license was the holder, LICENSE.md differed, and `LicensedFolders` returned "." itself: init marked the whole repository
  `generated_code = true` and counted nothing. Now every root license's holder is the root's own and "." is never vendored.
- `StyleCopSetup.EnumerateFiles` skips a folder holding a `.nupkg` (an extracted package: a nuget.config packages folder or
  packages.config's packages/ inside the repository); the preview excludes NuGet's global packages folder when it is inside the
  copy (`PreviewCommand.PackagesFolderInside`, `dotnet nuget locals global-packages --list`). Hangfire's
  `globalPackagesFolder = %USERPROFILE%/...` isn't expanded off Windows: 12,576 "changed" files, a vendored Test.Sdk.
- Owner ("Keep `<Name>Property` fields"): the renamer keeps static fields registering a XAML property or routed event
  (`CamelCaseRenamer.IsRegistrationField`: `...Property` of an AvaloniaProperty/DependencyProperty/BindableProperty type,
  `...PropertyKey`, `...Event` of a RoutedEvent; by type name; `KeptReason.RegistrationField`). Ursa's naming rule renamed 8
  `LevelProperty`s to `_levelProperty`: AVP1022 +176.
- BRO1123's creation fix writes BRO1122's suffix (`LiteralSuffixes.GetSuffixedLiteral`, same value check through
  `SuffixedText`) where it would cast a literal: `new ValueTuple<double, ...>(0, ...)` needed a second run in Ursa.
- `format`: a skip named `Lib(netstandard1.3)` (a multi-targeted reference loaded with all frameworks in another framework's
  run) is Lib's skip for that framework (`FormatCommand.SkippedFor`), not "Incomplete: ... weren't formatted". Hangfire's
  netstandard1.3 projects don't load in `dotnet format` at all (plain `dotnet format` says "Required references did not load",
  the build works), so its verify says "Not verified" by design.
- Environment: blocked feeds (Avalonia nightly, feedz.io) removed from the clones' nuget.config; global.json 10.0.402/patch
  pinned down to the apt SDK's band; `NUGET_CERT_REVOCATION_MODE=offline` (NU3018 through the proxy); Hangfire's
  `signatureValidationMode = require` set to accept for the unsigned trial package. CliWrap's untouched TWAE build fails on
  CS8620 with SDK 10.0.112 (compare counts); Nerdbank's net9.0 test runs can't start (no .NET 9 runtime).

## Trial 32 (2026-10-18, not clean: owner "Not clean", tally 0)

- Repos: ControlzEx/ControlzEx (WPF library, StyleCop 1.2 + WpfAnalyzers, migrate; builds on Linux with
  `EnableWindowsTargeting=true`, its tests are Windows-only), PintaProject/Pinta (GTK, two root licenses with different
  holders, tab_width 8), toddams/RazorLight (xUnit; xunit.analyzers 1.18 swapped in by a hook, owner's decision: no repository
  with real xUnit warnings left builds here, ~50 checked), dotnetcore/CAP (random, seed 32; ShareX, MaterialDesign, Terminal.Gui,
  ReactiveUI, quartznet, AngleSharp, ImageSharp too large, spectre.console/Dapper used, StackExchange.Redis needs a server,
  command-line-api needs .NET 11).
- Held up: the XAML registration guard on WPF (5 private `...PropertyKey` fields kept with BRO1306 + camelCase statics on a
  spot copy), no root vendored with two root licenses, the xUnit allowlist (xUnit2013/xUnit2000 fixed in one run, xUnit1031 left).
- RazorLight (owner: "Exempt those files"): init wrote `end_of_line = lf` (134 of 153 files) and format converted CRLF files
  whose multi-line verbatim strings the tests compare with: 2 failures. `Conventions.LiteralEndingFiles`/`LiteralEndingSections`:
  each file of the other ending with a token spanning lines gets `[path]` + its own ending (init's scopes, migrate's
  `EndingSections`). Probed: `end_of_line = unset` in that section makes `dotnet format` write the OS's ending.
- CAP: `bro format`'s net10.0 run skipped two samples: Dashboard.K8s (net8.0) gets CAP (net8.0;net10.0) as a TRANSITIVE
  project reference, added during the build by `IncludeTransitiveProjectReferences`, so the evaluation-time `Update` never
  marked it `StyleBroFormatLoose`. `SelectFrameworkTargets` has a target after it that marks those too (real-MSBuild test).
- Pinta (one-run fixes): BRO1504's `ContinuesTrailingComment` compares visual columns (`Indentation.GetVisualColumn`,
  `GetTabWidth`: tab_width, else indent_size): the formatter aligns a comment below a trailing comment by visual column;
  BRO1132/BRO1134 join `{` onto the header line when `csharp_new_line_before_open_brace` leaves the block's kind out.
- ControlzEx (owner: "Migrate detects it"): migrate writes `csharp_indent_case_contents_when_block = false` when the code puts
  `case X:` blocks at the label's level (`AddUncheckedFormatting`); format's whitespace pass re-indents them whatever IDE0055's
  severity. The other IDE0055-only conventions are StyleCop's (SA1500, SA1107, SA10xx).
- Environment: setup script works (SDK 10.0.112 + 8.0.131 preinstalled; pwsh via `dotnet tool install -g PowerShell`); Pinta's
  effects tests need `apt-get install libgtk-4-1 libadwaita-1-0`; ControlzEx: global.json removed, `Directory.packages.props`
  renamed (case-sensitive FS), the trial feed added to its NuGet.Config (`<clear/>`); CAP/RazorLight `-p:NuGetAudit=false`;
  RazorLight tests with `DOTNET_ROLL_FORWARD=Major`, MarkdownSnippets regenerates its README on build, 16 Precompile tests fail
  untouched (line endings). `NUGET_CERT_REVOCATION_MODE=offline` is needed for `dotnet test` too (tests restore). A test that
  starts `dotnet build` must not redirect output (a surviving build node keeps the pipe open): log to a file.

## Trial 33 (2026-10-19, not clean: owner "Not clean", tally 0)

- Repos: tmds/Tmds.DBus (runtime style: `_x`/`s_x` fields, `init --style microsoft --write`; in-solution generator, xunit.v3),
  Handlebars-Net/Handlebars.Net (7 CRLF files in an LF repository, one a test file with multi-line verbatim strings),
  dotnet/reactive's Ix.NET (`Ix.Async.NET.sln` leaves out the source generator its projects reference; init moved to the git
  root and counted all of reactive), mono/taglib-sharp (tabs, `tab_width = 4`), scriban/scriban (random, seed 33). Trial 32's
  fixes held: Handlebars' CRLF test file got its own `end_of_line` section, Ix.NET's generator was formatted in the
  netstandard2.0 run, taglib-sharp needed one run. After the fixes all five matched their untouched TWAE builds and tests.
- Lone CRs (owner: "Convert them"): taglib-sharp (`}\r}`) and Scriban (CRLF file with a lone CR) were left out ("Not
  formatted: ... old Mac line endings"), but format and check ended "Clean" and the build failed on them. format now writes
  each lone CR outside literals as the file's own line break (`FormatCommand.WithoutLoneCrs`/`ConvertLoneCrs`: the more
  common of CRLF/LF, else end_of_line, else LF; BOM kept) and says so (`Line endings:`); `--verify-no-changes` doesn't exclude
  them, so check reports them as files formatting would change.
- xUnit (owner: "Skip the ids"): from xunit.analyzers 1.24.0 on NONE of its fixes has Fix All (1.18-1.23 all had; checked by
  loading each version's providers), so `dotnet format` prints "Unable to fix xUnitNNNN ... doesn't support Fix All". format
  said Clean, check Not clean (Tmds.DBus, xunit.v3 3.2.2 -> 1.27.0). `FormatCommand.XunitWithoutFixAll` reads the restore
  output; `Diagnostics` leaves the allowlist out then, and after a restore the run drops it and prints `xUnit: ...`.
- BRO1405 removing the `(` before a multi-line query moves the clause lines aligned under `from` with it
  (`Parentheses.GetQueryAlignment`): the formatter aligns clauses with `from`, so Ix.NET's generator needed a second run.
- Environment: pwsh missing (setup script didn't link it; `dotnet tool install -g PowerShell` + `ln -s` into /usr/local/bin);
  reactive needs a full clone (Nerdbank.GitVersioning); taglib-sharp `-p:NuGetAudit=false`; no .NET 9 runtime (taglib's and
  Tmds' net9.0 test runs can't start, as untouched). `bro init --write` doesn't add the StyleBro.Analyzers reference: add it
  as the preview's patch does. GOTCHA: a mutation loop's `git checkout <file>` reverts uncommitted work in that file: commit
  first.

## Trial 34 (2026-10-19, not clean: owner "Not clean", tally 0)

- Repos: sprache/Sprache (query syntax in parentheses, BRO1405), jaredpar/complog (runtime/Roslyn style `_x`/`s_x`, xunit.v3
  4.0.1 = xunit.analyzers 1.24+; `init --style microsoft --write`), protobuf-net/protobuf-net (a lone CR in a CRLF file,
  xunit.v3 4.0.0, `_x`/`s_`; 1,179 files, through a solution filter of its main projects), zeromq/netmq (random, seed 34:
  ConfuserEx drew first, .NET Framework only). Trial 33's fixes held: TypeModel.cs's lone CRs converted, xUnit 1.24+ format and
  check agree, Sprache's queries aligned in one run (the inner `)` line stays put: owner "Leave it"). After the fixes all four
  converged in one run, check clean, builds and tests as on the untouched code.
- netmq: `#if DEBUG` + `try {` + `#endif` around a statement: the Debug and Release runs re-indented it every run ("still
  changing after 3 runs") while check said clean (its copy formatted to no net change, the leftover whitespace finding read
  as a line ending). `IfBodies.Depth` counts blocks, `IfBodies.OpensBlock` makes a file whose `#if` branches open more braces
  than they close a candidate: the line's indentation is kept ("Indentation kept").
- protobuf-net: BuildTools compiles Core's files with other symbols; BRO1008 reported a using in Core's net8.0 build the fix
  could never move (a polyfill namespace under `#if !PLAT_DYNAMIC_ACCESS_ATTR`), and format said "Clean" above it. Owner
  ("Skip such files"): `UsingPlacement.DeclaresUnderIf` (disabled text with `namespace`, or a type keyword outside the
  namespace body). format now reports what its last check found and no fix changed (`RecordCheckFindings`/`TakeCheckFindings`,
  `FindingsToFix`, `UnfixedLine` shared with verify; "Run N: 0 files changed, but its check found what no fix changes").
  IDE0055 lines count as line endings like WHITESPACE where the run changes nothing (`IsFormattingLine`: a CRLF file's lines
  the formatter rewrites with LF). Projects the workspace doesn't list but references join `Keep`/`Other`
  (`OutsideProjectRuns(..., out outsideProjects)`, `WithOutside`): TestCompatibilityLevel (netstandard2.0, not in the filter)
  references multi-targeted Core, and protobuf-net.Test didn't load in the net8.0 run.
- Environment: SDK 10.0.112 + 8.0.131 and pwsh were there. complog's tests fail as a whole with both `HTTPS_PROXY` and
  `https_proxy` set (a case-insensitive dictionary): run the test executable with `env -u https_proxy -u http_proxy -u no_proxy`;
  its untouched TWAE build fails on CS8002 (Mono.Options). netmq's Debug test host crashes on a `Debug.Assert` untouched: tests
  in Release; Ipv6ToIpv4 fails (no IPv6). protobuf-net: global.json 10.0.201 pinned down; BuildToolsSmokeTests needs a package
  BuildTools can't pack (IsPackable=false): a solution filter without it and the F#/VB/AOT harnesses; its NuGet.Config has
  `<clear/>`, so the trial feed goes BELOW it; 12 untouched TWAE errors (obsolete members in generated code); tests through
  `dotnet test --solution trial.slnf` (MTP). serde-dn needs Roslyn 5.6 (generator, CS9057) and clrmd dnceng feeds (blocked).
  GOTCHA: the container has 15 GB; protobuf-net's format (~10 GB) next to StyleBro's test suite got both OOM-killed (exit 137,
  "Test host process crashed"): run heavy jobs one at a time. `init --write` puts StyleBroPreset in the props file the
  projects import (netmq: src/Directory.Build.props), so add the StyleBro.Analyzers reference there.

## Trial 35 (2026-10-20, not clean: owner "Not clean", tally 0)

- First trial picked from `scripts/trials/` (pool + random.json; seed 35 -> Cysharp/MemoryPack). Repos: ExcelDataReader
  (StyleCop 1.2, migrate; `#if` opening `try {` in XlsWorksheet.cs: settled in one run), praeclarum/sqlite-net (SQLite.cs
  linked into net8.0 tests and netstandard2.0 libraries, iOS projects), castleproject/Core (usings inside the namespace:
  BRO1008 left its two file-wide-`#if` files alone, format clean), MemoryPack. Lens 3 (an unlisted referenced project):
  none of ~40 checked repos had one that builds here; a MemoryPack spot copy without MemoryPack.Streaming in the .sln
  formatted the tests like the real run (Streaming, multi-targeted, "Leaving alone" by design). Lens 2 spot: a using
  whose move would rebind `StringBuilder` stayed, a movable one moved. After the fixes all four matched their baselines.
- Missing workloads (owner: "Leave it alone"): a multi-targeted project's outer evaluation lists no MissingWorkloadPack
  items, so its platform frameworks (`-ios`, `-windows`, ...) are evaluated too (`MissingWorkloadPacksOf`,
  `PlatformFrameworks`); such a project is left alone like trial 30's heads. Pool tag `needs-workload` (owner, via parent).
- IDE0049 (owner: suppress, then "format tells the guard", then "Hold those ids back"): `using X = System.IntPtr;` ->
  `= nint;` broke a C# 7.3 build (CS8370). `MultiTargetSuppressor` hides IDE0049 where the fix needs C# 9 (`nint`) or 12
  (keyword in a using alias) and a framework defaults older (`AllFrameworksDefaultTo`, moved from EmptyRecordBodies) or
  where `bro format`'s list says another project compiles the file with an older C# (`OldCSharpFiles`, a
  `stylebro-*.oldcsharp` AdditionalFile added by the format hook via `StyleBroFormatOldCSharp`). GOTCHA: SDK 10.0.113's
  `dotnet format` style pass loads no DiagnosticSuppressors, so neither this nor the tier-C guard works there
  (`scripts/verify-package.ps1` check 5 fails locally, passes in CI on the newest SDK 10). `bro format` probes it once per
  `dotnet format` version (`FormatHonorsIdeSuppressions`, cached in %TEMP%/stylebro-ide-suppressions-*.txt) and then
  leaves IDE0049/IDE0056/IDE0057/IDE0330 out where the guard matters (`HeldBackIds`, "Held back:").
- BRO1606: filler `Creates a new instance of <c>X</c>.` was cut at the inline `</c>` (`GetContainerEnd`, `<c>X</c>` as a
  name, `HasUnclosedElement`): `class.</c>.`, 32 CS1570 in Castle Core. Renames: a nested type named as reflection writes
  it (`"...Tests+PrivateInterface because"`) keeps its name (6 Castle Core test failures).
- Environment: SDK 10.0.113 + 8.0.132 and pwsh were there. ExcelDataReader needs `EnableWindowsTargeting=true` (env var for
  `bro preview`/`format`: its Sample targets net10.0-windows); sqlite-net: remove global.json, build the test project and
  the nuget/ libraries one by one; MemoryPack's untouched TWAE build fails (50 CS8619): compare with
  `-p:WarningsNotAsErrors=CS8619` too. `bro init --write` adds no StyleBro.Analyzers reference (add it like the preview's
  patch). Don't run `Invoke-Mutations.ps1 -OnlyFiles src/StyleBro.Migrate/FormatCommand.cs`: that is all 257 of its
  entries (hours); apply only the new entries. The stop hook flags commits in trial clones (third-party repos): give them
  the noreply identity, never push them.

## Trial 36 (2026-10-20, not clean: owner "Not clean", tally 0)

- First trial of PR #266's check coverage. Repos: aalhour/C-Sharp-Algorithms (random, seed 36: npoi drew first, 3,125
  files), icsharpcode/SharpZipLib (netstandard2.0 at C# 7.3 next to netstandard2.1/net6.0), nodatime/nodatime (`#if DEBUG`,
  docs with `<c>`/`<see>`, x64/x86 platforms), wiremock/WireMock.Net (an old-style packages.config net48 project; its pool
  entry's StyleCop/Sonar are commented out: init, not migrate). Held up: IDE0049 held back on SDK 10.0.113 with "Not
  checked"/exit 3 in format and check (SharpZipLib, NodaTime, WireMock), WireMock.Net.Service named as not loaded, format's
  Release runs for NodaTime's `#if DEBUG`; C-Sharp-Algorithms clean (exit 0 everywhere). After the fixes every repo matched
  its baseline builds and tests.
- NodaTime (owner: "Format them"): `src/NodaTime.Tools.Common` compiles `..\..\lib\commandline\*.cs`, above `src/` where
  format worked (`dotnet format --include` can't name `../`): the file was dropped silently, check said "the rest is clean",
  the TWAE build failed on 134 findings (`check --build` caught it). `WorkingRoot` now also holds the repository folders a
  project's Compile items reach above it (`LinkedFolders`, text-based, up to the first wildcard, inside the git repository
  only; outside stays someone else's code).
- Preview (owner: "exit 3 like format"): `bro preview` runs format one run at a time through `RunPlain`, which logs the
  `Not checked:` lines but returns 0, so it said "clean after 1 run" and exit 0 where format said "Not verified". It now
  collects the last run's gap lines and ends with `GapVerdict` and exit 3 (`FormatSummary(..., notVerified)`).
- `check --build` (owner: "Not verified, naming them"): a configuration that fails with an in-source error the untouched code
  has too counted as checked, but MSBuild silently skips the projects referencing the failed one (spot repro: "Clean.", exit
  0, App never compiled). `Uncompiled` lists the workspace projects (`BuiltProjects`: ReadProjects minus NotBuiltFolders) with
  no `Name -> output` line and no compiler/analyzer message (MSB/NETSDK/NU don't count); they make it "Not verified".
- Environment: SDK 10.0.113 + 8.0.132 and pwsh were there. WireMock's untouched TWAE build stops in WireMock.Net.Abstractions
  (CS8618, CS8002 RestEase): compare without TWAE (all 75 outputs) instead; SharpZipLib's has 2 SYSLIB0011 in its tests.
  NodaTime has CPM: the trial's StyleBro reference goes into Directory.Packages.props + a version-less PackageReference.
  GOTCHA again: a mutation loop's `git checkout` reverted uncommitted work; commit before testing mutations.

## Trial 37 (2026-10-20, not clean: owner "Not clean", tally 0)

- Repos: microsoft/MSBuildSdks (StyleCop 1.1.118, migrate; `src/Shared` files linked into several projects; net472 to
  net10.0), AArnott/Xunit.Combinatorial (StyleCop 1.2, migrate: changed nothing, 492/492), mcintyre321/OneOf (its F# project
  fails the TWAE restore, NU1504: honestly Not verified), shouldly/shouldly (random, seed 37: sqlite-net drew first). Lens 2
  (a project a failed one keeps from compiling): a spot repro (Broken <- Mid <- App, an MSBuild `<Error>` <- User): `check
  --build` said Not verified naming them. Preview, format, check and check --build agreed on every repository (exit 3: IDE0049
  held back on SDK 10.0.113). Wording nit left: a project failing on its own MSBuild error is listed as "a project they need
  failed"; in HEAD mode an error without an id (MarkdownSnippets') isn't shown under "(above)".
- MSBuildSdks: Directory.Build.rsp says `/Restore`, so every `dotnet msbuild` query that ran a target (CompileSettings'
  `-target:AddImplicitDefineConstants -property:TargetFramework=net9.0`) restored, and a netstandard2.0 project got a net9.0
  restore output (NU1109): `dotnet format` couldn't load Artifacts/CopyOnWrite (12 "Not checked" lines, among them net472's
  "reference assemblies not found"), 4 findings stayed for the TWAE build. `FormatCommand.WithoutRestore`: `Capture` adds
  `-restore:false` to msbuild queries. A plain `-getProperty` query doesn't restore, a target does.
- shouldly (owner: "Analyzer skips snippet regions"): BRO1113 removed `#region ShouldBeObjects`, a MarkdownSnippets snippet,
  and the build failed "Missing snippet". The package targets read the names (`_StyleBroFindSnippets`, compiler-visible
  `StyleBroSnippets`, '|'-separated) from the repository's `*.md` (bin/obj/node_modules left out) when an `mdsnippets.json` is
  above the project; RegionsAnalyzer skips those regions (`Regions.GetName`). GOTCHA: `--` in an XML comment breaks the
  targets file (MSB4024).
- shouldly (owner: "Widen both guards"): Shouldly quotes the call site's code in its messages, and its tests compare them: 37
  of 1,122 failed (`_target`, `ThisString`, `doSomething` in `*.approved.txt`, "`methodCall()` should not throw" strings).
  `CamelCaseRenamer.FindQuoted`: any rename's old name in a snapshot file keeps it (FindInCode's data-member-only check
  removed), and with Shouldly (its assembly or a project named Shouldly) a word in a string of the declaring file
  (`KeptReason.NameInShouldlyMessage`, `ContainsWord`). Rerun: 30 kept, baselined, 1,122/1,122.
- Environment: SDK 10.0.113 + 8.0.132 and pwsh were there; `bro` needed `ln -s ~/.dotnet/tools/bro /usr/local/bin/bro`.
  MSBuildSdks and Xunit.Combinatorial need full clones (Nerdbank.GitVersioning) and global.json lowered; their nuget.configs
  `<clear/>`, so the trial feed goes into them (run 1 only worked from the cached package). MSBuildSdks' `dotnet test -f`
  fails its restore (NU1109): test without -f (exit 1 untouched too, net472). OneOf: `-p:WarningsNotAsErrors=NU1504`, build
  its generator tests without TWAE. Mutation entries' `Tests` must not start with `FullyQualifiedName~` (the runner adds it).

## Trial 38 (2026-10-21, clean: owner "Count it clean", tally 1 in a row)

- Repos: AwesomeAssertions/AwesomeAssertions (assertion library quoting caller code, PublicApiGenerator approval files; StyleCop
  only in its net8.0 build: migrate), Cysharp/MagicOnion (init, in-solution generator, 921 tests), microsoft/vs-solutionpersistence
  (StyleCop GlobalPackageReference, Directory.Build.rsp without /Restore: a spot copy added it), git-ecosystem/git-credential-manager
  (random, seed 38: nunit, sqlite-net, shouldly, DiscordChatExporter, Verify, WireMock used or rejected, xunit/MudBlazor too large,
  yarp needs SDK 11). All converged in one run, verdicts agreed (exit 3 for IDE0049 on SDK 10.0.113; MagicOnion's check --build
  honestly "Not verified": its untouched TWAE restore fails on NU1510), builds and tests matched the baselines. No buildable
  MarkdownSnippets repository (SimonCropp's need SDK 10.0.401+ / C# `with(...)`): lens 2 was a spot repro.
- Snippets (spot): MarkdownSnippets reads `snippet:` in any case, `.mdx` pages, and `include: Name` takes a snippet too (in place
  `<!-- include: Name. path: ... -->`, `...<!-- singleLineInclude: Name. path: ... -->` after the included text). The package
  targets' `_StyleBroFindSnippets` regex and glob read all of them; checked against the real `mdsnippets` tool 28.5.1.
- Owner ("API listings: public names only"): a snapshot shaped like a PublicApiGenerator listing (`namespace X` + `public` type
  declarations, `SnapshotFiles.IsApiListing`) goes to `Words.ApiListings`, which keeps a name only when
  `CamelCaseRenamer.IsInPublicApi` (public/protected all the way out); a spot AwesomeAssertions copy with `_camelCase` had 127 renames
  held by parameter names there, after the fix 0 kept and tests identical. Public names rename only with
  `stylebro_rename_public_api`, so the kept branch is tested with an `.editorconfig` in `KeptFindingsTests.FindAsync`.
- Environment: SDK 10.0.113 + 8.0.132 and pwsh were there (`bro` needs `ln -s ~/.dotnet/tools/bro /usr/local/bin/bro`).
  vs-solutionpersistence: internal feed and MicroBuild packages (signing) removed, InternalsVisibleTo's PublicKey dropped,
  global.json lowered. MagicOnion: `-p:NuGetAudit=false -p:WarningsNotAsErrors="NU1510%3BCS8002"`, packageSourceMapping needs a
  `StyleBro.*` entry for the trial feed; 4 IDE0161 with EnforceCodeStyleInBuild are the untouched code's own. AwesomeAssertions:
  `dotnet test --solution` refuses its VSTest projects; test the MTP projects one by one (5 caller-identification failures in
  Release untouched; net472 needs mono). A baseline script that `cd`s into the clone needs absolute log paths.

## Trial 39 (2026-10-21, not clean: owner "Not clean", tally 0)

- Repos: domn1995/dunet (CSharpier 1.3, in-solution generator, TWAE, C# 14 extension blocks), andrewlock/StronglyTypedId (Verify
  snapshots, generator), dotnet/Open-XML-SDK (StyleCop 1.2, migrate; PublicApiAnalyzers per framework, net35-net10.0, generator,
  TWAE), jstedfast/MailKit (random, seed 39: netstandard2.0-net10.0 and net462/net47/net48, MimeKit submodule in its .sln).
  All converged in one run; TWAE builds and tests matched the untouched code. dunet: format said clean, check named the one file
  CSharpier already rejected before (by design: CSharpier runs only on the files format changes).
- MailKit (owner: "Hide via the solution"): MailKit.sln builds `submodules/MimeKit`'s project; init's new root props gave it
  StyleBro, and the build reported 145,994 findings `format` leaves alone (submodules) while format/check named only IDE0049.
  Trial 28 keeps a submodule's own project's findings; `_StyleBroFindSubmodules` now keeps that submodule in
  `StyleBroSubmoduleFolders` when `SolutionDir` is set and outside it (IDE and `dotnet format` set it too). verify-package check 12.
- Open-XML-SDK (owner: "Follow the code"): `blankLinesBetweenUsingGroups: require` -> `dotnet_separate_import_directive_groups =
  true` made the whitespace pass split root namespaces' usings in 342 StyleCop-clean files (StyleCop's groups are System-first/
  alias/static, the SDK's every root namespace). `Conventions.UsingGroupsKey` counts files (blank line at every root change or
  none); `Migration.AddUsingGroups` writes false unless `SeparatesUsingGroups`. Probed: with false the SDK neither adds nor removes
  those blank lines. The 62 `field` -> `_field` renames (369 vs 62, SA1309 off) are the existing, reported design.
- StronglyTypedId: projects in the .sln without a Build.0 line were "Not built ... left alone" yet listed as "Not checked"
  ('dotnet format' couldn't load them: their package isn't packed). `FormatCommand.ProjectsIn` drops them from the runs.
- Environment: SDK 10.0.113 + 8.0.132 and pwsh were there (`ln -s ~/.dotnet/tools/bro /usr/local/bin/bro`). MailKit: `git submodule
  update --init`, `--project MailKit.sln` (three solutions), 17 untouched TWAE errors (CS0168/CS8632), 64 network tests fail
  untouched. Open-XML-SDK: its nuget.config `<clear/>`s, so the trial feed goes in there; a Debug build takes ~14 minutes and
  check --build ~1 hour. A worker restart kills background waits: check the logs before rerunning.

## Trial 40 (2026-10-21, not clean: owner "Not clean", tally 0)

- Repos: Cysharp/YetAnotherHttpHandler (its HttpClientTestServer submodule's projects are in the .slnx; lens 1), SixLabors/
  ImageSharp.Web (StyleCop from the shared-infrastructure submodule, migrate; lens 2), Blazored/Toast (Razor, bUnit; lens 4),
  danielgerlag/workflow-core (random, seed 40: graphql-dotnet drew first, 1,111 files), a Toast spot copy with one project
  unchecked in every configuration and one in Release (lens 3: no buildable repository had one; Bogus and Blazor.Diagrams
  were used before, check used.tsv for EVERY candidate first). Held up: submodule projects built through the superproject's
  solution report nothing (58 findings built on their own, trial 28's design); migrate kept ImageSharp.Web's own using keys
  (1 file changed); the unbuilt project is "Not built ... left alone", not "Not checked". After the fixes every repository
  matched its baseline (exit 3 only for IDE0049 on SDK 10.0.113).
- ImageSharp.Web (owner: "Write into the folders"): SixLaborsCopyConfigFiles copies shared-infrastructure/.editorconfig over
  the root one before every build, so migrate's block was gone after the first build (format/check Clean, TWAE build 897
  findings). `Migration.Synced` (root .editorconfig text equal to an .editorconfig inside a submodule, every .csproj in a
  top-level folder): migrate's plan and init's targets (`InitCommand.Synced`) write the block into `<folder>/.editorconfig`
  instead, rebased (`Migration.Rebase`); `Migration.BlockFile` finds it for later runs (`IsMigrated`, recorded version/settings,
  unformatted folders, kept usings). A new folder file must not get `root = true` (init's loop default).
- `check --build` before format's changes are committed compared with a HEAD copy that had init's settings but not the fixes:
  its library failed on BRO findings, the projects referencing it never compiled, and their own errors (Toast's CS0067) counted
  as new: a false "Not clean". The copy's build now imports `stylebro-untouched.targets` through the
  `CustomAfterMicrosoftCommonTargets` environment variable (`UntouchedHook`/`WithHook`), appending format's ids to
  `WarningsNotAsErrors`. `NotBuiltIn`: a project the .sln doesn't build in a configuration isn't expected to compile there.
- BRO1102 (owner: "`[A][B]`"): a shared-line split writes `[A][B]`; the whitespace pass removes the space between attribute
  lists, so `[A] [B]` changed again on the next plain `dotnet format` run (`bro format` hid it in its own later pass).
- Unexplained: 2 of 7 ImageSharp.Web previews printed "Without StyleBro: samples/ImageSharp.Web.Sample ... no StyleBro.Analyzers
  in its restore output", both while other heavy dotnet jobs ran; never reproduced alone (new package version in the cache,
  stale artifacts/ and full copies tried). A project without StyleBro is a note, not a gap: the build doesn't run StyleBro there
  either.
- Environment: SDK 10.0.113 + 8.0.132 and pwsh were there (`ln -s ~/.dotnet/tools/bro /usr/local/bin/bro`). YAHA:
  `-p:NuGetAudit=false`, its tests load `runtimes/linux-x64/native/*.so` relative to the working folder (copy it into the test
  output), one gRPC loopback test can hang (`--blame-hang-timeout 3m`). ImageSharp.Web: feedz.io and dnceng removed from
  SixLabors.Global.props' RestoreSources and the trial feed added there (committed in the submodule and the clone, not pushed),
  LFS images not fetched (4 tests fail untouched), AWS/Azure tests need emulators. workflow-core: `-p:WarningsNotAsErrors=NU1510`
  (its TWAE restore fails, so check --build honestly says Not verified), database/queue tests need servers. `pkill -f`/`pgrep -f`
  with a pattern from the same command line kills the calling shell (exit 144): filter by process name instead. Count TWAE
  errors with `--no-incremental`, and build the tests without TWAE (a failed TWAE build leaves no test binaries).

## Trial 41 (2026-10-21, clean: owner "Count it clean", tally 1 in a row)

- Repos: SixLabors/PolygonClipper (StyleCop from the shared-infrastructure submodule whose `.editorconfig` the build copies over
  the root one: migrate wrote into `src/` and `tests/.editorconfig`; lenses 1 and 2), havit/Havit.Blazor (Razor, 20
  `[Parameter, EditorRequired] public ...` lines, x64/x86 solution platforms; lenses 3 and 4), ClosedXML (random, seed 41:
  websocket-sharp has no tests, the next three were used or rejected). All converged in one run; preview, format, check and
  check --build agreed before and after committing (exit 3 only for IDE0049 on SDK 10.0.113; PolygonClipper's honestly
  "Not verified": its vendored tests/GeoJson fails TWAE untouched, so the tests never compile); builds and tests matched.
- BRO1509 + attributes (owner: "Own lines"): the SDK formatter puts each attribute list of a member spanning several lines on
  its own line (types, methods, ctors, operators, properties, indexers, events, accessors; not local functions/parameters;
  probed with `dotnet format whitespace`), but the whitespace pass runs before the fixes, so expanding `[A] public void M() { }`
  needed a second run. `SingleLineBlocks.AddAttributeLines` breaks after each of the owner's lists in the same edit.
- BRO1139 + BRO1519 (owner: "Keep the blank line"): `else { if (x) y; }` + a statement gave ClosedXML a different blank line
  per fix order (preview and format disagreed in 2 files). `ElseIfs.GetChanges` adds BRO1519's line after the removed `}`
  (`BlankLineRuns.JudgesGapAfter`), except when the join adds braces (the wrap brings it). Lesson: compare preview and format
  output file by file, not only the file counts; FixOrderTests only sees analyzer fixes, never the whitespace pass.
- Lens 3: BRO1102's `[A][B]` settles in one run with CSharpier (it puts each list on its own line) and with the SDK formatter
  (Havit copy with BRO1102 on: `[Parameter][EditorRequired] public ...`, a later plain `dotnet format` changed only IDE0049).
- Environment: SDK 10.0.113 + 8.0.132 and pwsh were there (`ln -s ~/.dotnet/tools/bro /usr/local/bin/bro`). PolygonClipper:
  `git submodule update --init`, feedz.io/dnceng removed from SixLabors.Global.props and the trial feed added there
  (RestoreSources overrides nuget.config). Havit.Blazor: global.json's sdk pin removed (keep `{"test":{"runner":
  "Microsoft.Testing.Platform"}}` or `dotnet test --project` fails), Microsoft.CodeAnalysis 5.9 -> 4.14 (CS9057), trial feed
  plus a `StyleBro.*` mapping in its nuget.config; E2E tests left out. ClosedXML's nuget.config `<clear/>`s: a version already
  in the package cache restores anyway, a new one fails (NU1102). The mutation loop's `git checkout` reverted uncommitted
  source once more: commit before testing mutations (third time this has bitten).

## Trial 42 (2026-10-22, not clean: owner "Not clean", tally 0)

- Repos: nilaoda/N_m3u8DL-RE (random, seed 42: the list walked from nunit past ten used/rejected entries; net10.0 console app,
  Shouldly tests), microsoft/sbom-tool (StyleCop 1.2, migrate, EnforceCodeStyleInBuild, TWAE, net8.0;net472), Tyrrrz/PolyShim
  (CSharpier, TWAE, 23 frameworks net35-net10.0, C# 14 extension blocks), microsoft/kiota-dotnet (netstandard2.0-net10.0-browser,
  net462, in-solution generator). Lens 1 (one-line members with attributes, else-if after jumps) was a spot repro: none of the
  four had such members; trial 41's fixes held (one run, preview = format, a later plain `dotnet format` clean).
- MSBuild's query mode (`-getProperty` without a target) evaluates every item's Condition, also inside a group whose own
  Condition is false: PolyShim's `VersionGreaterThanOrEquals($(TargetFrameworkVersion), '4.5')` under a .NETFramework group
  failed the outer evaluation (MSB4184) and init/format/preview stopped at the first query. `Capture` retries such a query
  with `-target:InitializeSourceControlInformation` (empty in single- and multi-targeted targets; `WithEmptyTarget`).
- File-based programs (owner: "Package skips them"): `dotnet run app.cs` imports the repository's Directory.Build.props, so
  PolyShim's build-time `dotnet run` of GenerateSignatures.csx failed on 37 BRO findings (the init block's `[*.cs]` doesn't
  cover `.csx`, the preset does). `dotnet format` 10.0.113 can't load such files. `_StyleBroLeaveFileBasedPrograms` removes
  the package's Analyzer items where `FileBasedProgram` is true.
- BRO1151 skips members of C# 14 extension blocks (`ContainingType.TypeKind` 14, unnamed in Roslyn 4.8): the compiler reports
  CS8620 for params elements passed one by one there (Task.WhenEach polyfill: 6 TWAE errors). Covered by Messy
  (TextExtensions.cs `TaskCounts`); unit tests can't parse C# 14, so no mutation entry.
- Preview: its runs are `RunPlain`, so the in-solution-generator configuration gap `Repeat` adds was missing; the preview now
  adds `FormatCommand.GeneratorGaps` after each run (`RememberGeneratorBuilds`).
- check --build (owner: "Build covers it"): when the only gaps are held-back IDE fixes (`HeldBackReason`) and the builds are
  clean, `BuildEnforces` (every built project's EnforceCodeStyleInBuild true, every C# file's severity warning/error) turns
  the verdict into "Clean." (exit 0). sbom-tool now exits 0; kiota (no EnforceCodeStyleInBuild) keeps exit 3.
- Kept as designed: PolyShim's netstandard1.x-only code doesn't load in `dotnet format` (named as Not checked); check --build
  reports its BRO1303 `HashSizeValue` there.
- Environment: SDK 10.0.113 + 8.0.132 and pwsh were there (`ln -s ~/.dotnet/tools/bro /usr/local/bin/bro`). kiota: global.json
  10.0.401 -> 10.0.100, Microsoft.CodeAnalysis 5.9 -> 4.14 (CS9057), net472 tests need mono. PolyShim's tests are net6.0
  (`DOTNET_ROLL_FORWARD=Major`). GOTCHA: a clone left in `.trials/` with `*.verified.txt` files (OpenAPI.NET) made
  verify-format fail (Messy renames kept: SnapshotFiles scans the git root on disk, `.trials/` included); delete unused clones.
  Two background jobs on one worktree collided after a worker restart: check `ps` before rerunning.

## Trial 43 (2026-10-22, not clean: owner "Not clean", tally 0)

- Repos: RicoSuter/NSwag (random, seed 43: yarp, git-credential-manager, N_m3u8DL-RE used or rejected, DnsServer has no
  tests), martincostello/xunit-logging (StyleCop 1.3 beta, migrate; C# 14 extension block), Particular/NServiceBus.Gateway
  (EnforceCodeStyleInBuild with 52 IDE rules at warning/error), Tyrrrz/Gress (CSharpier.MsBuild runs csharpier in the build,
  C# 14 extension blocks). Lens 1 spot: BRO1151 leaves a params array passed to an extension member alone, fixes a plain one.
- Gateway (owner: "Set by id"): `src/.globalconfig` says `dotnet_analyzer_diagnostic.severity = none`; `dotnet format`'s
  analyzer pass reads a global config's bulk severity (the comment in init said it doesn't), the build lets the preset's ids
  win: format fixed nothing, check said Clean, check --build found 186. `Migration.WithGlobalBulk` (every .globalconfig in the
  repository, lowest per key, under the .editorconfig's own; an .editorconfig `default` doesn't override it in format) feeds
  init's `HiddenFromFormat` and `FormatCommand.HiddenLine` ("Not clean: A .globalconfig's bulk severity ...").
- NSwag (owner: "Warn, and patch includes it"): `.editorconfig` is in its .gitignore. `InitCommand.IgnoredLine` (init and
  migrate, after each written file: `git check-ignore -v`) names the line; `PreviewCommand.AddIgnoredSettings` force-adds
  ignored .editorconfig files (bin/obj aside) before the preview's baseline and settings commits, so the patch has them.
- NSwag: BRO1606 flipped a cref every run in `SwaggerSettings.cs` (`#if AspNetOwin` `class SwaggerSettings<T>` `#else`
  `class SwaggerSettings`, a file two projects compile). `ConstructorSummaries.HasConditionalHeader` skips a type whose
  header (to its `{`) has `#if` branches; an `#if` above the whole type doesn't count.
- NSwag (owner: "Keep quoted doc text"): BRO1603's period on a test controller's `<summary>Test summary</summary>` failed a
  test comparing what NSwag reads into the OpenAPI document. `QuotedDocumentation` (CodeFixes) keeps BRO1603-BRO1607 where
  the element's text (plain text only, white space collapsed, two words or more) is a string of the solution
  (`CamelCaseRenamer.GetNamesInCodeAsync`) or in a snapshot file (`SnapshotFiles.Words.Texts`); `KeptReason
  .DocumentationTextInString`, no code action. `Verifier.VerifyFixAsync(..., keepsSome: true)` lets the fixed sources' markup
  name kept findings (the framework ignores fixable ones there by default).
- Held up: preview = format on all four (patch applied to the base commit, CR ignored: `.trials/trial43/cmp3.sh`); NSwag's six
  CRLF blobs under `* text=auto` are converted by format but not shown by the preview (its fresh copy normalizes them on add:
  git's own renormalization). check --build's Release|x86 "Not verified" on NSwag is honest (the untouched code fails there).
- Environment: SDK 10.0.113 + 8.0.132 and pwsh were there (`bro` needs `ln -s ~/.dotnet/tools/bro /usr/local/bin/bro`).
  Gateway: feedz.io removed, Particular.Analyzers 2.1.4 -> 1.8.0, Particular.Packaging removed, Particular.Approvals 2.0.1
  packed locally from its repository (packageSourceMapping per source). xunit-logging: global.json 10.0.402 lowered, the
  trial feed needs a `StyleBro.*` mapping. NSwag: the WiX installer fails on Linux (use NSwag.NoInstaller.slnf); 52 tests fail
  untouched (network, npm/tsc, sample builds). GOTCHA: `--filter ...Snapshot...` runs tests that scan the git root on disk,
  which spin for many minutes with trial clones in `.trials/` (delete them first or narrow the filter).

## Trial 44 (2026-10-22, not clean: owner "Not clean", tally 0)

- Repos: team-charls/charls-dotnet (StyleCop 1.2, migrate; root `.globalconfig` `dotnet_analyzer_diagnostic.severity = error`,
  AnalysisLevel latest-All, TWAE), RicoSuter/NJsonSchema (init; XmlDocsTests read doc text at run time, Verify snapshots,
  netstandard2.0;net462;net8.0), dailydevops/arguments (init; NetEvolve.Defaults), MessagePack-CSharp (random, seed 44: index 13
  FastEndpoints, then 20 used/rejected entries to index 7; StyleCop, migrate, in-solution generator). Lens 2 was also a spot repro
  (`spot/ifheader`: `#if` class headers in a file two projects compile, a test const quoting a summary): held up.
- NetEvolve.Defaults (owner: "Detect, warn, Not verified"): its build copies `configurations/template.editorconfig` (and a
  `.gitignore` ignoring `**/.editorconfig`, so trial 40's folder files don't help) over the root files before every build.
  `Migration.PackageCopy` compares the root .editorconfig's non-empty lines, the block aside, with each `*editorconfig` file the
  restore outputs' (`project.assets.json`: each project's obj/, artifacts/obj, a root obj/) packages list; init/migrate print
  `PackageCopyLine`, format/check/preview add `PackageCopyGap` (Not verified).
- xUnit (owner: "A note, like IDE0049"): xunit.analyzers 1.24+ was dropped silently when the restore output was there before the run
  (Diagnostics) and a gap only after a fresh restore, so charls' preview said Not verified and format Clean. `XunitGap` is printed
  either way and ends like `HeldBackReason` (`NoteEnd`), so `Unverified` leaves both notes out.
- MessagePack: format ends Not verified for two generator test projects in Release; check and check --build build them: Clean (by design).
- Environment: SDK 10.0.113 + 8.0.132 and pwsh were there (`ln -s ~/.dotnet/tools/bro /usr/local/bin/bro`). Verify 33's sponsor
  check (SC021) fails builds: `-p:Verify_SponsorshipExemption=OpenSource -p:Verify_SponsorshipExemptionUntil=2027-06` (env vars for
  bro). MessagePack: dnceng feed removed, global.json lowered; tests net9.0 with `DOTNET_ROLL_FORWARD=Major`. charls: build the
  .slnx by name (its .sln.DotSettings confuses a bare `dotnet build`). A worker restart killed a background pipeline: rerun what
  its log doesn't show as finished.

## Trial 45 (2026-10-22, not clean: owner "Not clean", tally 0)

- Repos: dailydevops/guard (NetEvolve.Defaults 2.0.0, CSharpier, Sonar, TUnit; lens 1), sebastienros/parlot (xunit.v3 4.0.2,
  in-solution generator, net472-net10.0; lenses 2 and 4), martincostello/lambda-test-server (StyleCop 1.3.151-beta, migrate;
  lens 3), microsoft/FASTER (random, seed 45: random.json has 27 entries, index 8 ScottPlot was too large, 1,079 files). parlot:
  preview, format and check printed the same verdicts on a fresh clone and after a restore (format exit 3 for its generator
  projects in Release, check exit 0: by design); lambda-test-server changed nothing; FASTER converged in one run (260 kept
  findings baselined in cs/stylebro.baseline). TWAE builds and tests matched the untouched code everywhere.
- guard (owner: "Targets name the file"): the committed root .editorconfig is an OLDER version of NetEvolve.Defaults'
  template (the build rewrites it, Renovate bumps the package, nobody commits the result), so trial 44's exact-copy check
  missed it: preview/format/check said Clean, the first TWAE build had 214 BRO findings. `Migration.PackageCopy` also counts
  a package `*editorconfig` file whose name one of the package's build .props/.targets names and that file copies files
  (`BuildFilesName`, `CopiesFiles`: a `<Copy` task or `cp`/`copy`/`Copy-Item`; Meziantou.Analyzer names its configs only as
  `GlobalAnalyzerConfigFiles`, which the first version took for a copy: Mapperly's real-world check failed). The check needs restore output; preview and format run it after their restore.
- `SetPreset` on `<Project></Project>` (one line) wrote `</PropertyGroup></Project>`: CSharpier's check (CSharpier.MSBuild
  formats .props too) said Not clean right after a clean format. The closing tag now gets its own line.
- Environment: SDK 10.0.113 + 8.0.132 and pwsh were there (`ln -s ~/.dotnet/tools/bro /usr/local/bin/bro`). guard, parlot and
  lambda-test-server `<clear/>` their sources: the trial feed + a `StyleBro.*` mapping go into their nuget.configs. guard:
  `-p:NuGetAudit=false`, CS9057/CS0618 not as errors for the untouched comparison (CS9057 stops compilation, so check --build
  is honestly Not verified there), a net9.0 apphost MSB3030 untouched; its tests are TUnit executables (net8.0/net10.0).
  lambda-test-server: global.json 10.0.402 lowered, outputs in artifacts/bin. FASTER: `-p:NuGetAudit=false`, net7.0 tests with
  `DOTNET_ROLL_FORWARD=Major`. The stop hook flags trial clones' commits and dirty trees (third-party repos): never push them.

## Trial 46 (2026-10-23, clean: owner "Count it clean", tally 1 in a row)

- First trial of the variant "three random plus one targeted" (owner, 2026-10-23: "Mostly random"; `.trials/README.md`). Seed 46
  drew 2, 12, 1 from random.json's 27 entries, all used or needing SDK 11 (FastEndpoints, Verify), so five candidates were appended
  (entries 28-32) and the draws walked into them: altmann/FluentResults, jamesmh/coravel, sqlkata/querybuilder (MiniExcel: net11.0).
  Targeted: dailydevops/codebuilder (NetEvolve.Defaults 2.10.0, committed root .editorconfig an exact copy of its template,
  CSharpier.MSBuild, Sonar, TUnit). The three random ones converged in one run, preview = format file by file, check and check
  --build Clean, TWAE builds and tests as untouched. coravel: init turns IDE0055 off (mixed cast spacing), so a later plain
  `dotnet format --verify-no-changes` reports WHITESPACE that `bro format` skips by design.
- codebuilder (owner: "Builds prove it"): with `<DisableSupportAdditionalFiles>true</DisableSupportAdditionalFiles>` (the package's
  switch) init still warned and format/check/check --build said Not verified, though the builds kept init's block and were clean.
  `FormatCommand.CopyOffVerdict`: check --build holds the gap lines while a `PackageCopy` is named, and when its builds are clean and
  left the root .editorconfig (with init's block, `CanProveCopyOff`) as it was, `Migration.RememberCopyOff` writes a record in
  %TEMP%/stylebro-copy-off (hash of the package, its file and every .props/.targets/.csproj/.rsp path + text: the preview's copy
  finds it, a changed property needs new builds); `PackageCopy` returns null then (`FindPackageCopy` is the raw search). Copying
  turned back on: Not verified / Not clean again (checked).
- Environment: SDK 10.0.113 + 8.0.132 and pwsh were there (`ln -s ~/.dotnet/tools/bro /usr/local/bin/bro`). codebuilder's
  nuget.config `<clear/>`s: the trial feed + a `StyleBro.*` mapping go in there; its tests run with `dotnet test --solution
  CodeBuilder.slnx` (MTP). FluentResults' net48 tests need mono, querybuilder's MySQL tests a server (both fail untouched). The
  worker restarted twice: background jobs died, check their logs before rerunning.

## Trial 47 (2026-10-23, not clean: owner "Not clean", tally 0; host soak: not clean, 0 clean soaks)

- Repos: vkhorikov/CSharpFunctionalExtensions, xoofx/zio, axuno/SmartFormat (random, seed 47: draws 22, 4, 27 all used;
  random.json had 32 entries, 4 appended as entries 33-36, picks at indices 31, 32, 33; `.trials/trial47/logs/picks.md`),
  dailydevops/fluentvalue (targeted: NetEvolve.Defaults 2.10.0, copying turned off with DisableSupportAdditionalFiles as init
  advises; check --build proved it and remembered it, later checks didn't name the package: trial 46's fix held).
- zio (false Clean): GenerateDocumentationFile only in Release, BRO1601 reports only with doc generation, and format/check ran
  Release only for `#if`/Compile differences. `FormatCommand.DocumentsByConfiguration` (GenerateDocumentationFile or
  DocumentationFile under a `$(Configuration)` condition) joins `CompileByConfiguration`; `CompileView.Documentation`
  (`-getProperty:GenerateDocumentationFile`) is part of the compile key. The Configurations line no longer says "'#if' code".
- CSharpFunctionalExtensions (second run): a tab-indented header line in an `indent_style = space` file; BRO1107/BRO1108,
  BRO1110 own_line and BRO1111 copied its tabs onto the new lines, which the whitespace pass rewrote next run (it leaves the
  tab-indented line itself). `Indentation.InStyle(whitespace, unit, tabWidth)`: tabs become the spaces they show as when the
  unit is spaces. Other fixes that build `leading whitespace + unit` (BRO1105, BRO1132/BRO1134, BRO1008) don't use it yet.
- `PreviewCommand.Delete` on a folder that was never made says nothing (zio's broken restore: format --json's report folder).
- Format host soak (owner, 2026-10-23: `--engine host` on a second clone of every repository; harness
  `scripts/format-speed/Compare-Format.ps1` + check --build with both): SmartFormat's net462 view also had the libraries'
  netstandard2.0 instances, because MSBuildWorkspace sends a project reference it can't match by output ("Found project
  reference without a matching metadata reference") to a project's first instance; the analyzers reported their unformatted
  copies (23 extra findings). `Views.Of` retargets a reference of the run's framework instances to the instance the run
  names. A first try filtered the formattable documents: wrong, dotnet-format picks one copy per file (the netstandard2.0
  one), so the libraries weren't checked in net462 at all (only the "files with findings" counts showed it), and it
  analyzes every project with a copy of a formattable file. After the fix all four identical (trees, verdicts, exit codes,
  output, --json, check --build). Owner: a host-only difference makes the soak not clean (0 clean soaks); the harness keeps
  comparing changesPerRule exactly although the default engine's counts vary by one between runs (fix order).
- Timings (default vs host, final round): CSharpFunctionalExtensions check 220/230 s, format 196/171 s, recheck 137/121 s;
  zio 207/125, 227/95, 47/34; SmartFormat 313/179, 272/127, 73/55; fluentvalue 68/46, 112/30, 25/19; check --build 5-15 % faster.
- Kept as designed: the first check --build that proves a package's copying off prints the IDE0049 "Not checked" note
  instead of "The builds above check IDE0049" (the package gap is still in the gap list there); the copy-off record is keyed by
  relative paths and contents, so an identical second clone finds it.
- Environment: SDK 10.0.113 + 8.0.132 and pwsh were there (`ln -s ~/.dotnet/tools/bro /usr/local/bin/bro`). zio's TWAE
  restore fails on NU1510 untouched (check --build honestly Not verified; compare with `-p:WarningsNotAsErrors=NU1510`),
  its Release build fails on CS1591 untouched; CSharpFunctionalExtensions' untouched TWAE build fails on 36 nullable errors;
  SmartFormat needs EnableWindowsTargeting (Demo is net8.0-windows), net462 tests need mono; fluentvalue: trial feed + a
  `StyleBro.*` mapping in its nuget.config, `WarningsNotAsErrors` CS9057 in its props (NetEvolve.Arguments' analyzer needs
  Roslyn 5.6; dropping a package makes NetEvolve.Defaults fail with NED0010), net9.0 tests with DOTNET_ROLL_FORWARD=Major.
  A CPM repository needs the trial reference in its Directory.Packages.props (zio's is in src/). Long reruns: start them with
  `setsid nohup` (a background command dies at its time limit); `pkill -f` matched its own shell again.
