# Changelog

Versions are git tags (`vX.Y.Z`, prereleases `vX.Y.Z-alpha.N`); each one publishes StyleBro.Analyzers and
StyleBro.Tool (StyleBro.Migrate up to 0.7.0-alpha.1) to nuget.org and a GitHub release with its section below as the notes. What a version number may
change: [docs/versioning.md](docs/versioning.md). What's merged but not released yet is in [changes/](changes/) (one
file per pull request).

## 0.8.0-alpha.1 – 2026-10-19

129 rules, each fixed by `dotnet format`. Docs: https://bisforboman.github.io/stylebro/

### Breaking changes

- **The tool is now `bro`**, in the package **StyleBro.Tool** (was `stylebro-migrate` in StyleBro.Migrate). The
  commands are `bro init`, `bro migrate` (the StyleCop migration, before `stylebro-migrate [path]`), `bro format`,
  `bro check` (= `bro format --verify-no-changes`), `bro preview <init|migrate|format>` (= the command with `--diff`)
  and `bro baseline`. The old forms still work with `bro` in place of `stylebro-migrate` (`bro [path] --write`, `bro init --diff`,
  `bro format --verify-no-changes`).

### New

- **`bro format`** also applies xunit.analyzers' assertion fixes in repositories that use xunit, such as
  `Assert.Equal(true, x)` -> `Assert.True(x)` and `Assert.Equal(0, list.Count)` -> `Assert.Empty(list)`, where the
  repository leaves those rules at warning or error. Fixes that change what a test does stay out. `bro check`, `bro
  preview format` and `bro baseline` cover the same rules. See [xUnit's fixes](https://bisforboman.github.io/stylebro/xunit/).
- **`bro format --timings`** prints how long each phase took (evaluating the projects, the restore, each
  check and fix run, CSharpier, the builds `--verify-no-changes` makes).
- **`bro format --restore`** restores even when the restore output looks current (see below).
- **Docs:** [Pre-commit hook](https://bisforboman.github.io/stylebro/pre-commit/): check or fix the staged C# files
  with `bro format --files` before each commit, as a plain git hook, with Husky.Net or with lefthook,
  including files staged only partly and file names with spaces.
- **`bro format`** names checked-in generated files in the projects it changed ("rebuild and commit it"),
  and reminds you to run CSharpier where the repository uses it.
- `bro format` puts the findings its fixes keep on purpose into `stylebro.baseline` (the one the build
  reads) when a run ends clean, so a build with warnings as errors passes and `--verify-no-changes` agrees; new
  violations still fail. `<StyleBroBaseline>none</StyleBroBaseline>` turns it off.
- `format`, `--verify-no-changes`, `--diff` and `baseline` build an in-solution source generator first when it isn't
  built, so its generated code counts (a rename it used broke the build before).
- **CSharpier after `format`**: where the repository formats with CSharpier (its `csharpier` tool, or the
  CSharpier.MsBuild package), `bro format` runs it on the C# files each run changed, so one command leaves
  a tree `dotnet csharpier check` accepts. `--verify-no-changes` runs CSharpier's check on the C# files it formats too.
  Where CSharpier puts back what a fix changed (a modifier order), both say so and name the files.
- **Release code is fixed and checked too**: where a project's code tests `DEBUG`, `RELEASE`, `TRACE` or its own
  configuration symbols, `bro format`, `--verify-no-changes`, the preview and `baseline` also run its other
  configurations (Release), only where the code compiles differently there. Before, code under `#if !DEBUG` was never
  checked and a Release build with warnings as errors failed after a clean check. A project that runs an in-solution
  source generator stays in its default configuration, and `format` says so.
- **The Microsoft style:** `<StyleBroStyle>microsoft</StyleBroStyle>` puts dotnet/runtime's coding style on top of the
  preset: `_camelCase` and `s_camelCase` fields, fields first, braces optional on one-line bodies, no `this.`, and the
  StyleCop-derived rules runtime has off turned off. `bro init --style microsoft --write` sets it up with the
  built-in rules it needs; your code's own conventions still win. Plain `init` suggests it when your code looks like
  runtime's. An unknown style fails the build (STYLEBRO003), a style with `<StyleBroPreset>none</StyleBroPreset>` warns
  (STYLEBRO004). Details: [Styles](https://bisforboman.github.io/stylebro/styles/).
- `s_camelCase` for `stylebro_private_mutable_static_field_naming` and `stylebro_private_static_field_naming`: static
  fields get the .NET runtime's `s_` prefix (`count`, `_count` and `m_count` become `s_count`, `s_` is kept; constants
  stay PascalCase).
- `stylebro_internal_field_naming = like_private` names internal and private protected fields like private ones
  (`internal int _count;`, `internal static int s_count;`) instead of StyleCop's PascalCase.
- `stylebro_member_order_scope = fields_first` makes BRO1001 check only that fields come first in a type, keeping the
  order of everything else (the .NET runtime's rule 15).
- **The `stylecop` style:** `<StyleBroStyle>stylecop</StyleBroStyle>` (or `bro init --style stylecop
  --write`) names the default, StyleCop's defaults, so a build says which style it uses. It changes nothing: the preset
  already is that style. Details: [Styles](https://bisforboman.github.io/stylebro/styles/).
- `stylebro_summary_wording = stylesharp` makes BRO1604-BRO1607 follow StyleSharp's summary rules: a `protected` setter
  counts as restricted, a getter counts whatever its modifier, `Gets or sets` on a read-only property is fine, and a
  constructor summary only needs the standard words before the type.
- `stylebro_member_order` takes a `record` kind (records after classes, as StyleSharp orders them) and kinds joined by
  `|` that share a place (`conversion|operator|method`).
- `stylebro_trailing_comma_switch_expressions = false` leaves switch expressions to you (StyleCop 1.1 and StyleSharp
  don't ask for the comma there).
- **`bro migrate` reads StyleSharp.Analyzers** (a StyleCop successor with `SST` ids) like StyleCop: its rule
  severities count as the StyleCop rules they stand for (`SST1309` keeps your `_camelCase` fields, `SST1117` your `this.`
  choice), its `stylesharp.*` settings as `stylecop.json`'s, suppressions of SST ids are carried over, and the report says
  where to remove the package and which StyleSharp rules have no StyleBro replacement.
- **`stylebro_allow_leading_multiline_lambda`** (BRO1107, BRO1108; default `false`, like StyleCop): with `true`, a call whose
  first argument is a multi-line lambda, anonymous method or anonymous object may keep the other arguments on its closing
  line (`Register(ctx =>` / `{ ... }` / `}, SymbolKind.Field);`). `bro init` writes it when your code uses
  that layout.
- `bro init` detects more of your code's conventions: empty blocks `{ }` on one line
  (`stylebro_allow_empty_single_line_blocks`), properties with block-bodied accessors on one line (BRO1509), mixed accessor
  layouts (BRO1510) and partial type parts without an access modifier (BRO1007).

### Changed

- **BRO1524** with BRO1520 off puts the missing line break of a split conditional on the same side as its other
  token's (`cond ?` / `a : b` becomes `cond ?` / `a :` / `b`), instead of the side
  `dotnet_style_operator_placement_when_wrapping` asks for, so a conditional no longer mixes both layouts in code where
  `init` turned BRO1520 off.
- **bro format** converts a C# file's old Mac line endings (a CR alone, outside string literals) to the file's own line
  break before formatting it, and says which files. It used to leave such files out, end with "Clean", and the build then
  reported StyleBro's findings there. `bro check` reports them as files formatting would change.
- **bro format** leaves xUnit's assertion fixes out where the restore output has xunit.analyzers 1.24 or later (as with
  `xunit.v3` 3.x), and says why: from that version on its fixes don't support Fix All, which `dotnet format` needs. It used to
  say "Clean" while `bro check` reported the same warnings as not fixed.
- **`bro format` is faster before the first `dotnet format` run.** It skips `dotnet restore` when every
  project's restore output lists StyleBro.Analyzers, its packages are there and nothing it depends on changed since
  (project, props, targets, `nuget.config` and `global.json` files); evaluating the projects for other configurations
  runs all at once instead of partly one after another.
- **`--verify-no-changes` right after a clean `format` answers in seconds.** When the code, settings, options, SDK and
  StyleBro versions are the ones that `format` ended clean on, and its last run checked every target framework and
  configuration, verify answers from that record instead of running every check again. The builds of the
  configurations an in-solution source generator keeps from the checks still run, now one build per configuration for
  all its projects.
- **bro init** turns off **BRO1102** when the code mixes `[A, B]` and one attribute per list, or mostly
  combines them.
- **bro init** says why a few one-line jumps (`if (x) return;`) are enough to keep them allowed.
- **`--diff`** writes the full diff to `.git/stylebro-preview.patch` (outside a git repository: the temp folder) instead
  of an untracked file in the current folder; `--diff=file` still names another file.
- **`bro format`** says once what "Could not format '...fsproj'" and "Warnings were encountered while
  loading the workspace" mean, instead of repeating them in every run.
- **`bro init`** turns [BRO1506](https://bisforboman.github.io/stylebro/rules/BRO1506/) off when your code
  mostly keeps a blank line below its `//` comments (or mixes both), like its other convention checks.
- **`bro init`** no longer turns IDE0009 off when your `.editorconfig` requires `this.`
  (`dotnet_style_qualification_for_* = true:warning`).
- A suppressed Sonar S4136 in `<NoWarn>` no longer turns BRO1001 off in that folder: **`init`** and **`--write`** set
  `stylebro_keep_overloads_together = false` there instead, and members are still ordered.
- `bro init` honors folders that turn every analyzer off (`dotnet_analyzer_diagnostic.severity = none` or
  `silent` in their own `.editorconfig`, such as test samples): it turns StyleBro's rules, IDE0055 and the other built-in
  rules it manages off there by id, so the build and `format` leave those files alone. Before, it turned StyleBro's rules
  on there, and `format` rewrote the samples. Rules the folder's `.editorconfig` sets by id stay as they are.
- `init` keeps your own IDE0055 severity (e.g. `suggestion`) instead of writing `warning`.
- `init` detects three more conventions: a blank line between a doc comment and its element (BRO1511), primary
  constructor parameters named like fields (`_logger`: BRO1302 is turned off, it has no switch for those alone), and
  the casing of tuple element names (`stylebro_tuple_element_name_casing`).
- The block `init` writes into a folder's own `.editorconfig` is now called `stylebro-folder` (it holds rule severities
  too, not only whitespace settings); an existing `stylebro-whitespace` block is replaced.
- **`init`** follows two more conventions of your code: types without `internal` (then BRO1404 stays off, as no
  setting asks for modifiers on types only) and `case X:` blocks at the label's level
  (`csharp_indent_case_contents_when_block = false`).
- **`format`** removes the usings its fixes leave unused in the same run where IDE0005 is enforced, one run sooner.
- **`bro migrate`** says your `stylecop.json` can go once StyleCop.Analyzers is gone; the preview counts carried-over
  suppressions the same way `--write` does.
- **`bro format`** is faster on multi-targeted repositories: a framework whose code compiles exactly like
  another's (no `#if` tells `net8.0`, `net9.0` and `net10.0` apart, say) gets no run of its own, `LangVersion` `latest`
  no longer splits projects that share files into separate runs, and after a run that changed files, only the runs whose
  check finds something run again, on the files with findings.
- **`bro format`** honours your own IDE0055 severity: where it's `none` (by id, for the Style category or in
  bulk, in `.editorconfig` or `.globalconfig`), the whitespace pass leaves those files alone.
- **BRO1508** and **BRO1509** leave a one-line block with a comment after its `}` on its line
  (`catch (IOException) { } // slopwatch-ignore: ...`): expanded, the comment ended up on the `}` line, and tools
  that read a suppression from the statement's line reported the code again.
- **BRO1604** accepts `Gets or sets` on a property with `get` and `init` (it no longer rewrites it to
  `Gets or initializes`).
- `format` leaves Visual Basic alone: it runs on the C# projects only, never changes or reports `.vb` files, and keeps
  the name of a C# member that Visual Basic code uses.
- Projects that link a shared file now share a `format` run when the `#if` symbols that file tests agree (fewer runs
  on multi-targeted repositories).
- **BRO1001** sorts like StyleCop in two more places: an explicit event implementation (`event EventHandler IServer.E
  { ... }`) counts as private, not public, and in a record only kind and accessibility count (StyleCop doesn't check
  constants, statics or readonly fields there). StyleCop-clean code no longer gets reordered after migrating.
- **BRO1002** inserts its space before tabs after `//` instead of replacing them, so commented-out code indented with
  tabs keeps its layout.
- **BRO1504** no longer asks for a blank line above a comment between two parts of one expression (`ok ? value`,
  `// why`, `: throw ...`).
- `format --files` with files in several projects runs once for all of them (two edited files in two of 11 projects:
  27 s instead of 39 s), and a framework whose check found something formats just those files. `--verify-no-changes --files` formats a
  copy of just the files with findings.
- CSharpier after `format` and in `--verify-no-changes` gets whole folders where it would change nothing else, and says
  when it runs: a check of 612 files took 2 s instead of 85 s.
- The preview's patch includes the baseline entries for the findings kept on purpose, and its summary lists them.
- A killed `format` no longer leaves `dotnet format` running on Windows, and old temporary files of killed runs are
  cleaned up.
- The progress lines name what a run loads (`Checked 11 projects (run 1 of 1)`), and the line about folders the
  whitespace pass leaves out starts with `Whitespace pass leaves out:`.
- `bro format` leaves files a project compiles from outside the repository alone, like git submodules
  (`Leaving alone: ../outside/Shared.cs (outside the repository)`); before, it rewrote them without listing them.
- Where CSharpier formats the repository and `end_of_line` is `lf` or `crlf`, `format` also runs CSharpier on the C#
  files with other line endings, so `dotnet csharpier check` passes afterwards. `--verify-no-changes` also counts
  CSharpier's findings in Directory.Build.props and Directory.Packages.props.
- `--verify-no-changes` builds a project with an in-solution source generator in its other configurations (Release),
  which `format` can't run there, and is not clean when that build reports one of the rules `format` fixes.
- A `format` run that changed only line endings or byte order marks is the last one: no second run checks every
  framework again.
- **`bro init`** turns BRO1519 off when the code is mixed about blank lines after a block's `}`, like every
  other mixed convention, instead of adding hundreds of blank lines.
- **`bro init`** writes `s_camelCase` (`stylebro_private_static_field_naming`,
  `stylebro_private_mutable_static_field_naming`) when private static fields mostly have the `s_` prefix, instead of
  turning BRO1307 off.
- **`bro init`** no longer allows one-line jumps (`if (x) return;`) when your .editorconfig enforces braces
  (IDE0011 at warning or error).
- **`bro migrate --write`** with StyleSharp: SST1149 (`is null` checks) turns on BRO1133 with
  `stylebro_null_check_style = pattern_matching`, so `null == x` no longer becomes `x == null`.
- **BRO1001** keeps blank lines when it sorts: members that move together keep the gap between them (fields moved to
  the top stay together), and a moved member gets a blank line where its new neighbour needs one, also with BRO1505 off
  (the Microsoft style). No more `}` glued to the next member.
- **BRO1506**: a comment right after a block's `{` with a blank line below it counts as standalone, like the section
  headers after it, and keeps its blank line.
- Findings in files a project compiles from outside its git repository (`..\..\outside\Shared.cs`) are hidden in the
  build, StyleBro's and those of the built-in rules it turns on: `bro format` leaves those files alone.
  `<StyleBroRepositoryRoot>none</StyleBroRepositoryRoot>` reports them again.
- `bro format` fixes the Release-only code (`#if !DEBUG`) of projects that use an in-solution source
  generator: it builds the generator in Release first and gives the project a Release run like any other project.
- A project whose project file picks its `Compile` items by `$(Configuration)` (a file only Release compiles) gets a run
  in that configuration too, so that file is formatted and checked.
- `--verify-no-changes` counts the documentation comment indentation `format` rewrites only in projects without
  `GenerateDocumentationFile`, where the build reports it; with documentation generated the build reports none or only
  some of those lines.
- When `dotnet format` had warnings loading the workspace, `format` names the restore's first warning (such as NU1603)
  instead of guessing at the cause.
- `--diff` and `format` put a new `stylebro.baseline` in the same place: next to the projects of the solution `format`
  runs on, also when run on the repository folder.
- The preset's global configs moved from `global_level = -1` to `-2` (a style sits at `-1`). Your own `.editorconfig` and
  `.globalconfig` still win over both.
- **`format`**, its preview and `--verify-no-changes` are much faster on large multi-targeted repositories:
  - projects that compile a shared file with the same `#if` symbols, language version and nullable context (a polyfill
    every library links) share a run instead of getting one each;
  - a run whose files are all left to another formatter (CSharpier) skips the whitespace pass, and a run with none of
    them does all passes in one `dotnet format` call;
  - the check after a run that changed files counts the findings the fixes keep on purpose as clean, instead of running
    everything again;
  - `--verify-no-changes` right after a clean `format` knows the findings it kept and formats no copy; otherwise it
    formats only the files with findings, for their rules, in its temporary copy; and it shows its progress;
  - at most one `dotnet format` per processor runs at once.
- `bro` on a StyleSharp repository writes StyleSharp's own meaning where its rule differs from the StyleCop
  rule with the same number: the settings above, StyleSharp's list of words a name may start with (`id`, `ok`, `db`,
  ...), one-line empty blocks, no period check in `<param>`, `<typeparam>` and `<exception>`, and no namespace naming.
  StyleSharp-clean code no longer gets changes StyleSharp then reports (`SST1624`, `SST1201`).
- `bro` on a StyleCop 1.1 repository keeps BRO1401 on and only leaves switch expressions alone, instead of
  turning the rule off when the code had a switch expression without the comma.
- `bro` no longer migrates a leftover `stylecop.json` (no StyleCop package referenced) when the repository
  has a Sonar setup: it points to `bro init`, which follows the Sonar setup and the code's own conventions.
- **BRO1506** no longer reports a standalone comment, one with a blank line right above and right below it (a note on
  the code above, a section header): removing the blank line glued it onto unrelated code. The new
  `stylebro_blank_line_below_standalone_comments = remove` reports it again, like StyleCop's SA1512, and
  `bro migrate` writes it where SA1512 is on.
- **BRO1110** with `stylebro_closing_parenthesis_placement = own_line` leaves a `)` that is already below the last item
  where it is, at any indentation, instead of reindenting it or pulling it up to a `}`.
- **BRO1149** without BRO1520 (for example with CSharpier) puts the merged `&&` where
  `dotnet_style_operator_placement_when_wrapping` says (the beginning of the line by default), unless the condition
  already wraps after its operators.
- `bro init` counts four more conventions: `//` comments right below code or after a blank line (BRO1504),
  doc comment texts with or without a closing period (BRO1603), keywords in doc comments as `<see langword="null"/>` or
  `<c>null</c>` (BRO1618), and non-private fields against an enforced naming rule (`dotnet_naming_rule.*`): when your
  code doesn't follow the rule, BRO1306 is turned off instead of renaming the fields to it.
- `bro init`: calls whose first argument is a multi-line lambda no longer count for BRO1107/BRO1108's
  conventions once such calls are allowed (`stylebro_allow_leading_multiline_lambda`), so the two rules stay on.
- `bro init` keeping one-line empty blocks also writes `csharp_preserve_single_line_statements = true`: without
  it, IDE0055 split `else { }` and `lock (x) { }` onto two lines.
- Files from NuGet packages, git submodules and source generators are no longer analyzed at all: their findings were
  hidden anyway, but every rule still ran on them.
- **BRO1108** gives items that already start a line the same indentation as the items it moves, so a split list no
  longer ends up as a staircase.
- **BRO1149** keeps an inner condition's lines aligned under its `(` when it joins the outer condition's line.
- **BRO1504** no longer wants a blank line between `if (...)` (or `else`, a loop, `using`, `lock`, `fixed`) and a
  comment above the statement it runs without braces.
- **BRO1107**/**BRO1108** with `stylebro_allow_leading_multiline_lambda = true` also accept a leading lambda in
  parentheses (`Invoke((() => { ... }), 1)`) or wrapped in a delegate creation (`new Action(() => { ... })`).
- **bro init** counts namespace braces: when most files put a namespace's `{` where
  `csharp_new_line_before_open_brace` doesn't (Roslyn's `types` covers namespaces), it says how many files would change
  and turns IDE0055 off until you decide, so `format` doesn't move them.
- **bro init** reports two more conventions: statements after a block's `}` (BRO1519), and nested `if`s
  BRO1149 would merge; with a few nested `if`s in the code, BRO1149 is off.
- **bro** (`--write`) records `stylecop.json`'s settings in its block, so `stylecop.json` can go after the
  package swap; the report says so.
- **bro** (`--write`) turns StyleBro's rules and the built-in ones its block sets off by id in the folders
  without StyleCop, so the build there agrees with `format`, which leaves those folders alone.
- **bro** (`--write`) maps StyleSharp's SST1653 (short summaries on one line) to BRO1616 with
  `stylebro_summary_layout = single_line_when_fits`.
- **bro** says when a new `Directory.Build.props` hides one above the repository.
- `bro format` runs a solution's platforms like its configurations: code under
  `'$(Configuration)|$(Platform)' == 'Release|x64'` is fixed and checked too.
- `format` rewrites the indentation of documentation comment lines that `dotnet format` leaves while the build's IDE0055
  reports them (`///` lines indented with tabs under a first line with spaces, at the same column), and
  `--verify-no-changes` reports them.
- The files of NuGet packages a project compiles (source packages such as PolyShim or Nullable) no longer add
  configuration runs or split runs, so the runs no longer depend on whether the repository was restored before.
- A project that a `.slnx` doesn't build (`<Build Project="false" />`) is left alone like one a `.sln` doesn't build.
- **`bro init`** recognizes a vendored folder by a license file of its own whose copyright holder isn't the
  root license's (a fork copied in with its `LICENSE`): it marks it `generated_code = true` and leaves it out of the count.
- **`bro migrate --write`** follows your `.editorconfig`'s naming rule for private static fields over SA1309 when no
  such field in the code breaks it (it's the rule left after the swap).
- Names used as structured log keys keep them: a variable, parameter, field, property or tuple element used in an
  interpolated string's hole whose handler takes the hole's text as a `[CallerArgumentExpression]` argument (ZLogger's
  `$"{UserId}"` logs the key `UserId`) isn't renamed by BRO1301-BRO1311. `bro format` lists it as kept, with
  the handler's name, and baselines it.
- Files a T4 template writes (a Compile item with `DependentUpon` `*.tt` or `AutoGen` True) count as generated code:
  StyleBro and the built-in rules it turns on report nothing there, and `bro format` leaves the C# file next
  to each `.tt` file alone.
- **`bro init`** and **`--write`** count only C# files a project compiles: a file below no project (a Unity
  `Assets/` folder) no longer decides a convention, unless a project links files from elsewhere or a shared project
  could include it.
- **`bro migrate --write`** follows your `.editorconfig`'s naming rule for private `static readonly` fields (one
  requiring just `static` covers them) when no such field breaks it, instead of pinning them to PascalCase.
- **`bro init`** counts how your public and internal fields are named when no naming rule decides it: code
  that names them in camelCase turns BRO1306 off instead of having them renamed to PascalCase.

### Fixed

- **`bro --json check`** (and `--json` before `migrate` or `preview`) runs that command; it used to read the word as
  the folder to migrate. `bro preview --help` shows the help.
- **`bro check --diff`** says a preview and a check don't go together, instead of failing halfway through the preview.
- **`bro migrate init`** (or another command's name) migrates the folder of that name.
- **Temporary copies:** a later `bro` command deletes only the preview and check copies it made itself, and never one
  kept with `bro preview ... --keep`; folders with similar names that other programs made stay.
- **`bro init --write`** no longer writes the preset version into a `Directory.Build.props` that stops the import of the
  one above it (`<Project />`) when nothing under it references StyleBro.Analyzers.
- **`bro init`** no longer turns BRO1606 off because constructor summaries are in their own words: BRO1606 accepts those.
- **init** and **migrate** keep the line ending of a file that holds a string literal spanning lines (`@"..."`,
  `"""..."""`) when they write `end_of_line` for the other ending: the file gets a section of its own with its ending.
  Before, `format` converted it and changed the literal's value, and tests comparing text with it failed.
- **migrate** keeps `case X:` blocks written at the label's level (`csharp_indent_case_contents_when_block = false`), as
  init does: StyleCop doesn't check their indentation, and `format` re-indented them.
- **`bro format`** formats projects that reach a multi-targeted project only through another project's reference (a
  library for one framework that references one for one framework that references a multi-targeted one). Before, the
  run for a framework only the last one has skipped them, and every project referencing them, as "references didn't load".
- **BRO1504** no longer puts a blank line between a trailing comment and a comment the formatter aligned below it in a
  file indented with tabs (`tab_width` other than 4): the next `dotnet format` run moved the separated comment.
- **BRO1132** and **BRO1134** move the `{` up to the header's line when `csharp_new_line_before_open_brace` says so for
  that kind of block (`if (x) // why` + `{`), instead of leaving that to the next `dotnet format` run.
- **BRO1405** moves the `let`/`where`/`select` lines of a multi-line query along when it removes the parentheses in front of
  its `from`, so `bro format` needs one run instead of two.
- **BRO1602** also removes the empty `//` lines its conversion leaves at the start or end of a comment (BRO1120's), so
  `bro format` no longer needs a second run for them.
- **Naming rules** (BRO1303-BRO1309) rename theory data that `[MemberData(nameof(...))]` names, instead of keeping the
  name: the rename updates the `nameof` too.
- **`bro baseline`** no longer hangs when `dotnet format` reports a lot of whitespace changes (it waited
  forever on a large multi-targeted solution).
- **`bro baseline`** writes the baseline the build reads: the file `<StyleBroBaseline>` names (with `../`
  paths when it's outside the code, e.g. `eng/stylebro.baseline`), else the nearest `stylebro.baseline` above, and says
  where it wrote. With `<StyleBroBaseline>none</StyleBroBaseline>` it writes nothing. Entries for files outside the
  folder it ran on stay.
- **BRO1311** and the renaming rules (BRO1301-BRO1309) no longer break the build where IDE0042 (deconstruct variable
  declaration) is a warning or an error: a rename that would end a clash between a tuple element and a variable, so that
  IDE0042 reports the tuple's variable, is left with its warning.
- BRO1608-BRO1614 and BRO1616 leave documentation with malformed XML (a bare `<` as in `when a < b`) alone too, like the
  other documentation rules, instead of removing, unwrapping, renaming or reordering its tags. An empty `name=""` no
  longer counts as malformed.
- Expanding a one-line method body (BRO1508/BRO1509) with `csharp_preserve_single_line_statements = false` now also lays
  out a braced `if` with an unbraced `else` (`if (x) { a; } else b;`) and other braced loop bodies the way the SDK's
  formatting does, so `format` no longer needs another run for them.
- **bro migrate --write** judges `end_of_line`, `charset` and `insert_final_newline` against the .editorconfig that
  actually decides them for each file, like `init` does: a nested .editorconfig (e.g. `Source/.editorconfig`) that sets
  them gets its own `stylebro-whitespace` block, and files under it no longer count for the root's settings. Before, the
  migration could promise that `format` leaves line endings or final newlines alone while the nearer file made it
  rewrite them.
- **bro init** and **--write** remove a `stylebro-whitespace` block from a nested .editorconfig that no
  longer decides those settings, so running them again stays clean.
- **bro migrate --write** asks MSBuild which projects run StyleCop whenever a repository has several projects, so an
  import the tool can't read (like `GetPathOfFileAbove($(MSBuildThisFile), $(MSBuildThisFileDirectory)..)` in
  Library.Template repositories) no longer makes a folder look StyleCop-free. Such a folder got no settings before and
  showed hundreds of StyleBro warnings that contradicted StyleCop. The unquoted `GetPathOfFileAbove` form is now read too.
- The report line for folders without StyleCop now says what really happens there (StyleBro's rules still report in the
  build; remove the block's `# No StyleCop ran in` line to format them) instead of pointing to `init`.
- **bro init --write** run again gives a byte-identical .editorconfig: the using-order comment without a
  setting line was written a second time.
- **bro format --verify-no-changes** counts only files formatting really changes: not files a build step
  generates during the check, and not files whose only change is a line ending.
- The preview no longer puts the `StyleBro.Analyzers` PackageVersion between a comment and the item it describes.
- `init` names the Directory.Build.props the projects import in its `<EnforceCodeStyleInBuild>` hint
  (`src/Directory.Build.props` when that's the only one).
- **BRO1519** no longer asks for a blank line after a `}` that ends a pattern or expression when the next line continues
  it (`?`, `??`, `?.`, a binary operator, `with`, `switch`, `as`, `when`, ...), nor between a property's accessors and
  its initializer. With CSharpier, the blank line before a conditional's `?` came back on every run.
- **BRO1303** renames drop a `this.` that only told the field apart from a parameter of the old name
  (`this.pet = pet` now becomes `_pet = pet`, not `this._pet = pet`), unless `dotnet_style_qualification_for_field`
  is `true`.
- **BRO1124** keeps `x.Item1` when the element is named `x` (a name inferred from another lambda's `x`): `x.x` was no
  clearer.
- **bro init** and **--write** find CSharpier when its tool manifest or `.csharpierrc` sits next to the
  solution or a project rather than at the repository root. When CSharpier formats only one folder, its settings go into
  that folder's section.
- With CSharpier, **bro init** and **--write** no longer write a using sort key: CSharpier sorts usings
  itself, and `dotnet format` ordered some of them differently.
- Files **bro** creates (such as `Directory.Build.props`) get the line endings of the files around them.
- **--diff** adds StyleBro's package version where central package management is turned on by a `Directory.Packages.props`
  below the root, and never into a conditional `ItemGroup`.
- **--diff** leaves packages.config projects out and previews the others, instead of refusing the whole repository.
- **`bro format`** and **`baseline`** leave a folder CSharpier formats out of the whitespace pass when
  CSharpier only formats part of the repository (`IDE0055 = none` in that folder's section), so the two formatters no
  longer undo each other's changes forever (`[A] [B]` against `[A][B]`).
- **BRO1603-BRO1607, BRO1617-BRO1619** leave documentation comments with malformed XML alone: `IComparable<T>` written
  in prose no longer became `IComparable.<T>`.
- **BRO1603** removes trailing spaces after the period it adds, so the line doesn't end in `. `.
- **BRO1604/BRO1605** treat a private or internal getter like StyleCop does: `{ private get; set; }` is documented
  "Sets ...", not "Gets or sets ...".
- **BRO1606** accepts the standard constructor sentence with a spaced cref (`<see cref = "X" />`) instead of rewriting it.
- **`--diff`** no longer tells you to run `dotnet restore` for `packages.config` projects it left out on purpose.
- **`bro init`** writes StyleBro's rules by id where the `.editorconfig` silences analyzers in bulk
  (`dotnet_analyzer_diagnostic.severity = silent`). Before, `format` skipped every StyleBro rule and said clean, while
  the build reported them. **`bro format`** no longer says clean while such a setting hides rules from it.
- **`bro init`** and **`--write`** recognize CSharpier in every folder that configures it, not only the
  first one, so StyleBro's layout rules no longer fight CSharpier in the other folders.
- StyleBro no longer reports code that a project compiles from a git submodule. `format` can't fix it there.
- **`bro migrate`** names a StyleCop reference that comes from a git submodule and says where the package swap
  goes instead (the repository's own `Directory.Build.props`). After that swap, a second `--write` recognizes the
  migrated setup instead of calling it leftovers.
- **`bro init`** no longer makes BRO1404 add `public` to interface members when the `.editorconfig` says
  `dotnet_style_require_accessibility_modifiers = always`, nothing enforces that in the build, and the code's interface
  members don't have access modifiers.
- **`bro init`** writes the same block when it runs a second time on a repository that uses CSharpier, and
  doesn't warn about IDE0055 crashes where CSharpier turns IDE0055 off.
- **`bro init`** explains how to keep a setting it overrides (such as `end_of_line = crlf` in files that
  are mostly LF): set it again below the block.
- **`--diff`** really leaves `packages.config` projects out of the patch. **`packages.config`** files written on one
  line get StyleBro's entry on a line of its own, and NuGet's `EnsureNuGetPackageBuildImports` target is found when its
  name is in single quotes, so a second target no longer replaces it.
- **BRO1405** and **BRO1410** no longer delete a comment inside the parentheses they remove: `(x + 1 /* one */)` becomes
  `x + 1 /* one */`, and parentheses with a `//` comment and a line break inside them are left alone.
- **BRO1104** and **BRO1123** leave code with a comment inside (`new Guid(/* none */)`, `ValueTuple<int, /* id */ int>`)
  alone instead of dropping the comment.
- **BRO1603** no longer adds a period after a quoted char or string literal (`Default is ',';`).
- BRO1603-BRO1607 and BRO1617-BRO1619 leave documentation with a bare `<` (`when a < b`) alone: BRO1603 put a period in
  the middle of the text (`a. < b`).
- Expanding a one-line method body (BRO1508/BRO1509) puts an `if`/loop body without braces on its own line when
  `csharp_preserve_single_line_statements = false`, so `format` no longer needs another run for it.
- `bro init` and `format` also handle a bulk severity below warning in a folder's own `.editorconfig`
  (`tests/.editorconfig` with `dotnet_analyzer_diagnostic.severity = none`): `init` sets StyleBro's rules by id there, and
  `format` no longer says clean while the build reports them.
- Code from a git submodule or a source generator is left alone also when a project compiles it through a relative
  path (`<Compile Include="..\..\ext\lib\*.cs" />`).
- **BRO1301-BRO1311** no longer rename where IDE0042 (deconstruct variable declaration) would then report, when IDE0042
  is a warning or an error through a category (`dotnet_analyzer_diagnostic.category-Style.severity`) or for every rule
  (`dotnet_analyzer_diagnostic.severity`). IDE1006's severity is read the same way when StyleBro follows your naming rules.
- `init` counts IDE0040 and IDE0009 as enforced through a category or bulk severity
  (`dotnet_analyzer_diagnostic.category-Style.severity = warning`), and no longer counts
  `dotnet_style_require_accessibility_modifiers = always` without a build severity as enforced: **BRO1404** then no
  longer adds `public` to interface members the build never asked for.
- `init` no longer reads `} else return x;` inside a one-line method as "else on the `}` line", which turned the code
  into K&R braces.
- `init` no longer sets the preset version in `Directory.Build.props` files of example folders whose projects aren't in
  a solution and don't use StyleBro.
- Report texts: a Sonar rule in a `<NoWarn>` says the option it no longer sets, and the line-ending verdict for files git
  converts no longer starts with a count of the other files.
- BRO1514-BRO1516 put the braces of a nested statement on its owner's line (`for (...) if (x) y;`) at the wrong
  indentation, so `dotnet format` needed a second run to fix it.
- BRO1508/BRO1509, with `csharp_preserve_single_line_statements = false`: expanding a one-line body now also lays out
  nested statements (`while (n > 0) if (x) return n;`, `using (s) lock (gate) ...`) and a one-line `switch` the way the
  SDK's formatter does, so one `dotnet format` run is enough.
- BRO1601 below a `//` comment and a blank line: the blank line now goes whether BRO1601 or BRO1506 runs first (it
  stayed when BRO1601 ran first).
- BRO1603 no longer adds a period after text ending in `,` or `;` (`1033 => "en",` became `"en",.`).
- BRO1609 no longer unwraps a `<placeholder>` at the top level of a comment, which left its text outside any element.
- **BRO1301-BRO1314** leave names where the code turns the SDK's naming rule off: after `#pragma warning disable IDE1006`
  or with `[SuppressMessage("Style", "IDE1006")]` on the symbol or what contains it (a file copied from another project
  keeps its names).
- **BRO1309** (and the other renaming rules for properties, events, methods, parameters, locals, type parameters and
  namespaces) no longer renames a symbol to a name your enforced `dotnet_naming_rule` rejects: `private int _format { get;
  set; }` stays when your rule asks for `_camelCase` private properties, instead of becoming an IDE1006 build error.
- **BRO1311** renames element uses inside a tuple literal that is returned or assigned as another tuple type
  (`return (false, map.First().documentName);`); before, the rename was dropped and the warning stayed.
- **BRO1311** renames the element names of one tuple type all or none: it no longer leaves `(int Total, int max)` when
  `max` has to keep its name.
- **`bro baseline`** records the right lines when `dotnet format`'s whitespace pass would split lines
  (`if (x) y;`): it reads each pass on its own, so the build is clean after a baseline.
- **`bro format`** on a solution in its own folder (`solutions/App.slnx` listing `../src/...`) formats the
  projects instead of reporting "clean" without touching them; a path that doesn't exist says "Not found".
- **`bro format`** removes usings its fixes leave unused (IDE0005) where the repository enforces IDE0005.
- **`--diff`** previews show changes to files git tracks under an ignored folder, and say when an ignored `nuget.config`
  isn't in the copy.
- **BRO1603** no longer checks the documentation of events and finalizers, like StyleCop's SA1629.
- **BRO1603** no longer adds a period after an inline element whose own text ends a sentence
  (`<paramref name="name">The name of the request.</paramref>`), or after a last word ending in a period and digits
  (`HTML 5.0`).
- **BRO1603** and the other documentation rules that edit text leave a comment with a bare `&` alone: it's malformed
  XML (the compiler reports CS1570), like a bare `<`.
- **BRO1308** no longer joins a name starting with `_` in the `_camelCase` style (`_signature_size` stays), like
  StyleCop's SA1310.
- **BRO1126** keeps how a type argument names a built-in type in a using alias: `System.Char` no longer becomes `char`,
  which IDE0049 reported under `dotnet_style_predefined_type_* = false`. Same as StyleCop.
- **BRO1303/BRO1306** rename a nested type's field when an enclosing type has a member with the new name, and a field
  named after its own type (`SimulatorEvent _simulatorEvent` -> `SimulatorEvent`); both were kept before.
- **BRO1309** keeps a static property only for a string that reaches it by name (`GetProperty("nbc")`), not for a URL or
  file name that contains the word.
- `bro format` lists a tuple element (**BRO1311**) kept because another element of its tuple type keeps
  its name as kept with that element, instead of repeating the other element's reason.
- **`init`** recognizes CSharpier set up above a folder whose `.editorconfig` says `root = true` (CSharpier in
  `.config/dotnet-tools.json`, code under `src/`): that folder gets the CSharpier settings, and `format` says to run
  CSharpier afterwards.
- **`init`** and the preview write `<StyleBroPreset>` and the package reference in the file's own indentation, and the
  package version next to the other analyzers in `Directory.Packages.props`.
- **`format --verify-no-changes`** no longer says "Clean" when findings are left that no fix changes (like IDE0049 in a
  using alias): it lists them and fails.
- **`format`** says how to format a project no solution configuration builds; its note on generated files leaves out
  resource and WinForms designer files and mentions other projects' generated files; and it no longer says fixes still
  run in a folder that turns every analyzer off.
- **`init`**'s report: clearer labels for primary-constructor parameter names, and no "nothing enforces it in the build"
  when `EnforceCodeStyleInBuild` is on.
- **`bro format`** no longer breaks multi-line strings that hold lines starting with `#` (`#pragma warning
  disable` in a source generator's test input) inside an `#if` branch: the SDK's whitespace formatting moved them to
  column 0 where the branch is inactive (CS8999 where it's compiled). Such lines are now held in place, and the files
  named.
- **`bro format`** and **`--verify-no-changes`** no longer say "clean" when a code fix failed ("Failed to
  apply code fix ..."): they name the rule and the error and exit with 2.
- **`bro format`** keeps each file's line ending when `end_of_line` is `unset` or missing for it: the SDK's
  whitespace pass rewrote LF files to CRLF on Windows on every run.
- **`bro format`** stops after an option `dotnet format` doesn't accept instead of repeating its usage text
  for every framework, names the projects that really run apart ("Shared files: Lib.Aot | Lib1, Lib2"), says nothing about
  checks it didn't need to repeat, and `--verify-no-changes` prints what it checks before the progress lines.
- **`bro format`** no longer asks to rebuild generated files no build writes: a Visual Studio custom tool's
  output (T4 templates without `TransformOnBuild`, `LastGenOutput`) or a file with just an `<auto-generated>` header.
- **`--diff`** works with `RestoreLockedMode` always on (the copy restores with locked mode off; its patch shows the
  updated `packages.lock.json` files), and no longer says the patch includes the package for `packages.config` projects
  it leaves out. A restore that fails with NU1004 says how to update the lock files.
- The rename fixes (**BRO1301**-**BRO1309**, **BRO1409**) work in a project that references a source generator project
  that isn't built yet. Before, `dotnet format` failed with "Unexpected value 'UnresolvedAnalyzerReference'" and
  renamed nothing.
- **BRO1123** no longer rewrites the `ValueTuple<A, B>` a pattern tests for (`x is ValueTuple<T, T> pair`,
  `case ValueTuple<A, B> p:`): written as `(T, T)`, it is a different pattern and didn't compile.
- **BRO1137** keeps a lambda's last `return;` when the lambda can't reach its end without it
  (`_ => { throw new X(); return; }`): removed, the call could become ambiguous between overloads.
- **BRO1303** and **BRO1306** no longer skip a field because a base class has a private member with the new name. The
  build reported such fields and `dotnet format` didn't fix them.
- **BRO1515** and **BRO1602** add the blank line **BRO1504** wants above a `//` comment that now follows a `}`, so one
  `dotnet format` run is enough.
- **BRO1515** and **BRO1516** indent the body of a statement that shared a line with `else` (`else switch (x)`) like
  the statement once it has its own line; before, the next `dotnet format` run moved it back a level.
- **BRO1518** removes blank lines between a trailing comment (`x(); // note`) and a `}`; before, no rule did, until a
  fix added braces there and the next run removed them. **BRO1519** no longer adds a blank line before an empty
  statement's `;` that **BRO1101** removes, and **BRO1504** leaves a comment alone that continues the trailing comment
  above it in the same column. Each of these took a second `dotnet format` run.
- **BRO1139** lines up an `if` that the whitespace formatting had left unindented below `else` with the joined
  `else if`; before, the next `dotnet format` run moved its lines back.
- **BRO1606** writes a generic class link as `Cache{TKey, TValue}`, with a space after the comma.
- `format` now also protects `#` lines in multi-line strings under `#elif`, under conditions like
  `NET8_0_OR_GREATER && !NET10_0_OR_GREATER`, and a string's own `#if`/`#endif` lines (CS8999 after formatting).
- After a killed `format`, a `dotnet format` still running no longer leaves swapped `#` characters behind, and a
  temporary `.stylebro-format-*.slnf` it left is removed by the next run.
- On Linux and macOS, `format` read no file's `#if` symbols, so frameworks whose code differs could share a run (code
  only the other framework compiles wasn't formatted).
- A CR inside a string literal (a verbatim regex) no longer counts as an old Mac line ending.
- A code fix of another analyzer that fails is no longer reported as a StyleBro bug.
- The baseline now also covers files a project compiles from another folder (`<Compile Include="..\Lib\A.cs" />`): their
  baselined findings still failed that project's build.
- `--verify-no-changes` deletes its temporary copy, and copies a killed preview or verify left behind are removed
  after a day.
- **Naming rules (BRO1303, BRO1306, BRO1309)**: a name in a checked-in snapshot or approval file (`*.verified.*`,
  `*.received.*`, `*.approved.*`) keeps it, like a name in a string: renaming a DTO's property failed Verify's snapshot
  tests. `bro format` lists such findings as kept, with the file.
- **Naming rules**: a member used through `dynamic` (`((dynamic)x).Field`) keeps its name; renaming it compiled but failed
  at run time.
- **BRO1306**: a public or internal field of a private class is now also kept when its name is inside a string (a script
  engine reading `host.ByteField`).
- **BRO1306**: a private `static readonly` field is no longer renamed against the repository's own naming rule when the
  SDK's naming rule order gives it a `_` prefix (the renamed field got IDE1006).
- **BRO1309**: a class with a snake_case property (`line_count`) keeps all its property names, instead of renaming the
  others and mixing casings.
- **Naming rules**: a name used from Visual Basic code keeps it, with the reason "Visual Basic code uses the name"
  (StyleBro doesn't edit VB code); it said "the new name would mean something else" before.
- **BRO1151** leaves the array in a project that targets frameworks before and from .NET 9 (`net8.0;net10.0`) when the
  call goes to a library: on .NET 9 and later, `string.Join`, `string.Format` or `Task.WhenAll` with the elements
  bind to a `params ReadOnlySpan<T>` overload instead. In any multi-targeted project it also leaves calls to a method
  whose type has `#if` in it, where an overload can hide.
- **BRO1508** leaves a one-line block inside a one-line block whose rule is off (`int M() { foreach (...) { ... }
  return s; }` with **BRO1509** off). Expanding it alone took a second `dotnet format` run and put two statements on
  one line.
- **BRO1504**'s fix removes the blank line below the comment when **BRO1506** is on, so a comment below a `}` with a
  blank line under it ends up the same whichever fix runs first.
- **BRO1505** puts its blank line below an `#endif`, not above the `#else` or `#endif` before the element.
- `format` no longer breaks an interpolated raw string (`$"""`, `$$"""`) whose first line starts with `#` inside an `#if`
  branch (CS8999 in the build that compiles the branch): every string shape is protected now.
- A CR without an LF inside a string (a verbatim regex) no longer becomes a line break where `.editorconfig` sets
  `end_of_line`: `format` leaves such files out of the whitespace and style passes and says so.
- Files a project compiles only for some target frameworks (`<Compile Include="..\Polyfills\*.cs">` under a framework
  condition) are formatted and checked now; `--verify-no-changes` said clean while the build failed on them.
- StyleBro rules listed in `<WarningsNotAsErrors>` are fixed and checked now (plain `dotnet format` skips them while the
  build reports them as warnings).
- `format` run from the repository's root removes unused usings where only a folder's `.editorconfig` enforces IDE0005.
- A baseline named with `<StyleBroBaseline>` outside the code (`eng/stylebro.baseline`) now gets the findings `format`
  keeps, and covers them in the build (`../src/...` entries). With `<StyleBroBaseline>none</StyleBroBaseline>`, `format`
  and `--verify-no-changes` say that the build still reports the kept findings. The baseline message counts entries.
- EF Core migration designers (`*.Designer.cs` next to a migration) are no longer listed as generated files to rebuild.
- `init` (and `init --diff`) no longer spends minutes evaluating projects: reading `dotnet msbuild`'s output waited for
  thread-pool threads. `init --diff` says it can't choose a solution before evaluating anything.
- `format` with several runs (multi-targeted projects) checks them all at once first, also in the first run, and formats
  only the runs with findings: a repository with nothing to fix no longer waits for one `dotnet format` after another.
- `format --files` lists the changed files relative to the repository, not the project.
- A relative `<StyleBroBaseline>eng/stylebro.baseline</StyleBroBaseline>` is found by the build now (it looked in each
  project's folder, so `format` said clean and the build failed). The docs recommend `$(MSBuildThisFileDirectory)`.
- The preview no longer removes `using` directives code needs when a project compiles files from outside the
  repository: the copy gets those files. Where they're missing, `format` says so and leaves IDE0005 out.
- The note about generated files checked in no longer names a `Settings.Designer.cs` (Visual Studio writes it) or a
  `.g.cs` file no build step writes.
- `format` no longer goes back and forth on lines whose if/else or loop body `#if` decides
  (`#if NETFRAMEWORK` ... `else` / `#endif` / `builder.Append(v);`): each framework's or configuration's run indented
  them its own way, so it never settled. It now leaves their indentation alone and says so (`Indentation kept: ...`).
- Where CSharpier puts back what a fix changed, `format` and `--verify-no-changes` name the rules whose edits it undoes,
  and `format` no longer says "clean" right before "Not clean". The preview (`init --diff`) says not clean and exits 2
  there too, like `format`.
- `format` no longer says "style and analyzer fixes still run there" about a folder it leaves alone.
- The hint about warnings loading the workspace names the run that had them and the command that shows them
  (`--verbosity diagnostic` on the same `bro format` command).
- `format <solution in a subfolder> --files <file elsewhere in the repository>` no longer says the file "isn't under"
  the solution's folder.
- `--verify-no-changes` with CSharpier runs its CSharpier check after StyleBro's, not before the first line.
- **`bro init`** writes `<StyleBroPreset>` on a line of its own into an empty `<PropertyGroup></PropertyGroup>`
  (CSharpier's check rejected the closing tag on the same line), and keeps a self-closing `<Project Sdk="..." />` intact.
- **`bro init`** counts only compiled C# files: Verify snapshots (`*.verified.cs`, `*.received.cs`) and files a
  project with its own Compile items leaves out no longer decide the conventions.
- **`bro init`** no longer crashes on a file it can't read (a log another process holds open in the
  repository).
- **`bro migrate --write`**: with SA1633 off, the report no longer lists SA1634-SA1641 as not enforced (StyleCop checks
  no header then), and it mentions that StyleBro.Analyzers can go once into the root Directory.Build.props when every
  project references StyleCop.
- **`bro init`**'s report names the doc elements found out of order, and mentions the template's empty lines
  only when there is a file header template.
- **BRO1604** compares a property summary's first words ignoring case, like StyleCop: `Gets or Sets the key` is right
  (it became `Gets or sets or Sets the key`), and a lower-case `gets the count` is replaced instead of prefixed.
- **BRO1605** no longer asks for `Gets` when an `internal` setter belongs to a property only its assembly sees (an
  `internal` class's): like StyleCop's SA1623, it says `Gets or sets`, so the two no longer contradict each other.
- Renames keep names a XAML file uses as element or attribute names from the project's own namespaces:
  `<local:BoolConverter/>`, `local:TagBehavior.Key="main"`, `<local:Badge Caption="hi"/>` (a renamed type or
  property there broke the build). Letters in path markup (`Data="M0,0 z"`) no longer keep a name.
- **BRO1616** leaves a `///` comment that documents nothing (below an attribute) to BRO1602, so `dotnet format` gives
  the same result every time.
- **BRO1305** drops a leading underscore first: `_TVertex` becomes `TVertex`, not `TTVertex`.
- **BRO1306** keeps a field whose new name would differ only in case from another member (`_horizontalgap` next to
  `HorizontalGap`).
- The assembly attributes the build writes under `obj/` no longer count as strings that keep a name.
- **BRO1001** with `stylebro_member_order_scope = fields_first` no longer leaves a type unsorted because its first member
  has a comment above it.
- **BRO1515** with `stylebro_layout_by = csharpier` no longer wants braces around a long `return` of one string, which
  CSharpier keeps on one line.
- **BRO1514-BRO1516** leave an empty statement as the body (`foreach (var x in xs) ;`) to the compiler's warning, like
  BRO1101; wrapped in braces, it took a second `dotnet format` run.
- **BRO1405** removes nested parentheses (`F(((x * y)))`) in one run, and **BRO1523** looks through the parentheses
  BRO1405 removes, so a chain they joined is laid out in the same run.
- Old-style projects (`TargetFrameworkVersion`) in a solution with multi-targeted projects got no `format` run, and
  `--verify-no-changes` said clean without checking them.
- When `dotnet format` couldn't load a project for the framework whose run also stood in for another one (netstandard1.3
  for netstandard2.0), neither was checked; the other framework now runs instead. Code that no run could load makes
  `--verify-no-changes` say `Not verified` instead of clean.
- `--verify-no-changes` said clean where `end_of_line` asks for another line ending than a file has; `format` converts
  those files.
- `--diff` showed every file `format` changed with Windows line endings in a repository with LF files.
- `--verify-no-changes` showed a failed Release build only when that build had no findings; it now shows the failure
  next to the findings and says baselining them doesn't fix it.
- `baseline`'s "Not in the baseline: N formatting changes" says that the baselined findings on those lines show up again.
- **`bro format`**: if it's killed while the `#` of protected string lines is swapped for another
  character, the next `bro` command puts the `#` back and says `Restored: <file>`. `format` and
  `--verify-no-changes` stop with a clear message on a swapped character they can't account for.
- **BRO1311** no longer keeps a project's tuple element names because a tuple in another project with the same element
  names can't be renamed: each project's elements are judged on their own then.
- StyleSharp's SST1108 (files of commented-out code) no longer counts as StyleCop's SA1108.
- The migration report names StyleSharp where StyleSharp runs and says "everywhere except" when that list is the shorter
  one, and no longer prints a Sonar section for Sonar ids that are all turned off with no Sonar package.
- `bro migrate --write` gives a `root = true` folder that turns every analyzer off the same opt-out block as `init`,
  instead of turning rules on there.
- `bro` says when the repository's own `dotnet_sort_system_directives_first` doesn't match the order of
  its using directives; when the team's ordering rule (SA1210, or StyleSharp's case-sensitive SST1210) accepts that
  order, `format` leaves those files alone instead of sorting them into what that rule reports.
- `bro migrate --write` no longer adds suppressions for rules its settings turn off (StyleSharp's SST1462 reported
  them).
- Code in a folder without a project of its own (shared polyfills linked into the projects) gets the migrated settings
  of the projects that run StyleCop, instead of StyleBro's defaults.
- `bro format` no longer says "another formatter's code" where IDE0055 is just off, and no longer asks to
  rebuild a generated file only a target framework it didn't build compiles.
- **BRO1151** no longer passes elements to a `params ReadOnlySpan<T>` overload in a project that also targets a
  framework before .NET 9, where that overload doesn't exist and the build broke.
- **BRO1120** keeps a lone `//` in an otherwise empty block (an empty `catch`), which marks it as empty on purpose.
- **BRO1508/BRO1509** with `csharp_preserve_single_line_statements = false` and BRO1514 off add the braces BRO1515 and
  BRO1516 want for the statements the expansion makes multi-line, so `dotnet format` needs one run, not two.
- `bro init` and `bro migrate --write` run from a subfolder (`src/`, or a `build/` folder holding the
  solution) work on the repository's root when the build reaches past that folder. Before, `init` from `src/` created a
  `src/Directory.Build.props` that hid the root's (StyleBro, `TreatWarningsAsErrors` and `Nullable` were lost), and from
  `build/` it wrote settings no project read. A `Directory.Build.props` StyleBro creates below another one now imports it.
- `bro init` reads the root `.globalconfig` like `.editorconfig`: an enforced `this.` (IDE0009) and the
  team's IDE0055 severity are kept. With `AnalysisMode` `All` and `EnforceCodeStyleInBuild`, a `this.` setting without a
  severity counts as enforced.
- `bro init` turns the built-in rules off by id in a folder whose `.editorconfig` turns the Style category
  off; the root block's IDE0055 used to reformat it.
- `bro migrate --write` with StyleCop spacing rules off (SA1000, SA1003, SA1008, SA1009): the SDK spacing option that
  keeps your code's spacing is written (`if(x)`, `a+b`, ...), and the report names what IDE0055 still changes where no
  option can express it (`new ()`).
- `bro migrate --write` for StyleCop 1.1.x: BRO1401 stays off when your code has multi-line switch expressions without
  a trailing comma (1.1.x's SA1413 doesn't check them).
- `bro migrate --write` no longer writes a block into a `root = true` folder without C# files (an npm app), and
  carries suppressions over only where StyleCop ran.
- `bro migrate --write` with the repository's own `dotnet_sort_system_directives_first`: only files whose sorted
  usings StyleCop's ordering rules would report are left unsorted; the others are sorted (StyleCop accepts that order).
- Report texts: the final-newline note says which files `format` changes; "Kept your style" names what was written;
  StyleSharp-only repositories get "Generated from this repository's StyleSharp settings"; the Sonar note no longer
  credits a StyleCop setup in `init`.
- `bro migrate --write` finds StyleCop referenced only for one target framework of a multi-targeted project
  (`<ItemGroup Condition="'$(TargetFramework)' == 'net6.0'">`). Such projects counted as "No StyleCop": they got no
  settings, `format` left them alone, and the build failed.
- `bro init` no longer turns IDE0055, IDE0036 and IDE0049 back on by id when your root `.editorconfig` turns
  the Style category (or every analyzer) off. Your own path sections (`[src/**/*.cs]` with IDE0055 `none`) keep their
  values: the block repeats them at its end.
- `bro init`: a folder that turns every analyzer off no longer gets its usings sorted by `format`. The using
  order key goes into a section for the rest of the code.
- `bro init` and `--write` no longer count usings inside `#if` as out of order (`format` doesn't sort across
  directives), no longer pin a preset version in fixture folders that only their own solution builds, and carry
  `SA1300` suppressions on types and members over without the namespace rule (BRO1312).
- `bro init --diff` on a StyleCop repository says to run the migration instead, without starting a preview.
- Report texts: `init` says when IDE0009 is enforced only in `.globalconfig` (`format` doesn't fix it there), no longer
  says `format` formats whitespace when your IDE0055 is `none`, and names "your settings" for an IDE0055 set in
  `.globalconfig`; the migration no longer calls missing StyleCop settings leftovers.
- **BRO1001** with BRO1505 off (CSharpier): the sort adds the blank line BRO1519 wants after a moved member's `}`, and
  doesn't leave one between members that no longer need it, so one `dotnet format` run is enough and the result no
  longer depends on the order the fixes run in.
- Rules that need type information (such as **BRO1104**, **BRO1106**, **BRO1122**) now also report in the body of a
  partial method whose declaration a source generator wrote (CommunityToolkit.Mvvm's `OnTitleChanged`).
- **BRO1603** no longer adds a period after a line of code that ends in a word after an operator (`=> 0`, `x == null`).
- **init with CSharpier:** IDE0036 (modifier order) is off too: CSharpier orders modifiers itself, in another order
  (`file static class` and `static file class` went back and forth).
- **init, compact members:** when members have no blank lines between them and BRO1505 goes off, BRO1519 goes off too;
  it added a blank line after every method's `}` anyway.
- **init, one-line blocks:** method bodies and statement blocks with one short statement on one line
  (`int Next() { return ++count; }`, `if (x) { return; }`) are counted; when the code writes them that way, BRO1509 and
  BRO1508 go off (and `csharp_preserve_single_line_statements = true` keeps IDE0055 from splitting them).
- **init, doc element order:** doc comments with params or returns next to `<remarks>`, `<exception>` and the like are
  counted; when the code puts them in another order, BRO1619 goes off instead of reordering them.
- **init:** folders whose own `.editorconfig` turns every analyzer off or says `generated_code = true` no longer count
  towards the conventions.
- **init:** a `Directory.Build.props` above the git repository is no longer imported into a new one in the repository.
- **Report texts:** the line-endings note no longer says IDE0055 is lowered where it's off (CSharpier); a second
  `bro migrate --write` says suppressions were already carried over; the note about suppressions of whitespace
  rules says when it matters.
- **Preview:** StyleBro's `GlobalPackageReference` goes next to StyleCop's instead of into a new `ItemGroup` at the end.
- **BRO1311** no longer renames tuple element names in the signature of a public operator or conversion operator, or in
  a public type's header (record and primary constructor parameters, base types, constraints), when
  `stylebro_rename_public_api` is false.
- **BRO1306** now also renames the public fields of a private nested type when their name only appears in another
  type's `#if`-excluded code (that code can't use them).
- **BRO1509** and **BRO1001** with BRO1505 off no longer add BRO1505's blank lines when they expand a type written on one
  line, so the result no longer depends on the order the fixes run in.
- **BRO1405** and **BRO1108** give the same result in any order on a leading lambda in parentheses.
- **BRO1104** and **BRO1150** write `default (T)` and `Array.Exists (...)` with a space where
  `csharp_space_between_method_call_name_and_opening_parenthesis = true`, so a second `dotnet format` run changes nothing.
- **bro** (`--write`): a second run after removing StyleCop.Analyzers and `stylecop.json` no longer turns
  settings back to StyleCop's defaults (documentation of internal elements, using placement, file header company).
- **bro** (`--write`) writes `csharp_prefer_braces = true` while SA1503 is on, also when your
  `.editorconfig` says `when_multiline`: braceless one-line statements stay reported.
- **bro init**: an IDE rule's own severity (`dotnet_diagnostic.IDE0011.severity = warning`) now counts over
  the option's suffix (`csharp_prefer_braces = true:silent`), as in Roslyn; and when the block turns off a built-in rule
  you turned on (IDE0011, IDE0040, ...), the report says which StyleBro rules enforce its setting instead.
- **bro init**: doc comments with only `<param>`, `<typeparam>` and `<returns>` count for the doc element
  order when there are few others (BRO1619 reordered `<returns>` above `<param>` everywhere).
- **Renames (BRO1306, BRO1309)** keep a member or type whose name a XAML file (`.xaml`, `.axaml`) uses: a binding path
  (`{Binding title}`, `DisplayMemberPath="label"`), an event handler or `x:Class`. Bindings are resolved by name at
  run time, so the rename compiled but left the binding empty. The finding stays, with the reason.
- **BRO1001** keeps the order of the fields and properties of a partial type that generated code extends (a source
  generator's part): a generator such as Serde writes them in declaration order, so sorting changed the JSON.
- **BRO1519** no longer asks for a blank line before an accessor that starts with an attribute
  (`[MethodImpl(...)] set => ...`), the same as without one; CSharpier removed it again.
- **BRO1504** no longer asks for a blank line between an element's attributes and a comment right below them (also
  after BRO1602 turned a misplaced `///` there into `//`).
- **BRO1311** no longer reports tuple element names an implementation has to take from a public interface in the
  solution that isn't renamed (`stylebro_rename_public_api = false`); they were reported and then never fixed.
- **BRO1303-BRO1308**: a field that keeps its name (an attribute such as `[ThreadStatic]`) no longer blocks another
  field from getting that field's would-be new name.
- `--verify-no-changes` said clean when the Release build of a multi-targeted project with an in-solution source
  generator reported findings: none of them was counted.
- `--verify-no-changes` said clean when that Release build failed (an error in another project, a compile error); it
  now says not clean and shows the first errors.
- `baseline` and `format` write a project's findings to the baseline its build reads, also when `<StyleBroBaseline>` is
  set in a nested `Directory.Build.props`; projects that read different baselines each get their own findings.
- `baseline` run on a solution in a subfolder no longer adds entries for files outside that folder.
- `baseline` with nothing to write names the reason that applies (`none`, or a path it can't resolve).
- On lines whose indentation `#if` decides, `format` now fixes the rest of the line's spacing (`v+1` to `v + 1`) and
  `--verify-no-changes` reports it.
- The CSharpier hint in `--verify-no-changes` says that `format` also runs CSharpier on files with other line endings,
  and the preview names the lines whose indentation `format` keeps.
- **`bro init`** run from a subfolder works on the repository's root also when the root has only a
  solution that lists the folder's projects, when a project in the folder references one outside it, and when the folder
  has no project at all; before, it wrote a separate `.editorconfig` and `Directory.Build.props` there.
- **`bro init`** counts `using Helper;` (a type in an enclosing namespace) and `using Text;` inside a
  `System.*` namespace as relative usings, so BRO1126 no longer rewrites code that mostly uses them.
- **`bro init --style microsoft`** says that private static fields without a prefix get the style's `s_`
  (it said "BRO1307 is off" and `format` then renamed them), and drops lines of an earlier `init` that the style repeats.
  The summary names both static field keys when it writes both.
- **`bro init`** and **`--write`** no longer stop on a C# or project file another program has locked.
- **BRO1309** and the field rules (BRO1303, BRO1306-BRO1308) no longer rename a `new` member that hides a base member by
  its name (`private new Settings configData => ...`): renamed, it hid nothing and the build failed on CS0109 with
  warnings as errors. A base member that a derived type hides that way keeps its name too.
- **BRO1131** and **BRO1301** give the same code in either order: when a local named like a method (`var Download = ...`)
  is renamed, the `this.` BRO1131 wrote for `base.Download(...)` goes too where `dotnet_style_qualification_for_method`
  is false.
- **BRO1305** renames a type parameter in the crefs of its own type or method too (`<see cref="Node{_TVertex}"/>`), so
  the standard constructor summary is right in the same run.
- XAML files keep fewer names by mistake: words in text for people (`Text="... window width"`, `ToolTip`, `Content`,
  `Header`, `Title`) and the `x` of `{x:Static ...}` or `{x:Null}` no longer keep a member's name. A type or property
  named by a XAML element or attribute is listed as such ("renaming would break the XAML build").
- In a git submodule's own checkout, StyleBro reports the submodule's findings again: the superproject's `.gitmodules`
  above it named the submodule's own folder, which hid all of them.
- **`bro init`** and **`--write`** treat `end_of_line = unset` like no setting: when the C# files agree,
  they write their ending, so `format` no longer writes your OS's ending into the lines it changes while the build's
  IDE0055 reports them.
- **`bro migrate --write`** no longer crashes halfway on a file another program has open: it names the file and
  writes nothing, so the migration is never half done.
- **`bro init`** says which files it couldn't read (open in another program) instead of quietly counting
  without them; code whose license file can't be read is left out and not marked as vendored.
- **`bro init`** no longer takes a license template's placeholder (Apache's
  `Copyright [yyyy] [name of copyright owner]`) for a copyright holder, which marked your own folders as vendored code.
- **`bro migrate --write`** no longer warns that a project copies an `.editorconfig` when it only shows it in the
  solution (`<None Include="..\.editorconfig">`); the SDK's item types copy at most into the output folder.
- **`bro init`** leaves `///` comments that document nothing out of the one-line summary count, and says
  how many private fields follow the naming rule that decides their style.
- **BRO1302** no longer renames parameters Harmony binds by name: a name starting with `__` (`__instance`, `__result`,
  `___field`) and every parameter of a Harmony patch method stay as they are. Renamed, they compiled and the patches
  stopped working.
- **`bro format`** formats the hand-written `.cs` file next to a T4 template that writes another file
  (`<#@ output extension=".Generated.cs" #>`); it used to skip it, and the build then reported its findings.
- **BRO1303** and the other field rules keep a private field whose exact name, like `_root` or `m_count`, is any string
  in the solution (a test listing field names and reading each by reflection), not only an argument of `GetField(...)`.
- **`bro format`** formats a project your solution doesn't list but another project references (the build
  compiles it, so its findings failed a build with warnings as errors after a clean `format`).
- **`bro format`** (and `--verify-no-changes`, `--diff`) no longer stops when a project in the solution needs
  a workload that isn't installed, such as an Android, iOS or Browser head (`dotnet restore` failed with NETSDK1147 and
  nothing was formatted). Those projects are listed under "Leaving alone" and the rest of the solution is formatted; run
  `dotnet workload restore` and `format` again to format them too.
- **init** no longer marks a whole repository as vendored code when its root has several license files with different
  holders (a GPL `COPYING` next to the team's own `LICENSE.md`): every root license counts as the team's own, and the root
  folder is never vendored. Before, init wrote `generated_code = true` for everything and counted no conventions.
- **init** and **migrate** skip extracted NuGet packages inside the repository (a `nuget.config` packages folder or
  `packages/` from packages.config): a package's content files were counted as code and could be marked vendored.
- **`--diff`** leaves out a NuGet packages folder that a `nuget.config` puts inside the repository: the preview counted every
  restored package file as a change.
- Renames (BRO1303, BRO1306-BRO1308) keep a static field that registers a XAML property or routed event (`LevelProperty`,
  `IsOpenPropertyKey`, `ClickEvent` in Avalonia, WPF, WinUI or MAUI): XAML and Avalonia's analyzers find it by that name. It
  is listed as kept, with the reason.
- **BRO1123** writes `(0D, 1L)` instead of `((double)0, (long)1)` for `new ValueTuple<double, long>(0, 1)` when BRO1122 is
  on, so `dotnet format` doesn't need a second run for BRO1122.

### Upgrading from 0.7.0-alpha.1

Bump StyleBro.Analyzers to 0.8.0-alpha.1. The tool has a new package, so swap it instead of updating:
`dotnet tool uninstall -g StyleBro.Migrate`, then `dotnet tool install -g StyleBro.Tool --prerelease`
(in a tool manifest: `dotnet tool uninstall StyleBro.Migrate` and `dotnet tool install StyleBro.Tool --prerelease`).
Replace `stylebro-migrate` with `bro` in CI scripts and hooks (`dotnet stylebro-migrate` with `dotnet bro` in a
tool manifest); `stylebro-migrate [path]` becomes `bro migrate [path]`.
The settings blocks in your `.editorconfig` keep their `# BEGIN stylebro-migrate` markers and need no change. A
StyleBro section in `AGENTS.md` names the old command until the next `bro init --write --agents-md` (or `bro migrate --write --agents-md`).

## 0.7.0-alpha.1 – 2026-10-13

129 rules, each fixed by `dotnet format`. Docs: https://bisforboman.github.io/stylebro/

### New

- **Preset versions.** `<StyleBroPreset>` takes a version (`1.0`, later `1.1`, ...), `latest` or `none`, like .NET's
  `AnalysisLevel`: from 1.1 on, a release's new rules stay off until you raise it, so upgrading the package never adds
  warnings by itself. Unset means the package's major version (`1.0`); before 1.0 every rule is a 1.0 rule, so nothing
  changes yet. A version newer than the package warns (STYLEBRO002); any other value fails the build (STYLEBRO001).
  See https://bisforboman.github.io/stylebro/versioning/#new-rules

### Changed

- **Field renames follow your naming rules for non-private fields too** (BRO1303/BRO1306): the first
  `dotnet_naming_rule` that covers a public, internal or protected field, in Roslyn's order, decides its casing, but
  only when something enforces it (IDE1006's severity, or the rule's own, is `suggestion`, `warning` or `error`).
  A rule for camelCase internal fields no longer gets them renamed to PascalCase; a style StyleBro doesn't write (a
  prefix) leaves the name alone; an unenforced rule leaves StyleCop's defaults. `init` lets your code decide the
  private field style while IDE1006 is off.
- **StyleBro and CSharpier settle:** after `stylebro-migrate format` and `dotnet csharpier format .`, another `format`
  changes nothing.
  - `init` writes `stylebro_layout_by = csharpier` and `stylebro_layout_print_width` (the `.csharpierrc`'s
    `printWidth`, else `max_line_length`, else 100), and the opposite of a `dotnet_sort_system_directives_first =
    false` or `dotnet_separate_import_directive_groups = true` it finds, since CSharpier sorts usings System first.
  - BRO1149 lays out a merged condition the way CSharpier does (also `||` chains that don't fit); BRO1514-BRO1516
    count a statement CSharpier will split as multi-line, so its braces come in the same run.
  - `stylebro-migrate --write` (from StyleCop) detects CSharpier like `init` (IDE0055 and the same layout rules off;
    the report lists the StyleCop rules they replaced under "CSharpier decides it").
- **`init` follows more conventions:** spacing IDE0055 enforces (`if(x)`, `( a + b )`, `(int) x`, `F( x )`: a clear
  form is written as `csharp_space_*` keys, mixed code turns IDE0055 off), and blank lines between members (BRO1505)
  and above doc comments (BRO1513): compact or mixed code turns the rule off. `init` also takes `--project`.
- **Leftover StyleCop settings** (no project references StyleCop.Analyzers any more): `init` proceeds and follows the
  code, noting the lines; `--write` says StyleCop isn't installed and points to `init`.
- The preset file moved from `build/stylebro.recommended.globalconfig` to `build/presets/stylebro.1.0.globalconfig` in
  the package. Only matters if your build references the file by path.

### Fixed

- **Wrong code:** BRO1151 no longer unwraps a one-element array the call would take as the array itself
  (`Take(new Item[] { default! })` became `Take(default!)`, a null array; the compiler now decides), nor an array whose
  elements take their type from it (`M([new() { ... }])` didn't compile).
- **Rules:** BRO1123 and BRO1115 leave doc comment crefs alone (`Func{Nullable{int}}`), where the build warned and
  `dotnet format` had nothing to fix; BRO1311 keeps a tuple element's rename only for a use as a member in code the run
  doesn't load (`.value`, `value:`; internal members only where `InternalsVisibleTo` reaches), and says so.
- **Line endings, charset, final newline:** `init` judges them against the `.editorconfig` that decides each file (the
  nearest one setting the key) and writes its line there (a `# BEGIN stylebro-whitespace` block), so a nested
  `insert_final_newline = false` no longer makes `format` strip line breaks the report said it would keep.
- **In-solution source generators** (Mapperly): `format --verify-no-changes` and `--diff` previews run the generator in
  their copy too, so verify no longer fails where `format` is clean and previews no longer show renames `format` keeps
  back. Verify also stays clean where the restore installs git hooks (Husky.Net).
- **Migration where StyleCop is referenced conditionally:** the MSBuild `Condition` is evaluated, so settings go only
  where StyleCop ran (a `root = true` folder where it didn't gets none), and StyleBro.Analyzers is added next to the
  StyleCop reference under the same condition.
- **stylebro-migrate:** `baseline` skips the whitespace pass where IDE0055 is off (CSharpier), like `format`; `format`
  leaves projects no solution configuration builds alone and says so; a second `init` writes its lines in the same
  order; suppressions are no longer carried into generated code or onto IDE0055; lower-case `sa1234` ids are read; the
  line-ending and private-field (SA1309 in a sub-folder) notes judge the code correctly; `net4.7` and `net47` share
  one format run; clearer messages.

### Upgrading from 0.6.0-alpha.1

Bump the package, then update the tool: `dotnet tool update -g StyleBro.Migrate --prerelease`.

- `StyleBroPreset` unset means `1.0`, which is every current rule, so nothing changes. `stylebro-migrate init --write`
  pins `<StyleBroPreset>1.0</StyleBroPreset>` in `Directory.Build.props`; repositories migrated from StyleCop keep
  `none`.
- Run `stylebro-migrate init --write` (or `--write` if you migrated from StyleCop) again if you use CSharpier (for
  `stylebro_layout_by` and `stylebro_layout_print_width`), have a nested `.editorconfig` that sets line endings, the
  charset or the final line break, or space your code differently from the SDK's defaults.
- Have `dotnet_naming_rule` entries for non-private fields? Check the first `format` run: BRO1303/BRO1306 now follow
  the enforced ones.

Full list of changes: https://github.com/bisforboman/stylebro/compare/v0.6.0-alpha.1...v0.7.0-alpha.1

## 0.6.0-alpha.1 – 2026-10-12

129 rules, each fixed by `dotnet format`. Docs: https://bisforboman.github.io/stylebro/

### Breaking changes

- **The preset no longer sorts usings** (it stopped setting `dotnet_sort_system_directives_first`, which makes
  `dotnet format` sort whenever it's set). `init` writes the key when your usings are already sorted, else a comment
  on how to turn sorting on.
- **Sonar mappings never fix what the Sonar rule accepts** (`stylebro-migrate`): S100, S101, S1118, S1125, S1481,
  S1854, S1905, S2325, S3052, S3260 and S3442 are no longer mapped (the report lists them with the reason); S121, S818,
  S1066, S1116, S3878 and S8969 use the new Sonar-matching settings below; S927 turns BRO1313 on with
  `stylebro_match_base_parameter_names = non_generic_methods`; S4136 maps to `stylebro_keep_overloads_together =
  same_access`. Where StyleCop turns the same rule on (SA1106, SA1503), StyleCop's wider scope stays.
- **A StyleCop migration applies only where StyleCop ran:** `stylebro-migrate --write` writes settings (and turns the
  preset off) only for the folders of projects that reference StyleCop.Analyzers, reading each project's own ruleset
  and `stylecop.json`. Other folders are listed in the report, and `format` leaves them alone.
- **More field renames go through:** BRO1303/BRO1306 keep a name only for a string that reaches a member by name
  (`GetField("_count")`, `Type.GetType(...)`, `[MemberData("...")]`, ...), no longer for any string with that text.
- **`stylebro-migrate format --verify-no-changes` exits 1 when a project wasn't loaded in any run**, instead of
  reporting "Clean." (projects skipped for some frameworks only, or for an F# reference, stay warnings).
- **Old-style (non-SDK) projects:** `format` no longer restores them; run `msbuild -t:restore
  -p:RestorePackagesConfig=true` first. `packages.config` projects get StyleBro.Analyzers the packages.config way, and
  the `Directory.Build.props` reference gets a condition that leaves them out (it broke their restore).

### New

- **`stylebro-migrate` for scripts and AI agents** ([docs](https://bisforboman.github.io/stylebro/agents/)): `--json`
  on every command, `format --files a.cs b.cs` (seconds instead of minutes), `--agents-md` for an `AGENTS.md` section,
  and `llms.txt`/`llms-full.txt` on the docs site.
- **Six rules can report exactly what their Sonar rule does:** `stylebro_braces_statements = if_and_loops` (BRO1514,
  S121), `stylebro_upper_case_literal_suffixes = lone_l` (BRO1135, S818), `stylebro_allow_nested_dynamic_ifs`
  (BRO1149, S1066), `stylebro_allow_semicolon_after_closing_brace` (BRO1101, S1116),
  `stylebro_params_arrays = except_object_and_jagged` (BRO1151, S3878), `stylebro_allow_null_forgiving_on_unannotated`
  (BRO1147, S8969).
- **More settings:** `stylebro_match_base_parameter_names = all | non_generic_methods` (BRO1313),
  `stylebro_private_static_field_naming`, `stylebro_private_mutable_static_field_naming`,
  `stylebro_allow_leading_underscore_in_pascal_fields` (BRO1306; `--write` sets it from SA1309),
  `stylebro_keep_overloads_together = same_access`.
- **Finding the solution:** Traversal projects, the only solution one folder down, and a solution next to several
  `.slnf` filters of it work without naming one. `stylebro-migrate --version`.

### Changed

- **`init` follows more of your code's conventions** (clear code sets the style, mixed code turns the rule off):
  line endings, charset and final newline (from the files, also against your `.editorconfig`); naming (static field
  casing, `s_`/`t_` prefixes, `String` vs `string`); layout (member order, one-line statements and jumps, multi-line
  auto-properties, grouped list items, `#region`s in members, combined declarations, implicit `private`, relative
  usings, summary wording); other formatters (CSharpier, ReSharper/Rider column alignment). A key the code contradicts,
  or one nothing enforces, turns off the rule instead; `root = true` folders get their own block; re-runs keep what the
  last run wrote.
- **Rules:** BRO1514-BRO1516 allow consecutive `fixed` statements (`stylebro_allow_consecutive_usings`); BRO1523 leaves
  chains whose first line has several calls; BRO1606 and BRO1604 leave summaries in your own words; BRO1504 needs no
  blank line before a comment in a list or call chain; BRO1401 skips property patterns; BRO1611/BRO1613/BRO1614 skip
  `<inheritdoc>`; naming rules leave interop names and `dotnet_naming_rule` prefixes alone; BRO1142 follows
  `csharp_style_var_*`; nothing is reported in source-generator output.
- **Faster:** a full `dotnet format` takes about half the time, and `stylebro-migrate`'s first run 35-60 % less.

### Fixed

- **One `dotnet format` run is enough** where several rules used to need a second one (BRO1101, BRO1122, BRO1126,
  BRO1149, BRO1509, BRO1514-BRO1516, BRO1527, member sorting with region removal, BRO1107 with BRO1108).
- **Rules that changed meaning:** BRO1001 keeps types read by position through reflection; BRO1301/BRO1302 leave
  expression-tree names; BRO1303/BRO1306 don't rename a field to its class's name; `nameof` and multi-targeted renames
  check the right symbols; file headers with `;` or `#` are kept whole.
- **`format` and `--diff` are sturdier:** the same result on every OS (each file keeps its line ending); no false
  "clean" when the analyzers didn't load; restores skip NuGet's audit and explain NU1102; projects sharing files,
  linked files, `.slnf` filters, central package management, nested `Directory.Build.props` and target framework
  spellings (`net6`, `net6.0`) are handled; clear errors instead of stack traces.
- **Migration and `init`:** Sonar severities per folder and per SonarAnalyzer version; versions from MSBuild properties;
  StyleCop 1.0.x; a second `--write` or `init` gives the same settings; edited files keep their line endings.

### Upgrading from 0.5.0-alpha.1

Bump the package, then update the tool: `dotnet tool update -g StyleBro.Migrate --prerelease`.

Run `stylebro-migrate init --write` again (or `stylebro-migrate --write` if you migrated from StyleCop) to pick up the
new conventions; it keeps what it wrote before. Code a `format` run already sorted keeps BRO1001 on.

- Relied on the preset to sort usings? Re-run `init`, or set `dotnet_sort_system_directives_first = true`.
- Migrated from StyleCop with SA1309 off? `--write` adds `stylebro_allow_leading_underscore_in_pascal_fields = true`,
  so BRO1306 stops renaming `_x` static readonly fields.
- Old-style projects: restore with `msbuild -t:restore -p:RestorePackagesConfig=true` before `stylebro-migrate format`.

Full list of changes: https://github.com/bisforboman/stylebro/compare/v0.5.0-alpha.1...v0.6.0-alpha.1

## 0.5.0-alpha.1 – 2026-10-10

129 rules, each fixed by `dotnet format`. Docs: https://bisforboman.github.io/stylebro/

### Fixed: problems in 0.4.0-alpha.1

- **Renames inside `#if` code broke builds:** a type parameter (or another name) used in a branch of `#if` that the
  current target framework doesn't compile was left with its old name. Names that appear in disabled code are no
  longer renamed.
- **`--diff` previewed without StyleBro** in repos whose projects use a nested `Directory.Build.props`: the package
  reference now goes where MSBuild picks it up (also for `--write`), and the preview warns when no StyleBro rule ran.

### New

- **Findings kept on purpose say why.** Renames StyleBro holds back (the name is read by reflection, used in
  `nameof`, public API, ...) get no code action, and `stylebro-migrate format` lists them with the reason.
- **A clean check that means clean:** `stylebro-migrate format --verify-no-changes` exits 0 when only kept findings
  remain (plain `dotnet format --verify-no-changes` keeps failing on them). Exit codes are in `--help` and the CI guide.
- **`init` explains what it adopted:** every convention gets a verdict (kept, default, off, too few), a summary says
  which rules were turned off because the code mixes both forms and how to choose later, and the written
  `.editorconfig` comments explain themselves.

### Changed

- **`init` needs at least 3 places** before following a style; with no clear majority it turns the matching rule off
  instead of rewriting the code to StyleBro's default.
- **SCREAMING_CASE locals** are converted word by word (`RANGE_REP_REG` → `rangeRepReg`).
- **BRO1150 with Sonar:** on lists and arrays it writes `Exists`/`Find` when the project's Sonar setup asks for them.

### Fixed

- `init`: vendored folders are treated as generated code; no hint for settings the repo already sets.
- `format`: names skipped projects, stops when restore is broken, lists the files each run changed.
- `--diff` describes invisible changes (byte order mark, final newline).
- BRO1606 no longer prefixes summaries that already start like a constructor sentence.

### Upgrading from 0.4.0-alpha.1

Bump the package, then update the tool:
`dotnet tool update -g StyleBro.Migrate --prerelease`.
Use `stylebro-migrate format --verify-no-changes` in CI instead of plain `dotnet format --verify-no-changes`.

## 0.4.0-alpha.1 – 2026-10-10

129 rules, each fixed by `dotnet format`. Docs: https://bisforboman.github.io/stylebro/

### Fixed: two problems in 0.3.0-alpha.1

- **Builds broke after `init`:** the SDK's IDE0065, which `init` turned on, could move usings out of a
  namespace and change what names refer to (CS0104/CS0118). It is replaced by **BRO1008**, which only
  moves usings when every name still means the same, and does nothing unless `csharp_using_directive_placement`
  is set (the preset sets `outside_namespace`).
- **Renames changed behavior through `nameof`:** a name that `nameof(...)` turns into a string is no longer
  renamed.

### New

- **SonarQube support:**
  - `stylebro-migrate` reads a team's Sonar setup (`.editorconfig`/`.globalconfig`, rulesets, or
    `--sonar-profile <exported profile>`) and turns on the StyleBro and SDK rules that fix what those Sonar
    rules report. Sonar suppressions are carried over.
  - Three new rules mirroring Sonar's: **BRO1149** merge nested ifs (S1066), **BRO1150**
    `Where(p).Count()` → `Count(p)` (S2971), **BRO1151** no explicit array for `params` (S3878).
  - With Sonar's package in the project, `dotnet format` also fixes Sonar's own S1066, S2971 and S3878.
- **`init` adapts to the repo:** it detects the repo's conventions for StyleBro's settings (`=>` and operator
  placement, trailing commas, `""`, null checks, using placement, `_field` names, ...) and keeps the
  majority style.

### Changed

- **Public API isn't renamed by default:** naming rules skip names visible outside the assembly;
  `stylebro_rename_public_api = true` to include them.
- **`stylebro-migrate format`** repeats until a run changes nothing (at most 3) and says how many files each
  run changed.

### Fixed

- **Fix order:**
  - Fixes that needed a second `dotnet format` run now converge in one: comments below a field, blank lines
    around documented properties, `: base(...)`/`where` at the line limit.
  - BRO1603 no longer adds a period after custom doc elements.
- **Migration and `init`:**
  - EF Core migrations are treated as generated code.
  - One-line `delegate { … }` statements are no longer split.
  - The tool asks which solution to use when there are several.
  - `--diff` samples show the right change.

### Upgrading from 0.3.0-alpha.1

Bump the package, then update the tool:
`dotnet tool update -g StyleBro.Migrate --prerelease`.
Run `stylebro-migrate init --diff` (or `<path> --diff` if you migrated from StyleCop) to preview the changes.

## 0.3.0-alpha.1 – 2026-10-09

125 rules, each fixed by `dotnet format`. Docs: https://bisforboman.github.io/stylebro/

### New

- **BRO1410:** unnecessary parentheses in patterns (`x is (> 0)` → `x is > 0`). Unnecessary
  parentheses (BRO1405, BRO1410) are now also faded in the IDE, like StyleCop's SA1119_p.
- **11 settings where teams prefer otherwise:**
  - `default` literal;
  - `""` instead of `string.Empty`;
  - omit trailing commas;
  - empty `{ }` on one line;
  - `)` on its own line;
  - first item on the `(` line;
  - one-line members together;
  - `<inheritdoc />`;
  - `: base(...)` / `where` on the declaration line;
  - `l`-only literal suffixes.

  See [Settings](https://bisforboman.github.io/stylebro/configuration/).
- **`stylebro-migrate --diff`:** previews `init`, a migration or `format` on a temporary copy.
  Prints changes per rule and writes the full diff to a `.patch` file.
- **Docs site with search:** each rule's help link now opens its page there.

### Changed

- **`stylebro-migrate format`** applies only StyleBro's and the SDK style rules' fixes;
  `--all` for everything.
- **`init` adapts to the repo:** it keeps `_field` names when they're the majority, and points
  to `--write` when it finds a StyleCop setup.
- **BRO1604** only rewrites summaries that start like a noun phrase.

### Fixed

- **Migrating:** StyleCop settings kept in a git submodule are read, and `format` no longer
  edits submodule files.
- **File header:** never set up with StyleCop's placeholder company.
- **Region removal (BRO1517):** several empty regions in a row now go in one run.
- **Fix order:** several cases that needed a second `dotnet format` run now converge in one
  (member sorting with blank lines, call chains with `#region`, documented fields).
- **Uneditable files:** no findings in NuGet `contentFiles`.

### Upgrading from 0.2.0-alpha.1

Bump the package and the tool:
`dotnet tool update -g StyleBro.Migrate --prerelease`.
Run `stylebro-migrate init --diff` (or `<path> --diff` if you migrated from StyleCop) to see what
the new version would change before running it for real.

## 0.2.0-alpha.1 – 2026-10-06

53 new rules, `stylebro-migrate format` for multi-targeted repositories, the PR pipeline (parity, real-world runs, mutation testing).

## 0.1.0-alpha.8 – 2026-10-02

Baseline support and a BRO1117 crash fix; 71 rules.

## 0.1.0-alpha.7 – 2026-10-02

71 rules.

## 0.1.0-alpha.6 – 2026-10-01

54 rules; the first prerelease published without an approval step.

## 0.1.0-alpha.5 – 2026-10-01

The `stylebro-migrate` tool (StyleBro.Migrate).

## 0.1.0-alpha.4 – 2026-10-01

43 rules.

## 0.1.0-alpha.3 – 2026-09-30

18 rules.

## 0.1.0-alpha.2 – 2026-09-30

11 rules, rule pages with tested examples, Fix All in multi-targeted projects.

## 0.1.0-alpha.1 – 2026-09-30

The first release: BRO1001 member ordering.
