# Getting started

From install to the first `dotnet format` run. Pick the path that fits:

- [Preview first](#preview-first): what either path would change, without changing anything
- [A. A new project, or no StyleCop](#a-a-new-project-or-no-stylecop)
- [B. Coming from StyleCop](#b-coming-from-stylecop)

Both use two packages: **StyleBro.Analyzers** (the rules, referenced by your projects) and **StyleBro.Migrate** (the
`stylebro-migrate` command line tool, used once to write settings):

### Which version

The latest release on nuget.org is **0.5.0-alpha.1**, a prerelease: install the tool with `--prerelease` and reference
that exact version. This page describes `main`: what changed since that release (some `init` verdicts,
for example) ships in the next one and is listed in [changes/](https://github.com/bisforboman/stylebro/tree/main/changes).
To try the code on `main` before the next release, build both
packages from a clone into a local folder (any version string works; `0.5.0-dev.1` here):

```
git clone https://github.com/bisforboman/stylebro
dotnet pack stylebro/src/StyleBro.Package -o stylebro-feed -p:Version=0.5.0-dev.1
dotnet pack stylebro/src/StyleBro.Migrate -o stylebro-feed -p:Version=0.5.0-dev.1
dotnet tool install --global StyleBro.Migrate --add-source stylebro-feed --version 0.5.0-dev.1
```

Then add the folder as a package source in your repository's `nuget.config` and use `0.5.0-dev.1` wherever this page
says `0.5.0-alpha.1`. When the `nuget.config` has a `<packageSourceMapping>`, the new source needs a mapping too: NuGet
only looks for a package in the sources mapped to it, so the source alone gives NU1102 (package not found):

```xml
<configuration>
  <packageSources>
    <add key="stylebro" value="path/to/stylebro-feed" />
  </packageSources>
  <packageSourceMapping>
    <!-- the mappings already there stay -->
    <packageSource key="stylebro">
      <package pattern="StyleBro.*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

## Preview first

Before changing anything, `--diff` shows what a path would do to your code. It works on all three commands:

```
stylebro-migrate init --diff          # path A: init's settings, then format
stylebro-migrate path/to/repo --diff  # path B: the migration's settings (--write), then format
stylebro-migrate format --diff        # only the format run
```

The preview copies the repository to a temporary folder (git's tracked and untracked files, not the ignored ones; the
working trees of submodules too), runs the command there, then `stylebro-migrate format` until a run changes nothing,
and deletes the copy. Your repository isn't touched. If it doesn't reference StyleBro.Analyzers yet, the copy gets the
reference from [step 1](#1-add-the-package) (the tool's own version) and the output says so. It prints a summary and
writes the full diff to `stylebro-preview.patch` in the current folder (`--diff=other.patch` names another file).
`--keep` keeps the copy and prints where it is; `--all` passes on to format; with several solutions or projects in the
folder, `--project MySolution.sln` names the one to format. `--no-restore` skips the restore in the copy: it takes the
repository's own restore output, so run `dotnet restore` first (it's ignored when the preview adds the package, which
needs a restore). Where the preview adds the reference under central package management: next to StyleCop's (a
`GlobalPackageReference` gets a `GlobalPackageReference`, in the same `Directory.Packages.props`), else a version-less
`PackageReference` with the version in the `Directory.Packages.props` that applies (also one that a folder's
`Directory.Packages.props` imports). FFMpegCore, `stylebro-migrate init --diff`:

```text
Previewing 'stylebro-migrate init --write' and 'stylebro-migrate format' on a copy; nothing in C:\src\FFMpegCore changes.
Copying the repository to C:\Users\me\AppData\Local\Temp\stylebro-preview-fmsw0dps.3qa\FFMpegCore... 0s
Settings: 31 lines in .editorconfig; 3 lines in Directory.Build.props (StyleBro.Analyzers)
  StyleBro.Analyzers wasn't referenced: added StyleBro.Analyzers 0.5.0-alpha.1 to Directory.Build.props for the preview, as docs/getting-started.md says (the patch includes it).
Finding what format fixes ('dotnet format --verify-no-changes')... 16s
Format run 1... 29s
Format run 2... 14s
Format: 124 files, 579 reported changes, clean after 1 run
  BRO1603  Documentation text should end with a ...    65 files
  BRO1001  Members should be ordered                   33 files
  BRO1401  Use a trailing comma in multi-line in...    33 files
  ...
  ... and 24 more rules
Sample (BRO1603, FFMpegCore.Extensions.Downloader/Exceptions/FFMpegDownloaderException.cs):
  -///     Custom exception for FFMpegDownloader
  +///     Custom exception for FFMpegDownloader.
Sample (BRO1401, FFMpegCore.Examples/Program.cs):
  -            FrameRate = 30 //set source frame rate
  +            FrameRate = 30, // set source frame rate
Full diff: stylebro-preview.patch (5,966 lines)
Done in 1m 2s.
```

Files are counted under every rule `dotnet format --verify-no-changes` reported in them before the fixes; a file with
only whitespace changes counts as IDE0055, one with nothing reported (another fix's side effect) as `other`. The patch
is what the real commands write (byte for byte on FFMpegCore); `git apply stylebro-preview.patch` applies it.

## A. A new project, or no StyleCop

### 1. Add the package

Add it to every project, for example in `Directory.Build.props` at the repository root:

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="StyleBro.Analyzers" Version="0.5.0-alpha.1" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

With central package management, the version goes in `Directory.Packages.props` and the reference has none:

```xml
<!-- Directory.Packages.props -->
<ItemGroup>
  <PackageVersion Include="StyleBro.Analyzers" Version="0.5.0-alpha.1" />
</ItemGroup>

<!-- Directory.Build.props -->
<ItemGroup>
  <PackageReference Include="StyleBro.Analyzers" PrivateAssets="all" />
</ItemGroup>
```

A project that turns central package management off itself (`<ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>`,
often an analyzer or source generator project) fails its restore with the version-less reference (NU1015). Give such
projects a reference with a version, next to the version-less one:

```xml
<!-- Directory.Build.props -->
<ItemGroup>
  <PackageReference Include="StyleBro.Analyzers" PrivateAssets="all" Condition="'$(ManagePackageVersionsCentrally)' == 'true'" />
  <PackageReference Include="StyleBro.Analyzers" Version="0.5.0-alpha.1" PrivateAssets="all" Condition="'$(ManagePackageVersionsCentrally)' != 'true'" />
</ItemGroup>
```

MSBuild imports only the nearest `Directory.Build.props` above a project: if a folder (say `src/`) has its own and
it doesn't import the root one, put the reference there (or in each such file). If it does import it (`GetPathOfFileAbove`,
or a property like Orleans' `$(_ParentDirectoryBuildPropsPath)`), the root's reference is enough: a second one fails the
restore (NU1504, duplicate reference). A reference inside a conditional
`ItemGroup` (`Condition="'$(TargetFramework)' == 'net8.0'"`) reaches only that target framework, and
`<RunAnalyzers>false</RunAnalyzers>` turns all analyzers off where it applies. `--diff` adds the reference to every
`Directory.Build.props` the projects use and warns when StyleBro reported nothing at all.

The package brings StyleBro's rules and its preset: rule severities and formatting options, close to StyleCop's
defaults. Your own `.editorconfig` wins over the preset. To configure everything yourself instead:
`<StyleBroPreset>none</StyleBroPreset>`.

### 2. Turn on the built-in rules

StyleBro relies on some built-in .NET rules (IDE0055 formatting, IDE0036 modifier order, IDE0049 type aliases, ...).
Their severities have to be in `.editorconfig`, because `dotnet format` ignores rule severities in a package's preset.
In the repository root:

```
stylebro-migrate init            # shows the block
stylebro-migrate init --write    # adds it to .editorconfig (created if missing)
```

`stylebro-migrate init --diff` shows what the block and the format run in step 3 would change, without writing
anything ([Preview first](#preview-first)).

The block sits between `# BEGIN stylebro-migrate` and `# END stylebro-migrate`; running the command again replaces it.
Put your own settings outside it. To get these rules reported by the build too, add
`<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` to `Directory.Build.props` (`init` says so when no project
file sets it yet).

#### init also looks at your code

`init` looks at your code first and keeps the conventions it clearly follows. For each setting below it counts both
forms in your C# (generated code, EF Core migrations, vendored folders and submodules don't count; code inside `#if`
counts too, as `format` sees it in the target framework that defines the symbol):

Vendored folders are recognized by their name (`vendor`, `third_party`, ...). Code copied in under another name (a
`src/Sdk` from another product) counts as yours and can decide a convention; mark its folder `generated_code = true` in
your root `.editorconfig` (`[src/Sdk/**]`, see [excluding vendored code](#excluding-vendored-code)) and run `init` again:
folders marked that way don't count.

- **fewer than 3 places** (or none): too few to tell, StyleBro's default stays and nothing is written;
- **3 to 9 places**: followed when they all agree;
- **10 places or more**: followed when one form has at least 75 %;
- **mixed** (no form gets there): the StyleBro rule that enforces the setting is turned off, so the code stays as it is
  (`off`: `dotnet_diagnostic.BRO1520.severity = none`; `both`: for braces on one-line bodies `csharp_prefer_braces =
  when_multiline`, which allows both). Where no StyleBro rule enforces the setting (`{` placement, `<inheritdoc/>`
  spacing, `default(T)`), the verdict is `mixed`: the default stays and nothing is turned off.

A followed form that is StyleBro's default needs no line; any other is written into the block. A key your root
`.editorconfig` sets wins, so does a Sonar setup's, and a `dotnet_naming_rule` for private fields decides the field style
(naming rules for interfaces or constants don't). One exception: when your `.editorconfig` sets an SDK key that only a
StyleBro rule enforces (operator placement, braces = `true`, using placement, arithmetic parentheses =
`always_for_clarity`) and your code clearly does the other thing, nothing ever enforced that key. Then `init` leaves the
key alone and turns that rule off (`your .editorconfig says end_of_line, ... so BRO1520 is off`), instead of letting it
rewrite the code.

A key your `.editorconfig` sets at severity `silent` or `none` (`csharp_prefer_braces = true:silent`, or
`dotnet_diagnostic.IDE0011.severity = silent` next to it) isn't enforced by anything: it's what the IDE wrote, not what the
team checks. `init` treats it like a key that isn't set and follows the code; where the code's form differs from the key,
it writes the code's form into the block, which comes later in the file and wins (also when that form is StyleBro's
default). This applies to every convention whose key takes a severity: braces (IDE0011), using placement (IDE0065),
arithmetic parentheses (IDE0048), default values (IDE0034) and operator placement.

**Folders with their own `root = true` `.editorconfig`** (a `sample/` with its own solution): the root's settings never
reach their files, since `root = true` stops the lookup there. `init` gives each such folder with C# code a block of its
own in its `.editorconfig`, with the built-in rule severities, judged from that folder's code and settings alone (the
report has a section per folder). A folder with too little code to tell keeps StyleBro's defaults, and the report says so.
`stylebro-migrate --write` writes the whole block into such a folder too.

A few settings work a little differently:

- **One-line statements** (`if (a) return;`, `case 1: a(); break;`): the preset sets
  `csharp_preserve_single_line_statements = false`, which moves each to a line of its own. From 3 such statements on,
  `init` writes `true`, majority or not, unless your `.editorconfig` sets the key.
- **`<inheritdoc/>` on overrides and implementations** ([BRO1601](rules/BRO1601.md)): in projects that generate
  documentation, `init` counts the overrides and interface implementations BRO1601 checks, with and without a doc
  comment. Mostly documented: the rule stays on; mostly undocumented or mixed: it's turned off.
- **Line endings** count files. Without `end_of_line`, `format` writes your OS's line ending into the lines it rewrites
  (on Windows CRLF into LF files), so `init` writes the files' ending when it's clear and nothing when it's mixed. Files
  git converts on checkout (`core.autocrlf`, or a `.gitattributes` `text` attribute without `eol`: LF in the repository,
  CRLF in a Windows checkout) don't count, and the report names which of the two converts them: there the OS's ending is
  what the checkout has, and a fixed one would make `format` rewrite every file on the other OS. When git converts at
  least as many files as it leaves alone, those are the convention and nothing is written: a few files committed with
  CRLF don't decide for the rest (one trial repository had 3 such files next to 145 converted ones, and
  `end_of_line = crlf` from those 3 meant 12,593 line-ending errors in an LF checkout). `stylebro-migrate --write`
  writes `end_of_line` the same way. When your `.editorconfig` sets `end_of_line` but git converts the files on
  checkout (`end_of_line = lf` with `*.cs text=auto` means CRLF files in a Windows checkout), `init` keeps endings out of
  it: `end_of_line = unset` for C# files and IDE0055 as a suggestion, so neither `format` nor the build touches them, and
  the report says how to settle it (`*.cs text eol=lf` in `.gitattributes`, or removing `end_of_line`). When your
  `end_of_line` says one ending but the files git leaves alone clearly have the other (`crlf`, with nearly every file LF),
  nothing ever enforced it: `init` writes the files' ending for C# files, so `format` doesn't flip every file it touches,
  and the report says how to switch to the key's ending instead.
- **`s_`/`t_` prefixes** count private static fields (constants aside) with and without the .NET runtime's `s_` (or `t_`)
  prefix. Mostly without: [BRO1307](rules/BRO1307.md) stays on; mostly with or mixed: it's turned off, so `format` doesn't
  rename them (there is no switch for static fields alone, so `m_` fields keep their prefix too). A `dotnet_naming_rule`
  of yours that requires `s_` decides instead.
- **Type names** count files: files that name built-in types only with keywords (`string`, `int`), and files with a
  framework name IDE0049 would rewrite (`String`, `System.Int32`). Mostly keywords: IDE0049 stays on; mostly framework
  names or mixed: it's turned off. Your own `dotnet_style_predefined_type_*` keys decide instead.
- **Member order** ([BRO1001](rules/BRO1001.md)) counts types whose members BRO1001 would sort apart (two or more
  members with different sort keys), with your own order settings: already in its order or not. A type that is in order
  once overloads stay together counts as sorted (whether they do is the overloads convention's call). Mostly sorted: the
  rule stays on; mostly unsorted or mixed: it's turned off, so `format` doesn't reorder code that was never sorted
  (StabilityMatrix: 370 sorted, 414 not). Only type members count: BRO1001 also sorts the types of a namespace, but most
  files hold one type, so they would say little.
- **Unsigned literal suffixes** ([BRO1135](rules/BRO1135.md)) count `u`/`ul` suffixes in upper or lower case (`0U` or
  `0u`); `L` alone doesn't count, since nobody writes `1l`. Mostly upper case: the rule stays on; mostly lower case or
  mixed: it's turned off (Orleans: 423 upper, 185 lower). With it off, BRO1122 writes `6u`/`1ul` instead of `6U`/`1UL`.
- **Items of split lists** ([BRO1108](rules/BRO1108.md)) count argument and parameter lists whose items start on more
  than one line: each item on its own line, or grouped (`F(tag, 1,` and `"a", "b")` on the next line). Mostly one per
  line: the rule stays on; mostly grouped or mixed: it's turned off (metadata-extractor: 104 one per line, 197 grouped).
- **CSharpier**: when the repository uses [CSharpier](https://csharpier.com) (a `.csharpierrc`, `.csharpierrc.json`,
  `.csharpierrc.yaml` or `.csharpierrc.yml` at the root, a `csharpier` tool in `.config/dotnet-tools.json` or
  `dotnet-tools.json`, or a `CSharpier.MsBuild` package reference), the two formatters would undo each other's layout
  (StabilityMatrix's pre-commit check rejected 161 files StyleBro had formatted). `init` then turns off IDE0055 (so
  `stylebro-migrate format` skips the whitespace pass) and every StyleBro rule for something CSharpier decides, and the
  layout conventions in the table are reported as `set` (`CSharpier formats it`). Off:
  - line breaks in lists: [BRO1107](rules/BRO1107.md)-[BRO1110](rules/BRO1110.md),
    [BRO1116](rules/BRO1116.md)-[BRO1119](rules/BRO1119.md); constraints [BRO1111](rules/BRO1111.md), constructor
    initializers [BRO1105](rules/BRO1105.md), enum values [BRO1121](rules/BRO1121.md), query clauses
    [BRO1127](rules/BRO1127.md)-[BRO1130](rules/BRO1130.md);
  - wrapping: [BRO1520](rules/BRO1520.md)-[BRO1524](rules/BRO1524.md) (operators, `=>`, `=`, call chains, `?:`);
  - single-line blocks and members, accessors: [BRO1508](rules/BRO1508.md)-[BRO1510](rules/BRO1510.md),
    [BRO1527](rules/BRO1527.md);
  - the blank lines CSharpier normalizes itself: before `{` and `else`/`catch`/`finally` ([BRO1501](rules/BRO1501.md),
    [BRO1502](rules/BRO1502.md)), after `{` ([BRO1503](rules/BRO1503.md)), before `}` ([BRO1518](rules/BRO1518.md)),
    before a do-while's `while` ([BRO1512](rules/BRO1512.md)), runs of them ([BRO1517](rules/BRO1517.md)), and the
    ones CSharpier removes where StyleBro would add them: before a comment ([BRO1504](rules/BRO1504.md): CSharpier
    removes it inside initializers and above `else`) and between elements ([BRO1505](rules/BRO1505.md): CSharpier
    removes it between accessors).

  Everything else stays: naming, member order, documentation, braces on one-line bodies, readability rules, and the
  blank lines CSharpier keeps as you wrote them (after `}`, after attributes, around doc comments, between switch
  sections). Run CSharpier after `format`: fixes that rewrite code (an `if` merged into the enclosing one, attributes
  split) leave lines in StyleBro's layout, which CSharpier then lays out its way, and StyleBro leaves that alone. To have
  StyleBro format instead, remove CSharpier and run `init` again.
- **Running `init` again** keeps the convention lines the first run wrote (the ones below `# Conventions in this
  repository's code`, with their `# init:` comments), and the report says `kept from the last init`. They describe your
  code as it was before StyleBro formatted it: counted again after a `format` run, StyleBro's own output would decide
  (a rule that added blank lines in some places would make your code look mixed, and the setting would flip). To have a
  convention counted again, delete its line from the block and run `init` again; to count everything again, delete the
  block. Conventions the first run left at StyleBro's default (no line) are counted again each time.
- **Private constants and static readonly fields** ([BRO1306](rules/BRO1306.md)) count three forms: `PascalCase`,
  `camelCase` and `_camelCase`. A clear one is written as `stylebro_private_static_field_naming` (PascalCase is the
  default and needs no line); mixed turns BRO1306 off, so `format` doesn't rename them (Ical.Net: 26 PascalCase, 8
  camelCase, 50 `_camelCase`; BRO1306 renamed them in 28 files). With it off, public and internal fields aren't checked
  either. A `dotnet_naming_rule` for constants or static readonly fields decides instead.
- **Usings inside a namespace** ([BRO1126](rules/BRO1126.md)) count fully qualified ones (`using Company.App.Common;`)
  and ones relative to an enclosing namespace (`using Common;` inside `namespace Company.App.Orders`). Mostly qualified:
  the rule stays on; mostly relative or mixed: it's turned off (Fitnet: 91 qualified, 92 relative). A name is relative
  when an enclosing namespace has a namespace your code declares with its first part; a namespace from a package doesn't
  count.
- **File headers with empty `//` lines**: when your `file_header_template` starts or ends with a line break (IDE0073
  writes an empty `//` line there), or most files' headers start or end with an empty `//` line, `init` turns
  [BRO1120](rules/BRO1120.md) off: it would remove those lines, and every new file from the template would bring them
  back (Ical.Net: 222 of 223 files).
- **Charset**: when your `.editorconfig` sets `charset` (`utf-8` or `utf-8-bom`) but the C# files don't clearly follow it
  (BOM or not, the same 75 %), nothing ever enforced it, and `format` would add or strip the BOM in every file. Like a
  line ending the files contradict, `init` writes the files' charset for C# files (`charset = unset` when they're mixed:
  each file keeps its own), and the report says how to switch (remove the line and run `stylebro-migrate format`).
  Ical.Net: `charset = utf-8` with 108 of 223 files starting with a BOM, all 108 stripped.
- **Using order** counts files with 2 or more usings: sorted with System first, sorted without, or not sorted. `dotnet
  format` sorts usings whenever `dotnet_sort_system_directives_first` is set (to any value), so the preset doesn't set
  it: `init` writes it when your usings are sorted (or there are too few files to tell), and only a comment when they
  aren't, or when some files put System first and others don't.

| Setting | What's counted | Rule | Key |
|---|---|---|---|
| Private field names | `field` or `_field` | [BRO1303](rules/BRO1303.md) | `stylebro_private_field_naming` |
| `{` placement | `{` of a multi-line block on its own line or at the end of the line | SDK formatter (IDE0055) | `csharp_new_line_before_open_brace` |
| `else`/`catch`/`finally` | after `}` on a new line or the same line | SDK formatter (IDE0055) | `csharp_new_line_before_else`, `_catch`, `_finally` |
| Braces | one-line `if`/`else`/`for`/`foreach`/`while`/`using`/`lock` bodies with or without braces | [BRO1514](rules/BRO1514.md)-[BRO1516](rules/BRO1516.md) | `csharp_prefer_braces` |
| Operator placement | binary and `?:` operators at the start or end of a wrapped line | [BRO1520](rules/BRO1520.md) | `dotnet_style_operator_placement_when_wrapping` |
| `=>` placement | `=>` of expression bodies and switch arms where a line wraps | [BRO1521](rules/BRO1521.md) | `stylebro_arrow_placement_when_wrapping` |
| `=` placement | `=` where a line wraps | [BRO1522](rules/BRO1522.md) | `stylebro_equals_placement_when_wrapping` |
| Trailing commas | multi-line initializers, enums and switch expressions | [BRO1401](rules/BRO1401.md) | `stylebro_trailing_comma` |
| Empty strings | `string.Empty` or `""` (outside constant contexts) | [BRO1106](rules/BRO1106.md) | `stylebro_empty_string_style` |
| Null checks | `is null` or `== null` | [BRO1133](rules/BRO1133.md) | `stylebro_null_check_style` |
| Summaries | one-line `<summary>` texts with the tags on their own lines or on one line | [BRO1616](rules/BRO1616.md) | `stylebro_summary_layout` |
| `<inheritdoc/>` | `<inheritdoc/>` or `<inheritdoc />` | [BRO1601](rules/BRO1601.md) | `stylebro_inheritdoc_style` |
| Default values | `default(T)` or `default` | IDE0034 | `csharp_prefer_simple_default_expression` |
| `)` of split lists | after the last item or on its own line | [BRO1110](rules/BRO1110.md) | `stylebro_closing_parenthesis_placement` |
| First item of split lists | on the next line or after `(` | [BRO1107](rules/BRO1107.md) | `stylebro_split_list_first_item` |
| Items of split lists | one per line or grouped on lines | [BRO1108](rules/BRO1108.md) | `dotnet_diagnostic.BRO1108.severity` |
| Constructor initializers | `: base(...)`/`: this(...)` on its own line or the constructor's | [BRO1105](rules/BRO1105.md) | `stylebro_constructor_initializer_placement` |
| Constraints | `where` on its own line or the declaration's | [BRO1111](rules/BRO1111.md) | `stylebro_constraint_placement` |
| `new T { ... }` | with or without `()` before an initializer | [BRO1141](rules/BRO1141.md) | `stylebro_object_creation_parentheses` |
| Switch sections | after a blank line or without one | [BRO1526](rules/BRO1526.md) | `stylebro_blank_line_between_switch_sections` |
| Using directives | files with a namespace and their usings on one level: inside it (file-scoped too) or outside; only `inside_namespace` is written, the preset already says `outside_namespace` | [BRO1008](rules/BRO1008.md) | `csharp_using_directive_placement` |
| Arithmetic parentheses | `a + (b * c)` or `a + b * c` | [BRO1406](rules/BRO1406.md) | `dotnet_style_parentheses_in_arithmetic_binary_operators` |
| One-line statements | a braceless `if`/`else`/loop body or a `case` label's statements on their owner's line or below it | SDK formatter (IDE0055) | `csharp_preserve_single_line_statements` |
| Documented overrides | overrides and interface implementations with or without a doc comment | [BRO1601](rules/BRO1601.md) | `dotnet_diagnostic.BRO1601.severity` |
| Member order | types whose members BRO1001 sorts apart: already in its order or not | [BRO1001](rules/BRO1001.md) | `dotnet_diagnostic.BRO1001.severity` |
| Overloads | overload groups whose members BRO1001 sorts to different places (by access, `static`, ...): next to each other or apart | [BRO1001](rules/BRO1001.md) | `stylebro_keep_overloads_together` |
| Unsigned literal suffixes | `u`/`ul` suffixes in upper or lower case (`0U` or `0u`) | [BRO1135](rules/BRO1135.md) | `dotnet_diagnostic.BRO1135.severity` |
| `s_`/`t_` prefixes | private static fields with or without the `s_`/`t_` prefix | [BRO1307](rules/BRO1307.md) | `dotnet_diagnostic.BRO1307.severity` |
| Type names | files with only keywords (`int`) or framework names (`Int32`) | IDE0049 | `dotnet_diagnostic.IDE0049.severity` |
| Private constants and static readonly fields | `PascalCase`, `camelCase` or `_camelCase` | [BRO1306](rules/BRO1306.md) | `stylebro_private_static_field_naming` |
| Usings inside a namespace | fully qualified or relative to an enclosing namespace | [BRO1126](rules/BRO1126.md) | `dotnet_diagnostic.BRO1126.severity` |
| File headers | headers with or without an empty `//` line at the top or bottom (or a `file_header_template` with one) | [BRO1120](rules/BRO1120.md) | `dotnet_diagnostic.BRO1120.severity` |
| Charset | files with or without a BOM, when your `charset` doesn't match them | SDK formatter | `charset` |
| Line endings | files with LF or CRLF line endings (not files git converts on checkout) | SDK formatter | `end_of_line` |
| Using order | files with 2 or more usings: sorted with System first, sorted without, or not sorted | `dotnet format` | `dotnet_sort_system_directives_first` |

The keys are described in [Settings](configuration.md). `init` prints each verdict (`kept`, `default`, `off`, `both`,
`mixed`, `too few`, or `set` when your configuration decides) with the value that applies, then a summary that names the keys
(RealWorld):

```
Conventions in the code (judged from 3 places: all must agree below 10, 75% from 10 on).
  off: mixed, or not what your setting says, so the rule that enforces it is turned off; both: mixed, both allowed;
  mixed: the default stays, nothing is turned off (no StyleBro rule enforces it, or your .editorconfig sets the rule):
  too few  private fields: 2 places -> default stays (camelCase)
  set      '{' of multi-line blocks: 196 of 196 on its own line -> set in .editorconfig (all)
  too few  operators where a line wraps: 1 place -> default stays (beginning_of_line)
  kept     multi-line initializers: 18 of 18 without a trailing comma -> omit
  default  empty strings: 3 of 3 string.Empty -> string_empty
  off      null checks: 7 'is null', 10 '== null' -> BRO1133 is off
  off      split lists: 13 first item on the next line, 17 first item after '(' -> BRO1107 is off
  ...
Summary:
  Kept your style for 5 settings (1 written, 4 already StyleBro's default): stylebro_trailing_comma.
  Turned 2 rules off because your code mixes both forms: BRO1133 (null checks), BRO1107 (split lists).
  To choose later: set the key in .editorconfig, remove its init line, run 'stylebro-migrate format'.
  4 settings had fewer than 3 places to tell, StyleBro's defaults stay: stylebro_private_field_naming,
    dotnet_style_operator_placement_when_wrapping, stylebro_equals_placement_when_wrapping, stylebro_summary_layout.
```

Each written line gets a comment in the block saying why:

```ini
# init: multi-line initializers: without a trailing comma in 18 of 18 places in your code
stylebro_trailing_comma = omit
# init: null checks: your code mixes both forms (7 'is null', 10 '== null'), so BRO1133 is off. To choose one: set stylebro_null_check_style and remove the next line.
dotnet_diagnostic.BRO1133.severity = none
```

#### Changing a detected setting later

Set the key in your own part of `.editorconfig`, outside the block (for example `stylebro_null_check_style =
equality_operator`), remove the rule's `severity = none` line from the block, and run `stylebro-migrate format` to bring
the code in line. The block is rewritten each time `init` runs, but a key you set yourself wins over what it counts: it
then writes neither the setting nor the `severity = none` line.

EF Core migrations (files with `[Migration(...)]`, a `Migration` base class or a `ModelSnapshot`) are written by
`dotnet ef`, so `init` marks their folders `generated_code = true`: formatting and StyleBro leave them alone. Vendored
folders (`vendor`, `vendored`, `third_party`, `thirdparty`, `external`) get the same section.

When it finds a StyleCop setup (`stylecop.json`, StyleCop rule ids in `.editorconfig`, rulesets or
global configs, a StyleCop.Analyzers reference), it stops without writing and tells you to run
`stylebro-migrate --write` instead: that's [path B](#b-coming-from-stylecop), which turns StyleBro's rules on only where
StyleCop enforced them.

With SonarQube or SonarCloud (a `SonarAnalyzer.CSharp` reference, Sonar rule severities in rulesets or configs, or a
quality profile you export and pass with `--sonar-profile profile.xml`), `init` also turns on the StyleBro and .NET rules
that fix what your Sonar rules report (S927 -> BRO1313, S4136 -> overloads kept together, ...) and prints which:
[Coming from SonarQube](migrating.md#coming-from-sonarqube). The same works for `stylebro-migrate --write` in path B.

### 3. Run stylebro-migrate format

```
stylebro-migrate format
```

This fixes whitespace, the built-in rules and every StyleBro rule. One fix can make work for another rule, so the
command runs `dotnet format` again until a run changes no file (at most three runs) and prints what each run changed
(`Run 1: 293 files changed:` and the first 10 of them, `Run 2: 0 files changed, clean.`); `--once` runs it once, with the same report and the findings kept in that run. It ends
with the findings StyleBro's fixes leave on purpose, each with its reason ([Findings kept on purpose](#findings-kept-on-purpose)),
and exits with 0 when clean, 2 when a run still changed files. When `dotnet format` skips a project because its
references didn't load (a broken restore or package cache), the command names it and says the run is incomplete (for
one target framework only: the code only that framework compiles); `--diff` stops then instead of showing a partial
preview, and `--verify-no-changes` fails when a project wasn't loaded in any run (a project that references an F#
project, which `dotnet format` can't load, stays a warning). When no project has StyleBro.Analyzers in its restore output (`obj/project.assets.json`), the command stops
before `dotnet format` runs: without the analyzers it would fix only whitespace and the built-in rules and report the
code clean. Projects without it next to ones with it are named (`Without StyleBro: ...`). The command restores once
before the runs; that restore skips NuGet's audit (`-p:NuGetAudit=false`): vulnerability warnings (NU1901-NU1904) are
the build's business, and with `TreatWarningsAsErrors` they failed every run. A solution filter (`.slnf`) works like
its solution, with only its projects. A folder with several solutions
or projects needs one named (`stylebro-migrate format MySolution.slnx`): the command lists them instead of picking one.
With none at the root, it looks for a Traversal project there (`dirs.proj` with `Sdk="Microsoft.Build.Traversal"`,
which `dotnet format` can't load: `format` runs on a temporary solution with the projects it lists), then for solutions
one folder down (`src/MySolution.slnx`): the only one is used, and the command says so (`Using src/MySolution.slnx`);
several are listed, as at the root. `baseline` and `--diff` find them the same way.
If `dotnet format` crashes, the command names the projects of that run, and when the crash came after some files were
written, lists every file it changed (`Run 1 failed after changing 12 files:`).
Review the diff and commit it. In an established codebase, land that commit when few pull requests are open (they
will conflict with it), and add its hash to `.git-blame-ignore-revs` so `git blame` skips it (GitHub reads that file
too; locally: `git config blame.ignoreRevsFile .git-blame-ignore-revs`). In a large codebase you may not want one big
change: see [Large codebases](#large-codebases).

**Why not plain `dotnet format`?** It also applies every other analyzer's fixes and the compiler's own, and some of
those change behavior or break the build: on one trial repository it added `required` to properties (CS8618) and a
Sonar fix removed `init;` accessors, nine build errors. `stylebro-migrate format` passes `dotnet format` only StyleBro's
rule ids and the built-in rules `init` or the migration turn on (`--diagnostics ...`); whitespace formatting runs as
usual. It also runs once per target framework where needed ([Multi-targeted repositories](#multi-targeted-repositories))
and never touches git submodules. The plain equivalent is `dotnet format --diagnostics` with the ids to fix:

```
dotnet format --diagnostics BRO1001 BRO1505 BRO1601 IDE0055 IDE0036 BRO1008
```

`stylebro-migrate format --all` applies everything, like plain `dotnet format`.

[What to expect on the first run](#what-to-expect-on-the-first-run): solution names, run time, warnings as errors.

### 4. Check it in CI

Run `stylebro-migrate format --verify-no-changes --severity warn` after the build: it changes nothing and fails (exit
code 2) when formatting would change a file. Findings kept on purpose don't fail it; plain
`dotnet format --verify-no-changes` fails on those too, so a script or an AI agent that loops "until clean" never gets
there with it. Set-up for GitHub Actions and Azure Pipelines: [ci.md](ci.md).

*Next release:* with `--agents-md`, `init --write` also puts a short StyleBro section into `AGENTS.md` for AI coding
agents (opt-in). For scripts and agents there are `format --files` (just the files an edit touched) and `--json`:
[Scripts and AI agents](agents.md).

## B. Coming from StyleCop

Still deciding? [StyleBro vs StyleCop](stylebro-vs-stylecop.md) compares the two with measured numbers.

`stylebro-migrate` reads your StyleCop setup (rulesets, global configs, `.editorconfig` files, `stylecop.json`) and
writes StyleBro and SDK settings that enforce the same things, so code StyleCop was happy with stays as it is. A
walk-through on a small project, with real output: [samples/StyleCopMigration](https://github.com/bisforboman/stylebro/blob/main/samples/StyleCopMigration/README.md).

### 1. Dry run

```
stylebro-migrate path/to/repo
```

It prints what it read, the settings it would write, and the StyleCop rules nothing enforces after the switch (dropped
by design, partly covered, or not covered yet). Nothing is changed. To also see what the settings and the format run
would do to the code: `stylebro-migrate path/to/repo --diff` ([Preview first](#preview-first)).

### 2. Write the settings

```
stylebro-migrate path/to/repo --write
```

This writes the settings into your `.editorconfig` files (between the `stylebro-migrate` markers), turns StyleBro's
preset off in the root `Directory.Build.props` (the settings replace it), and adds the replacing rule ids to existing
StyleCop suppressions (`#pragma warning disable`, `[SuppressMessage]`, `<NoWarn>`). You don't need `init` after this:
the block includes the built-in rules.

### 3. Swap the package

Remove the StyleCop reference and add `StyleBro.Analyzers` instead, as in [path A, step 1](#1-add-the-package). Look
for both package ids, `StyleCop.Analyzers` and the prerelease `StyleCop.Analyzers.Unstable`, and for a
`GlobalPackageReference` in `Directory.Packages.props` (central package management): replace it with
`<GlobalPackageReference Include="StyleBro.Analyzers" Version="..." />`. `stylecop.json` and the StyleCop suppressions
can stay; StyleBro ignores them.

### 4. Run stylebro-migrate format

```
stylebro-migrate format
```

Why not plain `dotnet format`: [step 3 of path A](#3-run-stylebro-migrate-format). In a StyleCop-clean repository this should change
little or nothing. Look at what it did change: usually code StyleCop
missed, or a rule where StyleBro deliberately differs ([differences-from-stylecop.md](differences-from-stylecop.md)).
Then check the report from step 1 for rules your team relied on that nothing enforces any more. Details:
[migrating.md](migrating.md). CI: [ci.md](ci.md).

## Newer C# and APIs (optional)

To also rewrite code into newer C# and newer APIs (target-typed `new()`, collection expressions, file-scoped
namespaces, `ArgumentNullException.ThrowIfNull`, ...) with the SDK's own rules, after either path:

```
stylebro-migrate init --modernize --write
dotnet format
```

In multi-targeted projects, StyleBro's multi-target guard hides the newer-API rules where a target framework lacks the
API, and the newer-C# rules are suggestions unless the project sets `LangVersion`. Tiers, examples and the rules left out:
[modernizing.md](modernizing.md).

## What to expect on the first run

- **Name the solution when the root has several.** With more than one project or solution file in the folder (say
  `Build.csproj` next to `MySolution.slnx`), `dotnet format` stops with MSB1011 ("more than one project or solution
  file"). Pass the one to format: `dotnet format MySolution.slnx` (the same for `stylebro-migrate format`).
- **Multi-targeted repositories take longer.** `stylebro-migrate format` runs `dotnet format` once per target
  framework, and each run loads the solution again. That's minutes, not seconds: Dapper, which builds in 13 seconds,
  took 345 seconds to format.
- **`TreatWarningsAsErrors` fails the first build** until everything is fixed or recorded: StyleBro's findings are
  warnings, so they become errors. Run `dotnet format` (or `stylebro-migrate format`) before building, or record what's
  left in a [baseline](baseline.md).
- **Some renames are left to you.** The naming rules don't rename a member whose name also appears in a string
  (reflection, serialized names, `DebuggerDisplay`): the rename would compile but break at run time. Those warnings
  stay after `dotnet format`: see [Findings kept on purpose](#findings-kept-on-purpose).

## Findings kept on purpose

Every StyleBro finding has a fix, but a fix can see more than the analyzer: the whole solution, other projects
included. When a change would break something only the fix can see, it leaves the finding, the warning stays, and the
fix offers no code action for it. That's the one exception to "`dotnet format` fixes everything StyleBro reports",
and it's on purpose. The reasons, per finding:

- the name is in a string elsewhere in the solution (serialized names and test JSON for properties and public fields;
  for other fields only where code reaches a member by name: `GetField("_count")`, `GetProperty(...)`, `Type.GetType(...)`,
  `[MemberData("...")]`, an attribute's `Name = "..."`): the rename would compile but break at run time;
- the name is in a `nameof(...)` elsewhere, whose text would change with it (an HTTP header name, a log key);
- another member would hide the new name, or the new name already means something else where the name is used;
- a use is in generated code (a source generator, a Razor page, a designer file), which a tool writes again;
- a related override or implementation is public API, or also implements a member that isn't renamed;
- the rename would add compile errors (tuple element names), or the namespace is also defined outside the solution.

`stylebro-migrate format` lists them after its last run:

```
Run 2: 0 files changed, clean.
Clean: 1 finding kept on purpose (listed below). StyleBro's fixes leave these: renaming by hand breaks what the check protects. Check those uses first, or suppress or baseline them (...).
  src/Counter.cs(5,17): BRO1303 Rename '_count' to 'count'. Kept: the name is in a string in the solution (reflection, serialization): renaming would compile but break at run time.
```

What to do with one: **don't rename by hand to silence it** (that's what breaks the reflection or the generated code).
Check the uses the reason names; if they can change too, rename both by hand. Otherwise suppress it with a reason
(`#pragma warning disable BRO1303 // read by reflection in Tests`), or record it in the [baseline](baseline.md).

**With `TreatWarningsAsErrors`** the kept findings are build errors after the first run, so make dealing with them part
of it: run `stylebro-migrate format`, then either suppress each one as above or run `stylebro-migrate baseline` (it
records exactly what is left, the kept findings included), and commit that together with the formatting. The list is
printed after every run, also when `format` ends "Not clean".

Exit codes of `stylebro-migrate format`, for CI and agents:

| Exit code | Plain run | `--verify-no-changes` |
|---|---|---|
| 0 | clean: the last run changed nothing (kept findings don't count) | formatting would change nothing; only kept findings are left |
| 2 | still changing after three runs | formatting would change a file |
| 1 | | a project no run loaded (`Incomplete:`): its files weren't checked |
| other | `dotnet format` failed (load or build errors) | the same |

`--verify-no-changes` changes nothing: when `dotnet format --verify-no-changes` reports findings, it formats a temporary
copy of the repository to see whether any file would change and which findings the fixes keep, then deletes the copy.
Plain `dotnet format --verify-no-changes` can't tell the two apart and fails on kept findings.

## Excluding vendored code

Code you don't own (a copied library, generated sources checked in, a `ThirdParty` folder) shouldn't be reformatted.
Mark it as generated code in the root `.editorconfig`:

```ini
[ThirdParty/**]
generated_code = true

# Entity Framework Core migrations: generated by 'dotnet ef', regenerated rather than edited
[**/Migrations/**]
generated_code = true
```

EF Core's migration files (`Migrations/*.cs` and the model snapshot) are generated but carry no `<auto-generated>`
header, so without this the first run reformats every one of them (291 files in one trial repository).

StyleBro's rules, the built-in rules (IDE0055, IDE0073, ...) and `dotnet format`'s whitespace pass all skip generated
code, in the build and in `dotnet format`. One side effect: the compiler treats generated files as nullable-oblivious
unless they say `#nullable enable` themselves.

`dotnet_analyzer_diagnostic.severity = none` in that folder isn't enough: a rule turned on by its id
(`dotnet_diagnostic.BRO1001.severity = warning` in the root) wins over it. `stylebro-migrate --write` therefore writes
every replacing rule as `none` into such a folder's `.editorconfig`, and it never edits files inside git submodules
(folders with their own `.git`).

**Files linked from a submodule.** A project can compile files from another folder (`<Compile Include="..\..\ext\Lib\Code.cs" />`,
or a shared `.projitems`). They're checked like the project's own files, but `stylebro-migrate format` never formats
anything inside a git submodule (it passes `--exclude` with every submodule folder), so their warnings stay. Mark the
submodule's folder as generated code too, with a section for its path in the root `.editorconfig` (`[ext/**]`, then
`generated_code = true`).

## Multi-targeted repositories

If any project sets several `<TargetFrameworks>`, plain `dotnet format` doesn't work at all; `stylebro-migrate format`
does. It takes the same arguments and options:

```
stylebro-migrate format
stylebro-migrate format MySolution.sln --verify-no-changes --severity warn
```

Plain `dotnet format` crashes there on the SDK's formatting fix (IDE0055) without writing anything (reported upstream as [dotnet/roslyn#86000](https://github.com/dotnet/roslyn/issues/86000) and [dotnet/roslyn#86001](https://github.com/dotnet/roslyn/issues/86001)). The command runs
`dotnet format` once per target framework instead; without multi-targeted projects it's one run. A diagnostic
every framework sees is printed once, and `--report` writes one `format-report.json` covering all runs. Spellings of
one framework share a run (`net6` and `net6.0`, NuGet's short names). A run loads only the projects that target its
framework, so the renaming fixes also check the names in every other C# file of the repository: a field the tests
read with `GetField("count")` keeps its name in the library's run too, and a name a project outside the run uses
(`UsedInProjectNotLoaded`) is left to a run that loads both, or kept. Files a project compiles from outside its own
folder (`<Compile Include="..\Shared\*.cs" />`) are formatted in the runs of the projects that compile them, and
`--files` finds such a file's project too. Projects that compile the same files (a library and its AOT variant
compiling `..\Lib\*.cs` with other `DefineConstants`) run in separate parts of a framework's run (`== net8.0, part 1
of 2`), each with what it references: loaded together, `dotnet format` crashes on their different edits of one file
([dotnet/roslyn#86001](https://github.com/dotnet/roslyn/issues/86001)). This applies without multi-targeted projects
too. Why:
[ci.md](ci.md#notes). `stylebro-migrate init` tells you when it finds such projects.

A project `dotnet format` can't load at all (F#, `.fsproj`) makes it skip the C# projects that reference it, for the
frameworks where the reference matters. `format` names them once, with that cause ("Partly incomplete: ... Likely
cause: they reference TestFSharp.fsproj"); a restore doesn't change it, and the rest of the repository is formatted.

## Old-style (non-SDK) projects

Projects in the old format (`<Project ToolsVersion="15.0" ...>` without `Sdk=`, usually .NET Framework with
`packages.config`) work with `stylebro-migrate format`, with a few extra steps:

- **Restore with Visual Studio's MSBuild first**, from a Developer Command Prompt:

  ```
  msbuild -t:restore -p:RestorePackagesConfig=true
  ```

  `dotnet restore` can't restore `packages.config`, and for the other old-style projects it writes restore output that
  Visual Studio's MSBuild then rejects ("Your project file doesn't list 'win' as a RuntimeIdentifier"). So `format`
  restores only the SDK-style projects when it finds old-style ones, and says so (the `-p:RestorePackagesConfig=true`
  part only matters with `packages.config`). If a build complains like that anyway (something ran
  `dotnet restore`), run the MSBuild restore again.
- **Reference assemblies.** `dotnet format` loads the projects with the .NET SDK, which needs the .NET Framework
  reference assemblies: without them every project is skipped ("Required references did not load"). Install the
  targeting pack (the .NET Framework Developer Pack, or Visual Studio's ".NET Framework 4.x targeting pack"), or use the
  `Microsoft.NETFramework.ReferenceAssemblies.net462` NuGet package (the version your projects target) and point
  MSBuild at it with an environment variable, which reaches both `msbuild` and `dotnet format`:
  `set TargetFrameworkRootPath=%NUGET_PACKAGES%\microsoft.netframework.referenceassemblies.net462\1.0.3\build`.
- **Install StyleBro.Analyzers the packages.config way**, like StyleCop was: Visual Studio's package manager
  (or `Install-Package StyleBro.Analyzers` in its console) writes all three parts, or add them by hand. In
  `packages.config`:

  ```xml
  <package id="StyleBro.Analyzers" version="0.5.0-alpha.1" targetFramework="net462" developmentDependency="true" />
  ```

  In the project (paths relative to it; the `packages` folder is next to the solution):

  ```xml
  <ItemGroup>
    <Analyzer Include="..\packages\StyleBro.Analyzers.0.5.0-alpha.1\analyzers\dotnet\cs\StyleBro.Analyzers.dll" />
    <Analyzer Include="..\packages\StyleBro.Analyzers.0.5.0-alpha.1\analyzers\dotnet\cs\StyleBro.CodeFixes.dll" />
  </ItemGroup>
  <!-- at the end, after the Microsoft.CSharp.targets import: the preset and the baseline -->
  <Import Project="..\packages\StyleBro.Analyzers.0.5.0-alpha.1\build\StyleBro.Analyzers.targets" Condition="Exists('..\packages\StyleBro.Analyzers.0.5.0-alpha.1\build\StyleBro.Analyzers.targets')" />
  ```

  plus an `<Error Condition="!Exists('...StyleBro.Analyzers.targets')" ... />` line in the project's
  `EnsureNuGetPackageBuildImports` target, as NuGet adds for every package with build targets. Don't use a
  `PackageReference` there: it makes NuGet restore the project the PackageReference way, its `packages.config` packages
  are no longer restored, and a fresh clone doesn't build. A `PackageReference` in a `Directory.Build.props` that also
  serves SDK-style projects needs a condition that keeps it away from them:

  ```xml
  <PackageReference Include="StyleBro.Analyzers" Version="0.5.0-alpha.1" PrivateAssets="all" Condition="'$(UsingMicrosoftNETSdk)' == 'true' or !Exists('$(MSBuildProjectDirectory)\packages.config')" />
  ```

  `stylebro-migrate` names the packages.config projects and says this in its report.
- **`--diff` doesn't work** for `packages.config` projects: the copy it formats has no git-ignored `packages` folder,
  and `dotnet restore` can't restore `packages.config`. It says so and stops. Other old-style projects preview like
  `format` runs: unrestored, so the preview has no StyleBro changes for them, and it says which. Instead, on a clean working tree: run the command without
  `--diff`, restore with MSBuild as above, run `stylebro-migrate format`, and review `git diff` (`git checkout .`
  undoes it).
- If StyleCop came in through `packages.config` or an `<Analyzer Include="..\packages\StyleCop.Analyzers.1.0.2\...">`
  hint path, `stylebro-migrate` reads its version there: rules that version doesn't have stay off.

## Large codebases

If `dotnet format` would change too much at once, record today's violations in a baseline instead:

```
stylebro-migrate baseline                            # in the folder with your solution
stylebro-migrate baseline --project MySolution.sln   # or name it
```

This writes `stylebro.baseline`; commit it. The build, the IDE and `dotnet format` then ignore those violations, and
only new code has to follow the rules. A baseline can't hide whitespace formatting. Details: [baseline.md](baseline.md).

**How long a full run takes.** `format` loads the whole solution once per run, and in a multi-targeted repository once
per target framework. On Orleans (about 200 projects, several target frameworks) the first `stylebro-migrate format`
took 57 minutes and about 9 GB of memory, and `format --verify-no-changes` 11.5 minutes. A repository that size with
`TreatWarningsAsErrors` is best started with a baseline: the build passes right away, and the backlog can be fixed a
project at a time (`stylebro-migrate format path/to/Project.csproj`) or a few files at a time (`--files`).
