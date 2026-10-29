# First-run trials

One section per trial (repositories, what broke, what was fixed, the owner's verdict and the clean-trial tally, environment notes). A new trial adds its `## Trial NN` section at the end of this file. Moved from CLAUDE.md on 2026-10-23.
Since 2026-10-25 the host is `bro format`'s default engine; until the next release each trial's soak still compares both
engines (`.trials/README.md`, "Engine soak").

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

## Trial 48 (2026-10-23, not clean: owner "Not clean", tally 0; host soak: clean, 1 clean soak)

- Repos (three random plus one targeted): mganss/HtmlSanitizer, HangfireIO/Cronos, benjamin-hodgson/Pidgin (random, seed 48:
  draws 35, 20, 8 of 36 entries; draw 20 walked to Cronos, draw 8 past the end of the list, so entries 37-40 were appended and it
  took Pidgin; `.trials/trial48/logs/picks.md`), xoofx/Tomlyn (targeted: a tab-indented file in an `indent_style = space`
  repository, an in-solution generator; no pool entry fitted). Tomlyn needed one run (trial 47's tab fix held).
- Pidgin (owner: "Read it"): its StyleCop settings are Benjamin.Pizza.BuildScripts' globalconfig, added through
  `GlobalAnalyzerConfigFiles` (SA1200, SA1101, SA1201-SA1204, SA1309 off). Migrate read only repository files, and the first
  format changed 115 StyleCop-clean files. `StyleCopSetup.PackageGlobalConfigs`/`ReadPackageGlobalConfigs` take the StyleCop
  projects' evaluated `GlobalAnalyzerConfigFiles` (`ProjectQuery` lists them) where the restore output exists (`Restored`:
  obj/ or artifacts/obj/<project>/), as a layer between the rulesets and the repository's global configs (strictest-wins would
  let a repository .globalconfig that doesn't set a rule bring StyleCop's default back); without it, a "Not restored" warning
  when the project references other packages. The preview copies the repository's restore output before its settings step
  (`CopiedRestoreFolders`, excluded from the patch) and passes the warning on. Pidgin: 115 -> 7 files.
- Pidgin (owner: "Name it, stay Not clean"): the package config also sets `dotnet_sort_system_directives_first = true`, and the
  SDK's style pass flips `using static Pidgin.Parser;` / `using static Pidgin.Parser<char>;` in 5 files every run, untouched and
  without StyleBro too (a 2-line repro doesn't do it). `FormatCommand.UndoneFiles`/`UndoneLine` name a file a run leaves as it
  was before the previous run; `RememberUndone`/`RecordedUndone` let `bro check` say the same; the preview compares its trees.
- Pidgin (owner: "Option, migrate sets it"): StyleCop 1.1.118's SA1629 skips enum members (probed; 1.2.0-beta.556 reports them):
  `stylebro_punctuation_check_enum_members` (BRO1603, default true), `false` written by migrate for 1.1.x. Left as is: IDE0055's
  range spacing (`i .. (i + 2)`, 1 file; StyleCop 1.1.118 predates ranges), BRO1104 on a target-typed `new()` (documented).
- Cronos: NuGet.config's `globalPackagesFolder = %USERPROFILE%/...` puts the packages inside the repository off Windows. The
  preview's copy took each package's files git doesn't ignore (`.nupkg.sha512`, `.nuspec`, not `*.nupkg`/`build/`), NuGet took them
  as installed, and the preview couldn't load net40/net45/net462/netstandard2.0 where format could; `AddIgnoredSettings` also put
  the analyzer packages' 52 `.editorconfig` files back (trial 43's force-add undid trial 31's exclude). `PreviewCommand.Copy` and
  `AddIgnoredSettings` leave untracked files of an extracted package behind (`InPackage`: a `.nupkg` or `.nupkg.metadata` above).
  Cronos' netstandard1.0 doesn't load in `dotnet format` (honest "Not verified", check --build too: no EnforceCodeStyleInBuild).
- Format host soak (owner: "Clean soak"): trees, exit codes, `Not checked` lines and --json identical on all four; only PR
  #287's load note (HtmlSanitizer, Cronos). Tomlyn's "Release build of Tomlyn: 983 findings" list comes in MSBuild's output order:
  `Compare-Format.ps1` now compares such lines as a set. Host vs default (s): check 47/71, 69/142, 130/162, 37/36; format 34/63,
  44/120, 93/146, 32/52; check --build 54/71, 131/136, 91/96, 82/88 (HtmlSanitizer, Cronos, Tomlyn, Pidgin).
- Preview matched format file by file on all four; TWAE Debug/Release builds and tests matched the untouched code (HtmlSanitizer
  402/402, Pidgin 149/149, Tomlyn 1040/1040, Cronos 1865/1877 with the same 12 DST failures).
- Environment: SDK 10.0.113 + 8.0.132 and pwsh were there (`ln -s ~/.dotnet/tools/bro /usr/local/bin/bro`). Pidgin: global.json
  10.0.201 lowered, `dotnet tool restore` (its docs project runs docfx); Cronos: trial feed in its NuGet.config (`<clear/>`) and
  `signatureValidationMode` accept; Tomlyn: `git submodule update --init` (its tests read ext/toml-test; a clone of the clone has
  no submodule files: 360 instead of 1,040 tests). GOTCHA: an environment variable set for a `bash -c` chain (`REUSE=-Reuse`)
  reaches every step; a wait loop's `pgrep -f soak.sh` matched other wait loops' command lines (deadlock); a stale log with the
  awaited marker fired a wait at once. Name logs per run and clear them first.

## Trial 49 (2026-10-23, not clean: owner "Not clean", tally 0; host soak: not clean, 0 clean soaks)

- Repos (three random plus one targeted): Cysharp/ConsoleAppFramework, datalust/superpower, mayuki/Cocona (random, seed 49:
  draws 4, 22, 26 of 40 entries; every walk passed only used or rejected entries and ended on the unused tail, entries 37-39;
  nothing appended; `.trials/trial49/logs/picks.md`), benjamin-hodgson/Sawmill (targeted at trial 48's migrate fix: StyleCop
  1.2.0-beta.556 whose settings come from Benjamin.Pizza.BuildScripts' globalconfig; no pool entry fitted). Sawmill: migrate
  read the package config, wrote no BRO1603 enum option (1.2), carried 24 suppressions over, and format changed nothing.
- Cocona (owner: "Both signals"): BRO1302 renamed `[Argument] string ArgName0` below `// ReSharper disable once
  InconsistentNaming`; Cocona binds parameter names (the command line's), and 5 tests failed on both frameworks.
  `NamingRules.IsDisabledByReSharper` (in `AllowsRename`: every naming rule) leaves names where ReSharper's InconsistentNaming
  is suppressed (`disable once` above the nearest declaration/statement/member, or `disable` up to `restore`); the renamer keeps
  a parameter with an attribute of its own (`KeptReason.ParameterHasAttribute`; caller-info, nullability, interop and
  JetBrains attributes don't count). After: 200 + 565 x2 tests pass, 5 kept findings baselined.
- superpower (owner: "Emit-step list"): the untouched TWAE build fails on `CSC : error CS8002` (strong-named benchmarks reference
  unsigned Sprache); check --build read the place-less error as "compiled nothing" (false Not verified). CS8002 comes from emit,
  after the analyzers (a finding planted there was reported): `FormatCommand.EmitErrors`/`StopsCompiling`. Now Clean.
- Cocona, check of the unformatted code: two kept findings were `kept` in one run and `warning` in the next (the copy's fixer
  records a finding where it is when its Fix All runs). `MarkKept`/`IsKeptLine` match by file, rule and message (`AsKept`).
- Format host soak (owner: "Not a clean soak"): Cocona's global.json selects SDK 8.0.132, and every host run crashed
  ("GetGenericInstantiation ... does not have an implementation"): SDK 8's dotnet-format ships System.Collections.Immutable 8.0,
  which the load context preferred over the framework's 10.0 that System.Reflection.Metadata binds to. `FormatHost.FromFramework`
  (the .NET host's rule: the framework's copy wins when it isn't older) got past MEF, then MSBuild failed: SDK 8's dotnet-format
  has no Microsoft.Build.dll (MSBuildLocator). `FormatHost.FolderProblem`: such an SDK falls back to dotnet format with an
  `Engine:` line. CAF, superpower, Sawmill identical on both engines (check --build's "Checked" lines differ only in order).
  Host vs default (s): check 102/202, 91/159, 28/27; format 74/173, 67/135, 26/26; recheck 26/36, 27/30, 26/26 (superpower,
  CAF, Sawmill); check --build 67/79, 83/85, 89/88.
- Preview matched format file by file on all four (Cocona again after the fixes). TWAE builds match the untouched code: superpower
  only CS8002, Cocona only CS0618 (a benchmark), CAF and Sawmill green; tests as untouched (superpower 283 x2, CAF 124, Sawmill 99).
  An incremental TWAE build right after check --build passed where a clean one fails (check --build's outputs): compare with
  `--no-incremental`.
- Kept as is: Cocona's `charset = utf-8` removes 44 files' BOMs (the repository's setting); init's summary names BRO1404 twice
  when two conventions turn it off (each with its reason).
- Environment: SDK 10.0.113 + 8.0.132 and pwsh were there (`ln -s ~/.dotnet/tools/bro /usr/local/bin/bro`); trial feed as a
  global NuGet source (no repository had a nuget.config). superpower and Sawmill: global.json 10.0.201 lowered; Sawmill:
  `dotnet tool restore` (docfx). GOTCHA: `git checkout -p` with piped answers reverted a second hunk of a fix (the host's
  load context) while undoing a hand-made mutation: the helper and its test were committed without the call, and the rerun
  crashed the same way. Undo hand mutations from a copy of the file, and test the call site, not only the helper.

## Trial 50 (2026-10-24, not clean: owner "Not clean", tally 0; host soak: not clean, 0 clean soaks)

- Repos (three random plus one targeted): commandlineparser/commandline, Cysharp/ObservableCollections,
  dahomey-technologies/Dahomey.Json (random, seed 50: draws 31, 17, 23 of 50 entries; every entry was used, so entries
  41-50 were appended first and all three walks ended on 41-43; `.trials/trial50/logs/picks.md`), ncalc/ncalc (targeted at
  trial 49's ReSharper fix: `// ReSharper disable once InconsistentNaming` on the public `IEEERemainder`; random.json entry
  47; SharpZipLib was withdrawn, used in trial 36). The ReSharper fix held.
- **Runner/fixer split** (owner: "Try it on trial 50", docs/decisions/2026-10-23-trial-50-tries-a-runnerfixer-split.md): one
  cloud session per repository ran it (both engines, builds, tests) and wrote `.trials/trial50/findings/<owner>--<name>.md`
  to the trial branch; the fixer reproduced each finding with a unit test or a two-project repro, without cloning the
  repositories, and asked the runners to re-run on its commits. What worked: the findings files carried a minimal repro and
  the cause (file and line) for every finding, so no finding had to be rediscovered; the runs went in parallel; a runner's
  re-check caught two of the fixer's own mistakes (additional files read from the outer evaluation only; then an
  over-split, net10.0 in 11 parts). What cost: every runner push to the branch cancelled the PR's CI (about 15 cancelled
  mutation runs); a fix late in the round made every runner start over (multi-targeted projects everywhere); a wrong
  commit id in one message; the fixer's host test assumed the SDK 10.0.1xx workspace bug, which CI's 10.0.402 doesn't have.
  Rediscovered beyond the files: the `Engine:` line `bro check` hid (found while testing the host fix).
- commandline: BRO1309 renamed `internal static int __Main(string[] args)` to `Main`; Microsoft.NET.Test.Sdk generates an
  entry point on .NET Core, so CS0017 and none of the 684 tests built. `NamingRules.AllowsRename`: no static method becomes
  `Main` (any static `Main` is an entry point candidate). After: 684/684.
- Dahomey.Json, ObservableCollections, commandline: `bro preview format --engine host` and `bro format --diff --engine ...`
  failed ("The preview failed": `--engine` reached `dotnet format`); `preview init --engine host` was "Unknown option".
  Program takes `--engine` for every preview, PreviewCommand too (`FormatCommand.CurrentEngine`). Dahomey.Json's re-check:
  `bro preview format Lib` with the reference in the root Directory.Build.props added Lib/Directory.Build.props, which would
  hide the root one (`PreviewCommand.ImportedFromAbove`).
- ncalc: Parlot.SourceGenerator makes `*.parlot.cs` AdditionalFiles in NCalc.Parser (inner evaluations only) and
  NCalc.Benchmarks compiles one; MSBuildWorkspace (SDK 10.0.1xx; 10.0.402 opens it) crashes opening any solution or .slnf
  with both ("The solution does not contain the specified document"), a project file opens. Owner's choices
  (docs/decisions/2026-10-24-*): such projects run apart (`Separate`, `LoadsAdditionalAsCompile`: only another project's
  additional file counts, a multi-targeted project's outer evaluation lists its own as Compile), a project whose references
  load both runs on its project file, the "Shared files:" line says why; the host falls back to `dotnet format` when its
  workspace doesn't load (`Entry.LoadFailed`, an `Engine:` line, shown in a check too: `IsHeaderLine`). `bro check` said
  "Not verified ... the rest is clean" when the crashed run came after runs with findings: `FormatCommand.Worse`. After: 6 runs
  (16 before the over-split fix), 61 files, Benchmarks' 28 TWAE errors gone, 901/901, Not verified for net462 only.
- Format host soak (owner: "Not a clean soak"): commandline, ObservableCollections, Dahomey.Json identical on both engines
  (trees, verdicts, exit codes, --json; preview patches byte-identical); format 47/82 s, 54/125 s, 53/104 s (host/default).
  ncalc: every host run failed before the fallback, identical trees after.
- Environment: SDK 10.0.113 + 8.0.132 and pwsh were there. Runners: `NUGET_CERT_REVOCATION_MODE=offline`,
  `DOTNET_ROLL_FORWARD=Major` (netcoreapp3.1/net5.0 tests), `-p:NuGetAudit=false`, `-p:EnableWindowsTargeting=true`
  (ObservableCollections' WPF sample); ncalc needs a full clone (Nerdbank.GitVersioning) and global.json lowered to 10.0.100.

## Trial 51 (2026-10-24, not clean: owner "Not clean", tally 0; host soak: clean, 1 clean soak)

- Repos (three random plus one targeted): JasperFx/oakton, mgravell/Pipelines.Sockets.Unofficial, sebastienros/esprima-dotnet
  (random, seed 51: draws 15, 32, 35 of 50 entries; every walk passed used entries and ended on entries 45, 46, 48; nothing
  appended), StephenCleary/AsyncEx (targeted at trial 50's subfolder fix: src/ and test/Directory.Build.props, none at the
  root; no pool entry fitted). Findings files: `.trials/trial51/findings/` (committed this time).
- **One session again** (owner's choice after trial 50's split): the repositories ran in parallel as background jobs, each
  with a findings file written before any fix, and one re-check of all four on the final commit. Wall clock: 08:04 start,
  PR at 10:31 (2.5 h; trial 49: 6.9 h, trial 50: 8.5 h), verdict asked at 16:07; the session was archived at 11:38 in the
  middle of the re-check and its container restarted, which lost the gitignored `.trials/trial51/` (findings files rebuilt
  from the session's record, two repositories re-checked again). About 4.7 h of work in all.
- Pipelines.Sockets.Unofficial (net462-net10.0): the net472 run kept Toys (net8.0;net462;net10.0) because it references the
  library, and forced it onto net472, which it doesn't target ("reference assemblies for .NETFramework,Version=v4.7.2 were not
  found"): the net472 tests didn't load, a false "Not verified" (the host was right). `SelectFrameworkTargets` clears the
  framework for `StyleBroFormatOther` projects too.
- AsyncEx (netstandard1.3;netstandard2.0;net461 libraries that reference each other, netcoreapp3.1 tests): a run's solution
  filter listed only its own projects; the libraries loaded as references and MSBuildWorkspace wired their references to
  each other's netstandard1.3 instance (no mscorlib), so `dotnet format` skipped 4 of 5 test projects ("Required references
  did not load"), format said Not verified, and the TWAE builds that were green untouched failed on 3 findings left there.
  `FormatCommand.FilterProjects`: a run's filter also lists the solution projects its projects reference (`--include` keeps
  the run to its own files).
- esprima-dotnet (`* text=auto`, two files committed with CRLF): the preview's copy repository stored them as LF, so init
  counted them as converted by git (no `end_of_line = crlf` for the file whose multi-line strings would change) and the
  patch made them LF; applied, `ToJavaScriptTest2` failed. The copy marks the source's CRLF-committed files `-text`
  (`PreviewCommand.CommittedWithCrLf`).
- AsyncEx (the lens): `bro preview format src/Nito.AsyncEx.Tasks/Nito.AsyncEx.Tasks.csproj` created a Directory.Build.props in
  the project's folder that hid src/Directory.Build.props (TWAE, docs, FxCop). `AddPackage` takes the props files (and the
  Directory.Packages.props lookup) from the copy's top for the projects under the previewed root; trial 50's test expected
  the new file and now expects the one above.
- oakton: no findings (check --build honestly Not verified: the untouched TWAE build fails on 96 CS8632).
- Format host soak (owner: "Clean soak"): oakton and esprima-dotnet identical in both rounds; Pipelines and AsyncEx differed
  in the first round only through the default engine's two bugs above (the host's trees and verdicts were the right ones).
  After the fixes all four identical (trees, exit codes, verdicts, --json); left in the logs: PR #287's load note (on
  Pipelines in another framework's run per engine) and Pipelines' net472 "files with findings" count (51/52: the engines
  order the runs differently, and a run also formats what an earlier run of the round changed). Host vs default, re-check
  on a quieter machine (s): format 116/327, 136/472, 72/327, 134/260; check 61/87, 61/100, 51/103, 83/82 (Pipelines,
  esprima, AsyncEx, oakton).
- Final re-check: TWAE builds and tests match the untouched code on all four (oakton 254/255 x 4 TFMs, Pipelines 287/292 with
  the flaky `BufferWriterDoesNotLeak`, esprima 7,880 + 2, AsyncEx 266); preview matches format file by file.
- Environment: SDK 10.0.113 + 8.0.132 and pwsh were there (`ln -s ~/.dotnet/tools/bro /usr/local/bin/bro`). Pipelines:
  `Directory.build.props` (lower-case b) isn't imported on Linux, renamed locally; its NuGet.config `<clear/>`s, so the trial
  package must be in the global packages folder or its feed in that file (a fresh container's first preview failed NU1102,
  with the right message). esprima: Test262 downloads its suite before building (fails the same before and after).
  GOTCHA: `pkill -f` with a pattern from its own command line killed the calling shell (as CLAUDE.md warns); a long loop of
  one `pwsh` per mutation entry took over 10 minutes (check all entries in one pwsh process).

## Trial 52 (2026-10-24, not clean: owner "Not clean", tally 0; host soak: clean, 1 clean soak on the new host code)

- Repos (three random plus one targeted): amantinband/error-or, DbUp/DbUp, drewnoakes/figgle (random, seed 52: draws 17, 3,
  46 of 50 entries; only entry 49, Tyrrrz/Onova, was unused and has Windows-only tests, so entries 51-55 were appended
  first (error-or, DbUp, figgle, EasyNetQ, Sharprompt; smoke-built and tested) and the walks ended on 51-53;
  `.trials/trial52/logs/picks.md`), EasyNetQ/EasyNetQ (targeted at trial 51's fixes: five test projects, Source/ props
  with none at the root, a multi-targeted library; entry 54 taken from the appended ones, no pool entry builds).
  Findings files: `.trials/trial52/findings/`.
- One session, the repositories in parallel (two chains), each with a findings file before any fix, then a re-check of
  all four on the fixed build. Wall clock: 16:26 start (after waiting for #297 to merge, from about 16:05), first round
  16:45, re-check 17:48, soak round 18:48, verdict asked at 19:50 (about 3.5 h; two worker restarts on the way).
- DbUp: `src/dbup.1.2.3.nupkg` is committed next to `src/DbUp.sln` and `src/Directory.Build.props` (none at the root).
  `StyleCopSetup.EnumerateFiles` skipped every folder with a .nupkg as an extracted package (trial 31), so init read no code
  ("too little code") and pinned the preset in a new root Directory.Build.props nothing imports, the preview put the
  reference there and failed ("analyzers aren't loaded"), format changed nothing and `bro check` said "Clean" while the
  TWAE build failed on 2 BRO1001 and the tests didn't build (`check --build` was honest, exit 2). A folder with a project,
  solution or Directory.Build.* file isn't an extracted package (`StyleCopSetup.IsExtractedPackage`, also the preview
  copy's `InPackage`). After: init reads src/ and src/.editorconfig, 42 files, all green, 100/100.
- error-or (StyleCop, migrate): its `[*] end_of_line = crlf` with 36 CRLF and 12 LF C# files; the migration (and init) turns
  IDE0055 on, and format rewrote the 12 LF files (about 1,000 of 1,800 changed lines) without a word. Owner: "Warn only"
  (docs/decisions/2026-10-24-trial-52-a-minority-of-files-against-the-repositorys.md): `Conventions.ConvertedMinorityNote`.
- figgle, EasyNetQ: no findings; trial 51's lenses held (all test projects loaded per run, the preview's reference in the
  props the projects import).
- Format host soak (owner: "Clean soak"): first round and re-check on main 8247120: engines identical on all four. #298 and
  #299 merged meanwhile (the count restarted), so a third round ran on a local merge of main cb47c2d into the branch
  (0.8.0-trial52.3): identical again (trees, exit codes, --json, logs). Format (s, default/host): 68/27, 49/36, 90/50,
  148/61 (error-or, DbUp, figgle, EasyNetQ).
- Environment: SDK 10.0.113 + 8.0.132 and pwsh were there; the trial feed went into the user-level NuGet.Config (no repo
  `<clear/>`s). error-or's net462 tests need Mono (fail before and after); EasyNetQ's IntegrationTests need RabbitMQ (left
  out) and Hosepipe.Tests fail 8-12 of 17 untouched. GOTCHA: a mutation script using `[IO.File]` with relative paths
  mutated the main checkout, not the worktree (.NET's current directory isn't PowerShell's location): join with the root.

## Trial 53 (2026-10-24, not clean: owner "Not clean", tally 0; host soak: clean, 2 clean soaks on the new host code)

- Repos (three random plus one targeted): shibayan/Sharprompt, AArnott/Nerdbank.Streams,
  autofac/Autofac.Extensions.DependencyInjection (random, seed 53: draws 39, 54, 13 of 55 entries; only entry 55, Sharprompt,
  was unused, so entries 56-60 were appended first (Nerdbank.Streams, Autofac.Extensions.DependencyInjection, Sleet,
  Finbuckle.MultiTenant, Flurl; smoke-built and tested) and the walks ended on 55-57; `.trials/trial53/logs/picks.md`),
  loic-sharma/BaGet (targeted at trial 52's .nupkg fix: `tests/BaGet.Tests/TestData/` commits TestData.1.2.3.nupkg next to C#
  code, no project there; no pool entry had either lens, added). Tested main 241bca8 (after #300). Findings files:
  `.trials/trial53/findings/`.
- One session, the repositories in two parallel chains (base, default, host on fresh clones each). Wall clock: 20:13 start,
  waiting for #300 until 20:38, runs 20:40-22:00, verdict asked at 22:40 (about 2.5 h).
- BaGet: `charset = utf-8` with 16 of 248 C# files committed with a BOM; init turns IDE0055 on and format stripped the 16
  BOMs without a word (trial 52's minority note covered only end_of_line). Owner: "Warn only"
  (docs/decisions/2026-10-24-trial-53-a-minority-of-files-against-the-repositorys.md): `Conventions.ConvertedMinorityFileNote`
  for charset and insert_final_newline, in init and migrate (with IDE0055 on).
- BaGet's lens: `IsExtractedPackage` still took TestData/ (a .nupkg and no project) for a package. No effect on BaGet (its
  file there is a generated .Designer.cs), but a spot repro with ordinary code there: format fixed the file and said "Run 1:
  0 files changed, clean" (its snapshot never saw the folder; init and the preview's copy skipped it too). An extracted
  package's .nupkg is named after its folder (packages.config, also -ExcludeVersion) or its id/version folders (a global
  packages folder); `.nupkg.metadata` still counts.
- Sharprompt, Nerdbank.Streams (StyleCop 1.3 as a GlobalPackageReference, submodule compiled in), Autofac (StyleCop + Sonar
  rulesets, 8 solution configurations): no findings; Autofac's `end_of_line = crlf` is for `*.sln` only (not the lens).
- Format host soak (owner: "Clean soak"): all four identical on both engines (trees, exit codes, --json, builds, tests);
  logs differ only in run order and PR #287's load note. Format (s, default/host): 63/57, 152/78, 58/38, 108/67 (BaGet,
  Sharprompt, Autofac, Nerdbank.Streams).
- Environment: SDK 10.0.113 + 8.0.132 and pwsh were there; the trial feed in the user-level NuGet.Config and in BaGet's
  nuget.config (`<clear/>`); global.json lowered in Nerdbank.Streams (10.0.402), Autofac (10.0.203); BaGet netcoreapp3.1 needs
  `DOTNET_ROLL_FORWARD=Major` and fails 47 + 2 tests untouched. Autofac's Bench project runs real benchmarks under
  `dotnet test -c Release` (hung the first baseline; tests limited to test/). One worker restart near the end (nothing lost:
  findings were committed). GOTCHA again: `pkill -f` with a pattern in the same command line killed the shell.

## Trial 54 (2026-10-25, not clean: owner "Not clean", tally 0; host soak: clean, 3 clean soaks on the new host code)

- Repos (three random plus one targeted): Finbuckle/Finbuckle.MultiTenant, emgarten/Sleet, tmenier/Flurl (random, seed 54:
  draws 58, 8, 28 of 60 entries; the walks ended on 58, 57, 59, the last unused entries, so entries 61-64 were appended for
  trial 55: ZLinq, Newtonsoft.Json.Schema, FeatureManagement-Dotnet, FusionCache, smoke-built and tested;
  `.trials/trial54/logs/picks.md`), StackExchange/Dapper (targeted at trial 53's charset/final-newline notes: `charset =
  utf-8-bom` with 1 of 157 C# files without a BOM, `insert_final_newline = true` with 2 without; no pool entry had either
  lens, added). Tested main dc107c4 (after #301). Findings files: `.trials/trial54/findings/`.
- One session, the repositories in two parallel chains (base, default, host on fresh clones each). Wall clock: 23:48 start
  (#301 had merged), first round done 00:51, fixes and re-checks until 02:00, verdict asked then; one worker restart (nothing
  lost: findings were committed), the owner's answers came through the parent session at 06:48.
- Dapper: in the net472, net481 and net5.0 runs (Dapper.Tests.Performance, Dapper.Tests, Dapper.Rainbow) the referenced Dapper
  library (net461;netstandard2.0;net8.0;net10.0) didn't load: NuGet's `nuget.g.props` saw the environment's framework (its
  per-framework ImportGroups matched none) and `nuget.g.targets` the framework `SelectFrameworkTargets` had cleared (the outer
  build's `buildMultiTargeting/ReferenceTrimmer.targets`, whose task assembly the unimported props define). Format said
  "Not verified" and check --build "Not clean" (16 findings in the skipped net472 code), both honest. The host engine, which
  sets no framework in the environment, loaded everything. Fixed: `SelectFrameworkProps`, the same condition through
  `CustomBeforeDirectoryBuildProps` (before Directory.Build.props and NuGet's props). After it Dapper is clean on both
  engines, byte for byte; Sleet and Flurl re-run on the fix gave the same trees.
- Dapper: "DataClasses.designer.cs is generated and checked in: rebuild and commit it" for a LINQ to SQL designer file (next
  to its .dbml). A `.designer.cs` next to a `.dbml`, `.xsd` or `.edmx` is designer-written now. Trial 53's notes fired as
  intended (1 BOM and 2 final line breaks announced, and exactly those changed).
- Finbuckle (and Dapper): init's summary said "Turned 13 rules off" with BRO1306 twice (non-private fields, private
  constants). One entry per rule now. Sleet and Flurl: no findings (Flurl: 146 kept findings, 139 BRO1603 in doc texts its
  code generator holds as strings, listed and baselined as designed).
- Format host soak (owner: "Clean soak"): Finbuckle, Sleet, Flurl identical (trees, exit codes, --json); logs differ in run
  order and PR #287's load note. Dapper differed only through the default engine's load bug above (the host was right);
  identical on the fix. Format (s, default/host): 96/89, 138/96, 117/49, 429/109 (Finbuckle, Sleet, Flurl, Dapper).
- Environment: SDK 10.0.113 + 8.0.132 and pwsh were there; the trial feed in the user-level NuGet.Config and in the repos'
  `NuGet.Config` (`<clear/>`; prep.sh now finds the file case-insensitively and adds the feed to the first `<clear />` only);
  Sleet's global.json lowered. Dapper needs a full clone (Nerdbank.GitVersioning); its TWAE builds fail untouched on 5 RT0000
  (ReferenceTrimmer wants doc files) and most tests need database servers (Sqlite/DuckDB ones run). A trial clone's `origin`
  is a local path, which SourceLink reports in `--verbosity diagnostic` logs (harmless).

## Trial 55 (2026-10-25, not clean: owner "Not clean", tally 0; host soak: not clean, the first with the host as the default)

- Repos (three random plus one targeted): Cysharp/ZLinq, JamesNK/Newtonsoft.Json.Schema, microsoft/FeatureManagement-Dotnet
  (random, seed 55: draws 11, 25, 19 of 64 entries walked to 60-62, appended by trial 54; only entry 63, FusionCache, is
  unused now: trial 56 appends first; `.trials/trial55/logs/picks.md`), protobuf-net/protobuf-net.Grpc (targeted at trial 54's
  load fix: net472 tests referencing net8.0/net10.0-only libraries, Nerdbank.GitVersioning in every project; no pool entry
  fit, about 25 candidates scanned with `scanmt.py`, added). Tested main 09d42be (after #303). Findings files:
  `.trials/trial55/findings/`.
- One session, two parallel chains (base, default = host, `--engine dotnet-format` on fresh clones, then
  `Compare-Format.ps1`). Wall clock: 10:37 start, runs 10:41-14:57, investigation until 15:45, verdict asked then.
- Newtonsoft.Json.Schema keeps its only solution in Src/: the preview's patch had Src/stylebro.baseline, `bro format` on the
  root wrote stylebro.baseline (and `format Src/x.slnx` Src/). `Repeat` passed the folder it shows paths from as the new
  baseline's folder; trial 28's `NewBaselineFolder` decides now. The root file worked for the build; only the preview's
  promise differed.
- ZLinq: the preview listed "Indentation kept: src/ZLinq/Linq/Sum.cs" twice (21,26,32,142 and 21,26,32-33,36,159): each preview
  format pass is its own process, the second numbered the fixed text. The preview keeps the first note for the same files.
- protobuf-net.Grpc: "Generated/Reflection.cs is generated and checked in: rebuild and commit it" for a hand-run protogen
  file, because `protobuf-net.Grpc.Reflection.csproj` in a ProjectReference contains its name; a build step must name it whole.
  Trial 54's fix held: the net472 runs loaded every project on both engines.
- FeatureManagement-Dotnet: no findings; trial 53's final-newline note fired for exactly its one file; check --build said
  "Not verified" (exit 3) for builds that fail untouched on NU1510, honest. The TWAE builds fail untouched on all four but ZLinq
  (NU1510/ASPDEPR008, RS0016) and matched after the fixes; protobuf-net.Grpc's DuplexEcho tests are flaky untouched.
- Host soak (owner: "Not clean"; follow-up PR, backlog 076, docs/decisions/2026-10-25-trial-55-...): trees, exit codes,
  verdicts, builds and tests identical on all four, no fallback to dotnet format. Two output differences: on unformatted code
  the host lists 28 findings dotnet format doesn't on protobuf-net.Grpc (IDE0055 beside WHITESPACE, BRO findings on lines an
  earlier fix changes: dotnet format analyzes again after each fix), so `changesPerRule.IDE0055` 54/31 and the preview's
  "reported changes" 407/384; on ZLinq the old engine loads a referenced project without the run's framework with all its
  frameworks (`StyleBroFormatLoose`) and lists 2 more files with findings in three runs. #303's Serilog follow-up didn't
  reproduce (the host lists Newtonsoft.Json.Schema's load warning at `--verbosity diagnostic`). Format (s, host/dotnet-format):
  203/834, 684/1308, 92/230, 235/790 (Json.Schema, ZLinq, FeatureManagement, Grpc), two chains on 4 cores.
- Environment: SDK 10.0.113 + 8.0.132 and pwsh were there; the trial feed in the user-level NuGet.Config and after the first
  `<clear />` outside a comment in each repo's nuget.config (protobuf-net.Grpc has `<clear/>` inside a comment: prep's sed put
  the feed in it, `addfeed.py` now); global.json files with an `sdk` version lowered (not a test resource named global.json);
  CPM repos get step 1 as PackageVersion + PackageReference; `EnableWindowsTargeting=true` for protobuf-net.Grpc; ZLinq is TWAE
  under CI=true. Running chains rewrite their tracked logs, so the stop hook kept asking to commit: in-progress logs stay
  untracked until their chain ends. One worker restart (nothing lost).

## Trial 56 (2026-10-26, not clean: owner "Not clean", tally 0; host soak: clean, the first clean soak with the host as the default)

- Repos (three random plus one targeted): robinrodricks/FluentFTP, ZiggyCreatures/FusionCache,
  andrewlock/NetEscapades.EnumGenerators (random, seed 56: draws 71, 1, 60 of 79 entries; draw 71, NickCraver/StackExchange.Exceptional,
  skipped: its solution has an ASP.NET MVC5 project (MSBuild.SDK.SystemWeb) and net462-net48 targets, a host repository; walks to 72,
  63, 64; `.trials/trial56/logs/picks.md`), reactiveui/DynamicData (targeted at trial 55's baseline location fix: the only solution is
  `src/DynamicData.sln`, StyleCop through GlobalPackageReference, so migrate; no pool entry fit, added). #306's two-path view came with
  FusionCache (net10.0 projects reach the core, which has net10.0, through netstandard2.0-only plugins); no repository had a generated
  file named inside a longer project name (`scan2.py`). Tested main 61b1a6e (after #306). Findings: `.trials/trial56/*/logs/`.
- One session, two chains (base, default = host, `--engine dotnet-format` on fresh clones, then `Compare-Format.ps1`), builds
  0.8.0-trial56.1, .2 and .3 (the fixes). Wall clock: 02:00 start; the container was reclaimed after 08:24 (files kept, runs lost) and
  the unfinished steps ran again 20:05-22:31.
- FusionCache: the engines' trees differed in 8 test files. The untouched code has `, token: TestContext.Current.CancellationToken);`
  at column 0; when BRO1108 split the list before BRO1117 moved the comma up, BRO1117 gave the item the comma's empty indentation, so
  the result depended on fix order (in round 1's soak both engines happened to leave column 0; FixOrderTests reproduced it). An item on
  a line below its comma keeps its own indentation now; on 0.8.0-trial56.3 both engines indent it, identical trees, soak Same.
- DynamicData: `bro check` and `check --build` stopped at their first project query (MSB4186) once format's builds had left
  DynamicData.Tests' restore and build output: `src/Directory.Build.targets` sets DependentUpon with `Regex.Replace(%(Filename), ...)`,
  and the outer evaluation without a target got `.NETCoreApp,Version=v9.0.AssemblyAttributes`, whose comma split the arguments (a
  build evaluates fine). Any user who had built the repository would have hit it in format too. The query is retried with the empty
  target, as trial 42 did for MSB4184; then check said Clean and check --build Not verified (exit 3: 4,870 TWAE errors the untouched code
  has keep the tests from compiling), honest. Migrate: 22 files, 54 CRLF files converted as announced; preview matched format.
- EnumGenerators: the trial's own setup first put the reference only in the root Directory.Build.props; build/ and the Benchmarks folder
  have their own that don't import it (rerun as the docs say). A spot run of that half setup: format, check and check --build name the
  Benchmarks project "without StyleBro"; init doesn't (its EnforceCodeStyleInBuild tip even names that props file), left as is.
  Format Not verified for six Release configurations of in-solution generator projects, check builds them: Clean. 6 kept BRO1306
  (`_enum`) baselined.
- FluentFTP: no findings beyond format's changes (404 files, clean after one run); check --build Not verified (its untouched CS0067,
  CS8425, CA2022 under TWAE stop four projects compiling), honest. The TWAE builds failed untouched on FluentFTP and DynamicData and
  matched after the fixes; the tests matched (FluentFTP's 3 timeout tests need a network; FusionCache's and DynamicData's
  concurrency tests are flaky untouched; one DynamicData Release run hung in a deadlock-probe test and was killed, its rerun had that
  test fail on its timeout, and it passed 3 of 3 alone on both the untouched and the formatted code).
- Host soak: no fallback to dotnet format. EnumGenerators' load-warning note sat after another run's header (the host prints it when its
  load ends; host runs differ among themselves): `Compare-Format.ps1` compares that line as unordered now; both engines listed the
  same warnings (dotnet format one more). FusionCache's tree difference was the fix-order bug above (not an engine difference). After the
  fixes all four identical on both engines (trees, exit codes, verdicts, --json, builds, tests). Format (s, host/dotnet-format, trial
  runs): EnumGenerators 121/423, FluentFTP 202/648, FusionCache 99/185, DynamicData 263/527; soak format 112/368, 123/452, 105/180,
  135/435.
- Environment: SDK 10.0.113 + 8.0.132 and pwsh were there; FusionCache's tests use MTP (`dotnet test --solution`); DynamicData needs a
  full clone (Nerdbank.GitVersioning) and DOTNET_ROLL_FORWARD=Major (net9.0 tests); a new build of the tool went to its own tool folder
  so running chains kept theirs; `pkill -f` with the pattern in the same command killed the calling shell (CLAUDE.md's gotcha).

## Trial 57 (2026-10-27, not clean: owner "Not clean", tally 0; host soak: clean, the second clean soak with the host as the default)

- Repos (three random plus one targeted): JamesNK/Newtonsoft.Json.Bson, microsoft/OpenAPI.NET, sergeyteplyakov/ErrorProne.NET
  (random, seed 57: draws 5, 47, 75 of 79 entries; 5 and 47 walked past the used entries and dotNext (1,160 files),
  fluentmigrator (1,248 files, net48, databases) and PropertyChanged (net48 everywhere) to 68 and 69; only entries 70, 73, 74,
  76-78 are unused now; `.trials/trial57/logs/picks.md`), DapperLib/DapperAOT (targeted at trial 56's MSB4186 retry:
  `$([System.String]::Copy(%(Filename)).Replace(...))` in its test project; ~50 repositories scanned with `scan.py`, leading
  commas in code are rare: Mapster (trial 11) was the only fit; added to the pool). Tested main 9e5b542 (after #308). Logs:
  `.trials/trial57/<repo>/logs/` (round 1 `default`/`dotnet` on 0.8.0-trial57.1, round 2 `default2`/`dotnet2` on .2).
- One session, two chains (base, default = host, `--engine dotnet-format` on fresh clones, then `Compare-Format.ps1`). Wall
  clock: 05:40 start (waited for #308 until 06:32), runs 06:35-08:28, fixes, round 2 09:16-11:07.
- DapperAOT: one test failed after format (CS9052 in generated code): BRO1404 added `internal` to an embedded-resource
  template's `#if !DAPPERAOT_INTERNAL` / `file` / `#endif` / `static class DbStringHelpers`, which its tests compile with the
  symbol defined. BRO1404 skips modifiers that depend on `#if` now.
- DapperAOT: the preview listed 164 files, format then changed 186. The preview's copy had a git history of its own two
  commits, so Nerdbank.GitVersioning's `versionHeightOffset: -4` gave a negative build number, the design-time build of
  Dapper.AOT.Analyzers stopped before the compiler's command line, and the project loaded without its settings: no BRO
  findings there, and only the load-warning note said anything. A depth-1 clone (CI's checkout) did the same: `format` then
  `check` said "Clean." with 22 files of findings left. Owner's decisions: such a project is `Not checked` with MSBuild's error
  (a design-time probe for the runs whose load warned; SourceLink's "has no remote" fails no target and isn't one), and the
  preview's copy borrows the repository's objects, HEAD, remotes and shallow boundary. Round 2: previews matched format on
  all four; the shallow clone says Not verified (exit 3) with the same 10 gaps on both engines.
- DapperAOT: `check --build` after the trial's builds said "Not clean" (7 CS0234 in Dapper.AOT.Test.Integration), the next run
  "Not verified": after a design-time load MSBuild compiled that project without the reference that failed (RS1032, untouched
  too), where the HEAD copy's build never compiled it. Owner: such errors are `Not compared` and Not verified. Trial 56's
  lens held: `check` before format, after format and after the builds ran without MSB4186.
- Newtonsoft.Json.Bson: netstandard1.3 doesn't load in `dotnet format` (Not verified by design, as trials 31/43/47); `check
  --build` printed that verdict before six clean builds, with a hint to run `check --build`, and ended on a build's line.
  Owner: the verdict comes after the builds now and says what they checked.
- OpenAPI.NET: 400 files, clean after one run, 2 kept findings baselined (a public parameter named like a type an unloaded
  file uses, kept by design). ErrorProne.NET: 98 files; TWAE fails untouched on NU1603, check --build Not verified, honest.
  The TWAE builds failed untouched on ErrorProne.NET and DapperAOT and matched after the fixes; every test suite matched
  the untouched code on both rounds (round 1 DapperAOT: the CS9052 test).
- Host soak: no fallback to dotnet format; trees, exit codes, verdicts, --json, builds and tests identical on all four in both
  rounds, Compare-Format Same eight times. DapperAOT round 2's check --build named Dapper.AOT.Test.Integration as not compared
  in the host clone only: MSBuild's state after the earlier loads, as in round 1 on both engines, same verdict. Format (s,
  host/dotnet-format, trial runs r1; r2): Bson 42/118; 51/160, OpenAPI.NET 151/283; 149/360, ErrorProne.NET 38/58; 39/70,
  DapperAOT 147/764; 160/727. Soak format r2: 41/122, 133/332, 35/59, 115/569.
- Environment: SDK 10.0.113 + 8.0.132 and pwsh were there; blocked feeds (myget for Json.Bson, dnceng dotnet-tools for
  ErrorProne.NET) removed in the clones; global.json files lowered; DapperAOT needs a full clone and a `StyleBro.*`
  packageSourceMapping for the trial feed (`addfeed.py`); `dotnet test` reads global.json from the current folder (run it in
  the repository); running the whole of a third-party test suite was refused once by the auto-mode check (DapperAOT, whose
  Integration tests need Docker anyway): its CI's unit-test step instead. A watcher with `pgrep -f` matched itself again.

## Fixture matrix (2026-10-27, owner: "fixture matrix first, then trials")

- Why: every trial since 46 was not clean, about half of each trial's repositories needed a fix, and each fix became a one-off
  unit test. `scripts/fixtures/` (README there) covers each recurring CLASS of repository shape end to end: tiny generated
  repositories run through preview, init/migrate, format, check, check --build (both engines), a TWAE build and their own
  tests, with packages packed from the commit. Each asserts honest verdicts and exit codes, one-run convergence, preview =
  format (C# files and baselines), builds and tests green, and identical trees, exit codes and --json on both engines.
- Shapes (24): basic; git state: git-height-history, git-height-shallow (Nerdbank.GitVersioning), built-checkout (MSB4184 and
  MSB4186 on an already-built checkout); committed or generated files: committed-nupkg, t4-outputs, designer-files,
  generated-name-in-project, in-solution-generator; line endings and encoding: line-endings, crlf-under-text-auto, encoding;
  multi-targeting: two-paths, trimmer-like (`packages/Fixture.Trimmer`, ReferenceTrimmer's props/targets shape); layout:
  subfolder-props, solution-in-subfolder, unlisted-projects, solution-filter; other: design-time-failure, if-dependent,
  visual-basic, xunit-analyzers, xunit-no-fixall, stylecop-cpm (path B under central package management).
- Found: `check --build` in a shallow clone of a git-height-versioned repository said "Not clean": its copy of HEAD had no
  `.git`, so Nerdbank.GitVersioning built there and the clone's identical failures counted as new.
  `FormatCommand.WithHistory` gives the copy the repository's history (`PreviewCommand.BorrowHistory`) and HEAD's index.
- Found: the host engine DELETED a C# method in a solution whose C# tests reference a Visual Basic project: `FixOrder.FixersFor`
  took every fixer for an id, so IDE0036's Visual Basic fixer ran after the C# one on the C# diagnostics (the VB build then
  failed, BC30456). Only fixers exported for the diagnostics' languages run now (`FixOrder.LanguagesOf`). The old engine was
  right; only the engine comparison and the VB build showed it.
- Found on CI's SDK 10.0.4xx only: after format baselined a kept finding, the host's `check` still listed it (the old engine
  didn't). Two gaps let the baseline's DiagnosticSuppressor miss it: `AnalysisCache` ran a suppressor only when its own
  (empty) result wasn't cached, and read each analyzer's diagnostics from the analysis result, before suppressions (it now
  runs the suppressors asked for with every analysis and keeps what `GetAnalyzerDiagnosticsAsync` leaves); and the fix steps
  analyzed with only the analyzers reporting the step's ids (`FixOrder.AnalyzersFor` keeps their suppressors). Locally
  (SDK 10.0.1xx loads no suppressors in `dotnet format`) both engines list it; confirmed on CI.
- Found by the real-world run (Newtonsoft.Json) on this PR: the fixer-language filter with no diagnostics left for an id
  filtered out every fixer ("No associated code fix found", format didn't settle); no filter then (`FixOrder.LanguagesOf`).
- CI lessons: a pwsh step without an explicit `exit 0` ends with the last native command's code; Nerdbank.GitVersioning
  under GITHUB_ACTIONS writes to `$GITHUB_ENV` (the runner clears the CI variables for the fixtures).
  Everything else the first round flagged was the fixtures' own mistakes (a net8.0 test referencing a net10.0-only project,
  VB's `Lib` keyword, a lone CR ending a `//` comment, a missing `<Nullable>`), and StyleBro's verdicts named them honestly
  ("Not verified": the untouched build fails). The runner now builds each fixture untouched first.
- Held as designed (now asserted): IDE0049's held-back note on SDK 10.0.1xx; a lone CR in a string makes the file "Not checked"
  (exit 3) everywhere; xunit.analyzers 1.24+ in any project drops the xUnit ids for the whole run with a note (trial 44: a
  mixed repository leaves the older project's fixable xUnit findings too); a solution filter checks what it lists; the
  in-solution generator fixture is clean (no Release-only differences).
- Cost: about 2.5-5 minutes per shape (two `check --build`s are half of it), about 80 minutes serial; CI runs six shards
  (decision 2026-10-27: required on src/ PRs, nightly on main).
- From now on: a trial finding that comes from the repository's shape adds a shape (`.trials/README.md`, step 4).

## Trial 58 (2026-10-27, not clean: owner "Not clean", tally 0; host soak: clean, the third clean soak with the host as the default)

- Repos (three random plus one targeted): seesharper/LightInject, natemcmaster/CommandLineUtils, schotime/NPoco (random,
  seed 58: draws 74, 25, 26 of 79 entries; 25 and 26 walked past WireMock.Net (trial 36), xunit (rejected, trial 38) and the
  used entries 27-72 to 70 and 73; only entries 76-78 were left, so 16 candidates were appended (80-95);
  `.trials/trial58/logs/picks.md`), zspitz/ExpressionTreeToString (targeted at #310's Visual Basic fixes: C# tests
  referencing a `.vbproj`; no pool entry fit, a scan of 20 repositories for `.vbproj` found it; added to the pool). Tested
  main 556db04 (after #310). Logs: `.trials/trial58/<repo>/logs/` (round 1 `default`/`dotnet` on 0.8.0-trial58.1, round 2
  `default2`/`dotnet2` on .2).
- One session, two chains (base, default = host, `--engine dotnet-format` on fresh clones, then `Compare-Format.ps1`). Wall
  clock: 19:00 start, round 1 19:09-21:06, fixes during it, round 2 21:07-22:57 (the container restarted once, out of memory
  while the test suite ran next to a chain: NPoco's round 2 soak was rerun alone).
- LightInject: StyleCop ran in the library only, so migrate named the tests and benchmarks as without StyleCop and format left
  them alone; an xUnit2013 there (one of the xunit.analyzers ids format applies), in the untouched code too, made
  `check --build` say "Not clean: fix them by hand" on both engines. In the folders format leaves alone, format's ids are
  now compared with the untouched build like any other error, and the HEAD copy's hook keeps them as errors there.
  New fixture shape `stylecop-partial`. The library is StyleCop-clean: nothing to format.
- ExpressionTreeToString: BRO1103 (`'c' == s[0]`, `Tuesday <= dow`) and BRO1106 (`string.Empty` -> `""`, literal style)
  rewrote lambdas converted to `Expression<T>`, which the tests render to text: 14 tests failed. Owner's decision: both rules
  skip expression trees ([decision](../decisions/2026-10-27-bro1103-and-bro1106-skip-expression-trees.md)); round 2's tests
  match the untouched code.
- ExpressionTreeToString: its net452/net472 tests see `RuntimeHelpers` in two references (CS1685); with warnings as errors
  that error has no place in the code, so `check --build` said "Not verified: ... compiled nothing past their errors"
  although the analyzers ran (the old engine's build of the same projects reported 329 findings). CS1685 joins CS8002 as an
  error that doesn't stop the analyzers; new fixture shape `duplicate-predefined-type`. Round 2: Not verified, naming
  Tests.DataGenerator, which really didn't compile (it needs Tests, whose untouched TWAE errors stop it).
- Targeted lens held: the host formatted the C# projects next to the Visual Basic one, left the `.vb` files alone, and the VB
  project built; the old engine left the four C# projects reaching it Not checked (exit 3; check --build 2 on what it left),
  the documented difference (#310), the only one in the soak.
- CommandLineUtils: 115 files, one run, 2 kept BRO1603 (a test compares the doc text) baselined, clean on both engines.
  Format ran net10.0 on its own (4 runs) where check merged it with net8.0 (3): the code tests only NET6_0_OR_GREATER, so
  both are right; the extra run is only time. NPoco: 325 files, clean on both engines.
- Every TWAE build and test suite matched the untouched code after the fixes (LightInject 1,568, CommandLineUtils 1,143,
  NPoco 367, ExpressionTreeToString 3,080 of 3,089 with the same 9 failing).
- Host soak: no fallback to dotnet format; trees, exit codes, verdicts, --json, builds and tests identical on LightInject,
  CommandLineUtils and NPoco in both rounds, Compare-Format Same six times; ExpressionTreeToString differs only by the
  documented Visual Basic gap. Format (s, host/dotnet-format, r1; r2): LightInject 39/62; 31/42, CommandLineUtils 99/215;
  130/288, NPoco 173/331; 201/401, ExpressionTreeToString 61/120; 72/84. The trial chain's `check` after format was slower
  on the host for NPoco (116/82; 105/76) and ExpressionTreeToString (30/23; 35/20); the soak's own check (on unformatted
  code) was faster (NPoco 148/349).
- Environment: SDK 10.0.113 + 8.0.132; the trial feed in the user NuGet.Config (no repository here clears its sources);
  LightInject's tests need `dotnet tool install -g dotnet-ilverify`; NPoco's test config switched to SQLite in-memory;
  global.json lowered (LightInject); the net4x test frameworks need mono (CommandLineUtils, ExpressionTreeToString:
  net6.0 only).

## Trial 59 (2026-10-28, not clean: owner "Not clean", tally 0; host soak: clean, the fourth clean soak with the host as the default)

- Repos (three random plus one targeted): xoofx/SharpYaml, adamralph/bullseye, danielpalme/ReportGenerator (random, seed 59:
  draws 28, 10, 83 of 95 entries; every entry 0-75 is used, 76 Vogen (702 files) and 77 PdfPig (1,238) are too large, so 28
  walked to 78 and 10 to 79; `.trials/trial59/logs/picks.md`), hazzik/DelegateDecompiler (targeted at trial 58's
  expression-tree skip; no pool entry fit, a scan of 10 LINQ/expression repositories found it; added to the pool). Tested
  main 1ab4981 (after #311) plus this PR's commits: round 1 on 0.8.0-trial59.1 (`default`/`dotnet`), round 2 on .2
  (`default2`/`dotnet2`), both before #312/#313. Logs: `.trials/trial59/<repo>/logs/`.
- One session, two chains (base, host, `--engine dotnet-format`, `Compare-Format.ps1`). Wall clock: 07:00 start (the
  session's permission classifier first refused to build the clones; the owner allowed it), round 1 09:24-12:08, round 2
  13:00-14:37; the worker restarted twice (no runs lost but DD's, rerun).
- SharpYaml: `check --build` said "Not clean" on both engines over the untouched Debug build's CS0281 (the tests sign only in
  Release): MinVer versions from git tags, the HEAD copy had none, built as 0.0.0.0, and CS0281's text names the version.
  The copy now gets the tags (packed refs); new fixture shape `git-tag-version` (fails without the fix, both engines).
- SharpYaml: kept BRO1303 renames' "first:" lines were the text before format's other fixes (BRO1514's braces moved one 96
  lines), on both engines, and the engines named different lines for three where their fix orders differ: the soak's one
  real difference. Owner's decision: format re-finds the string in the formatted text
  ([decision](../decisions/2026-10-28-a-kept-findings-first-location-is-refreshed-against-the.md)); round 2 identical.
- DelegateDecompiler: its tests pair `Expression<Func<E, bool>> expected = x => E.Bar == x;` with a compiled twin written the
  same way and compare the decompiled IL; BRO1103 swapped the twin (2 tests failed). Owner's decision: BRO1103 and BRO1106
  skip a delegate lambda equivalent to an expression-tree lambda of the same member
  ([decision](../decisions/2026-10-28-bro1103-and-bro1106-skip-expression-tree-twins.md)); round 2's tests match the
  untouched code. init turned BRO1106 off there (mixed `string.Empty` and `""`), so only BRO1103 reached the trees.
- DelegateDecompiler (setup): the reference in a new root Directory.Build.props, which `src/Directory.Build.props` (no import)
  shadows; format said only "add the package". It now names the file the reference belongs in; `subfolder-props` extended.
- ReportGenerator: migrate (StyleCop in Core only, the other projects left alone and named: trial 58's partial-StyleCop fix
  held), 46 files, clean on both engines; its untouched TWAE build's 44 SA1204/SA1629 errors are gone. Bullseye: lock files
  with RestoreLockedMode under CI (restored unlocked, as getting-started says), PublicApiAnalyzers, 22 files, clean.
- Every TWAE build and test suite matched the untouched code after round 2's fixes (SharpYaml 886, Bullseye 213,
  ReportGenerator 335 of 388 with the same 51 failing, DelegateDecompiler 451 + 50 VB).
- Host soak: no fallback; trees, exit codes, verdicts, --json, builds and tests identical on all four in round 2 (round 1:
  the "first:" lines above); SharpYaml differs only by the old engine's load-warning note (documented). The Visual Basic
  test project in DelegateDecompiler references C#, not the reverse: no Not checked on the old engine. Format (s, host/
  dotnet-format, r1; r2): ReportGenerator 66/131; 90/161, Bullseye 48/180; 53/136, DelegateDecompiler 82/230; 95/332, SharpYaml
  160/370; 146/347. The trial chain's `check` after format, host/dotnet-format: ReportGenerator 60/44 (r1, host slower); 66/75,
  Bullseye 30/50; 34/52, DelegateDecompiler 46/61; 67/95, SharpYaml 51/57; 58/72. The soak's own check (unformatted code) was
  faster on the host everywhere but ReportGenerator r1 (117/113).
- Environment: SDK 10.0.113 + 8.0.132; the trial feed in the user NuGet.Config; global.json lowered (ReportGenerator 10.0.403,
  SharpYaml 10.0.103); `-p:NuGetAudit=false` (DelegateDecompiler's EF Core 10 tests); DelegateDecompiler's EF tests need SQL
  Server and its net40/net45 tests mono (net8.0 + the VB project only); `pkill -f` with a pattern from its own command line
  killed the shell again.

## Trial 60 (2026-10-29, not clean: owner "Not clean", tally 0; host soak: clean, the fifth clean soak with the host as the default)

- Repos (three random plus one targeted): apache/logging-log4net, Cysharp/MasterMemory, dotnetcore/EasyCaching (random,
  seed 60: draws 39, 36, 73 of 95 entries; 0-75, 78, 79 and 83 are used, Vogen/PdfPig (as in trial 59) and Azure/DotNetty
  (898 files) too large, so they walked to 80, 82, 84; `.trials/trial60/logs/picks.md`), khellang/Middleware (targeted at
  trial 59's shadowed-props fix: `src/` and `test/` props, neither importing, no root one; added to the pool). Tested main
  f85bb6c (after #312-#316) plus this PR's commits: round 1 on 0.8.0-trial60.1 (`default`/`dotnet`), round 2 on .2
  (`default2`/`dotnet2`). Logs: `.trials/trial60/<repo>/logs/`; run.sh's `check-full` deletes format's clean record first
  (`rmrecord.py`; the record is keyed on the solution's folder, `src/` for log4net).
- One session, two chains (base, host, `--engine dotnet-format`, `Compare-Format.ps1`), resumable since the worker restarted
  five times (`queue2.sh` skips finished tags). Wall clock: 06:35 start, round 1 06:59-08:43 (MasterMemory and log4net's
  old engine cut off by restarts), round 2 09:33-11:31, local checks and verdict until 13:10.
- EasyCaching: a method's `}`, `#region Mainly For Memcached  `, a doc comment: BRO1505 accepts a blank line above or below
  the region line but adds it above; BRO1513 adds its own below. Run first, BRO1513's line satisfied BRO1505: the host
  wrote one blank line, the old engine two, each clean in its own check (the soak's tree difference). BRO1513's fix now
  adds BRO1505's line too when BRO1505 is on (FixOrderTests case, unit tests); round 2 identical.
- log4net: BRO1120 removed the first and last empty `//` of the Apache license header (inside `#region Apache License`) in
  325 of 402 files. Owner's decision: BRO1120 leaves the file header alone, also below a leading `#region` line
  ([decision](../decisions/2026-10-29-bro1120-leaves-the-file-header-alone.md)); init's trial 11 convention that turned
  BRO1120 off for such headers is gone. log4net has two solutions (`examples/`, `src/`): format asks for one, as documented.
- Middleware: with the reference in a new root props as getting-started says, format named both shadowing files (trial 59's
  fix held; init said nothing, but pinned the preset in all three props once they had the reference). Cloned without its
  submodule (ProblemDetails compiles `lib/AspNetCore/src/Shared/**/*.cs`), format and check said clean on code that doesn't
  compile; only `check --build` said Not verified. Owner's decision: format, check and the preview name the submodule and
  leave out IDE0005 ([decision](../decisions/2026-10-29-format-names-a-git-submodule-that-isnt-checked-out.md)); new fixture
  shape `submodule-not-checked-out` (fails without the fix). The soak's own clones have no submodule either.
- MasterMemory: format Not verified (exit 3) on the in-solution generator's Release configuration, check builds it: clean
  (documented). EasyCaching: a `Build.csproj` at the root in the solution, no props or .editorconfig; 84 files changed only
  their line endings (33 CRLF, 51 mixed: init's `end_of_line = lf` from 225 of 258); its untouched TWAE build fails
  (xUnit1048/xUnit2013), passes after.
- #312: `bro check` after a clean format answered from the record in about a second (MasterMemory 6-14 s: the generator's
  Release build) with the same verdict and exit code as the full check after deleting the record and as `check --build`, on
  both engines. The full check adds the load-warning note the fast one can't (it loads nothing).
- Every TWAE build and test suite matched or beat the untouched code in round 2 (log4net 447 + 52, MasterMemory 28 + 7,
  EasyCaching 893 of 897 offline with Redis/Memcached/Hybrid left out (a FasterKv test flaky untouched), Middleware 14 + 57 +
  9 with the untouched TWAE's 7 errors).
- Host soak: no fallback; round 2 trees, exit codes, verdicts, --json, builds and tests identical on all four. The old
  engine's check on unformatted code listed duplicate findings from a second instance of a multi-targeted referenced
  project (log4net 3, at the line before the imports fix; EasyCaching 39 IDE0055 next to the same WHITESPACE): owner's
  decision, an allowed difference
  ([decision](../decisions/2026-10-29-the-old-engines-duplicate-findings-from-a-second-project.md)). Format (s, host/
  dotnet-format, r2): log4net 199/702, EasyCaching 233/449, MasterMemory 83/97, Middleware 55/104; check --build:
  305/410, 450/474, 129/105, 135/126; full check: 84/149, 148/181, 53/48, 37/36. Soak (Compare-Format, check; format):
  log4net 176/549; 176/660, EasyCaching 268/536; 238/439, MasterMemory 101/142; 76/101, Middleware 74/122; 44/102.
- Environment: SDK 10.0.113 + 8.0.132; the trial feed in the user NuGet.Config; Middleware's dead dotnetfeed feeds removed
  and its submodule cloned with `--depth 1`; `-p:NuGetAudit=false`; MasterMemory's net9.0 tests with `DOTNET_ROLL_FORWARD=Major`;
  `pkill -f` with a pattern from its own command line killed the shell again; editing a running script with `sed -i` doesn't
  reach the running bash (it keeps the old file).
