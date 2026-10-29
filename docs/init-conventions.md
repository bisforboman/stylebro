# What init keeps from your code

[Getting started](getting-started.md), step 2: before writing its block, `bro init` looks at your code and keeps
the conventions it clearly follows. This page says how it counts and what it can keep.


`init` looks at your code first and keeps the conventions it clearly follows. For each setting below it counts both
forms in your C# (generated code, EF Core migrations, vendored folders, submodules and folders whose own `.editorconfig`
turns every analyzer off or says `generated_code = true` don't count; code inside `#if`
counts too, as `format` sees it in the target framework that defines the symbol):

Only C# files a project compiles count: a file below no project (a Unity `Assets/` folder) doesn't, unless a project
or a `Directory.Build` file has a Compile item reaching outside its folder (`..` or a property) or a shared project
(`.projitems`) could include it. A file `init` can't read (open in another program) isn't counted, and the report says
which ones; close them and run `init` again.

Vendored folders are recognized by their name (`vendor`, `third_party`, ...), or by a license file of their own whose
copyright holder isn't your root license's (a fork copied in with its `LICENSE`). A license template's placeholders
(Apache's `Copyright [yyyy] [name of copyright owner]`, MIT's `<year> <copyright holders>`) name no holder, so such a
license decides nothing either way. When a license file can't be read, its folder isn't counted and isn't marked. Code copied in under another name
without a license of its own (a `src/Sdk` from another product) counts as yours and can decide a convention; mark its folder `generated_code = true` in
your root `.editorconfig` (`[src/Sdk/**]`, see [excluding vendored code](vendored-code.md#excluding-vendored-code)) and run `init` again:
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
rewrite the code. The same goes when the code mixes both forms under such a key (the starter kit says
`beginning_of_line`, 486 operators start the line and 285 end it): nothing enforced the key, so the rule goes off as for
any mixed convention (`.editorconfig says beginning_of_line, unenforced, code mixed: BRO1520 is off`). Running `init`
again keeps such a line, even when `format` changed the counts.

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
`bro migrate --write` writes the whole block into such a folder too.

A few settings work a little differently:

- **One-line statements** (`if (a) return;`, `case 1: a(); break;`): the preset sets
  `csharp_preserve_single_line_statements = false`, which moves each to a line of its own. From 3 such statements on,
  `init` writes `true`, majority or not, unless your `.editorconfig` sets the key.
- **`<inheritdoc/>` on overrides and implementations** ([BRO1601](rules/BRO1601.md)): in projects that generate
  documentation, `init` counts the overrides and interface implementations BRO1601 checks, with and without a doc
  comment. Mostly documented: the rule stays on; mostly undocumented or mixed: it's turned off.
- **Line endings** count files. Without `end_of_line` (or with `end_of_line = unset`), `format` writes your OS's line ending into the lines it rewrites
  (on Windows CRLF into LF files), so `init` writes the files' ending when it's clear and nothing when it's mixed. Files
  git converts on checkout (`core.autocrlf`, or a `.gitattributes` `text` attribute without `eol`: LF in the repository,
  CRLF in a Windows checkout) don't count, and the report names which of the two converts them: there the OS's ending is
  what the checkout has, and a fixed one would make `format` rewrite every file on the other OS. When git converts at
  least as many files as it leaves alone, those are the convention and nothing is written: a few files committed with
  CRLF don't decide for the rest (one trial repository had 3 such files next to 145 converted ones, and
  `end_of_line = crlf` from those 3 meant 12,593 line-ending errors in an LF checkout). `bro migrate --write`
  writes `end_of_line` the same way. When your `.editorconfig` sets `end_of_line` but git converts the files on
  checkout (`end_of_line = lf` with `*.cs text=auto` means CRLF files in a Windows checkout), `init` keeps endings out of
  it: `end_of_line = unset` for C# files and IDE0055 as a suggestion, so neither `format` nor the build touches them, and
  the report says how to settle it (`*.cs text eol=lf` in `.gitattributes`, or removing `end_of_line`). When your
  `end_of_line` says one ending but the files git leaves alone clearly have the other (`crlf`, with nearly every file LF),
  nothing ever enforced it: `init` writes the files' ending for C# files, so `format` doesn't flip every file it touches,
  and the report says how to switch to the key's ending instead. A file with the other ending that holds a string literal
  spanning lines (`@"..."`, `"""..."""`) keeps its ending in a section of its own (`[tests/Render.cs]` +
  `end_of_line = crlf`): converting it would change the literal's value, and a test comparing text with it would fail.
- **`s_`/`t_` prefixes** count private static fields (constants aside) with and without the .NET runtime's `s_` (or `t_`)
  prefix. Mostly without: [BRO1307](rules/BRO1307.md) stays on; mostly with: `init` writes `s_camelCase` for the kinds
  that have it (`stylebro_private_static_field_naming` for `static readonly` fields,
  `stylebro_private_mutable_static_field_naming` for the others), so BRO1307 keeps the `s_`; mixed (or one kind mixed):
  it's turned off, so `format` doesn't rename them (there is no switch for static fields alone, so `m_` fields keep their
  prefix too). A `dotnet_naming_rule` of yours that requires `s_` decides instead.
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
- **Implicit `private`** ([BRO1404](rules/BRO1404.md)) counts the members of classes, structs and records that are private
  when nothing says otherwise (fields, methods, properties, events, constructors, nested types; not what BRO1404 skips:
  partial members, explicit implementations, static constructors): with `private` or without an access modifier. Mostly
  with: the default stays; mostly without: `dotnet_style_require_accessibility_modifiers = omit_if_default`, so neither
  BRO1404 nor [BRO1007](rules/BRO1007.md) adds modifiers (IDE0040, which would remove them, is off in `init`'s block);
  mixed: BRO1404 is turned off. Interface members are counted apart: when the `.editorconfig` says `always` (which asks
  for `public` on interface members too), the build doesn't enforce it (no `EnforceCodeStyleInBuild`), and the code's
  interface members are mostly without an access modifier or mixed, `init` writes `for_non_interface_members`, so
  BRO1404 adds no `public` there (BaGetter: none of its 102 interface members had one).
- **Implicit `internal`** ([BRO1404](rules/BRO1404.md)) counts the types in a namespace or a file (not partial ones):
  with `internal` or without an access modifier. No setting asks for modifiers on types only, so mostly without, or
  mixed, turns BRO1404 off (AngleSharp: 273 types without, 26 with `internal`).
- **`case` blocks** count blocks below a `case` label (`case X:`, then `{` on the next line): indented one level below
  the label or at its level. Mostly at its level: `csharp_indent_case_contents_when_block = false`, so the SDK
  formatter (IDE0055) keeps them (AngleSharp: all 131 at the label's level).
- **Combined declarations** ([BRO1142](rules/BRO1142.md) for locals, [BRO1114](rules/BRO1114.md) for fields) count
  declarations of two or more (`int n = h.Length, i = 0;`) and one-variable declarations right after one of the same type
  and modifiers they could have been combined with (not `var`, not fields with attributes). Mostly one by one: the rule
  stays on; mostly combined or mixed: it's turned off, so `format` doesn't split them. Locals and fields are counted apart:
  a repository may combine locals in hot loops and never fields.
- **Combined attributes** ([BRO1102](rules/BRO1102.md)) count declarations with two or more attributes (not parameters
  or type parameters): in one list (`[A, B]`) or one per list. Mostly one per list: the rule stays on; mostly combined or
  mixed: it's turned off, so `format` doesn't split them.
- **Summary wording** ([BRO1604](rules/BRO1604.md) for properties, [BRO1606](rules/BRO1606.md) for constructors) counts
  summaries that start the StyleCop way (`Gets ...`, `Gets or sets ...`; `Initializes a new instance of ...`) against
  ones in their own words (`The samples, ...`, `Whether ...`; `Opens a file ...`). Mostly the StyleCop way: the rule
  stays on; mostly their own words or mixed: it's turned off, so `format` doesn't reword public documentation. A summary
  that starts with an element (`<see cref="..."/>`) isn't counted.
- **CSharpier**: when the repository uses [CSharpier](https://csharpier.com) (a `.csharpierrc`, `.csharpierrc.json`,
  `.csharpierrc.yaml` or `.csharpierrc.yml`, or a `csharpier` tool in `.config/dotnet-tools.json` or
  `dotnet-tools.json`, at the root or in a solution's or project's folder or a folder above it; or a `CSharpier.MsBuild`
  package reference), the two formatters would undo each other's layout
  (StabilityMatrix's pre-commit check rejected 161 files StyleBro had formatted). `init` then turns off IDE0055 (so
  `bro format` skips the whitespace pass; when CSharpier formats only some folders, IDE0055 is off in those
  folders' sections, one section per folder CSharpier formats, and the whitespace pass leaves just them out), IDE0036
  (CSharpier orders modifiers itself, and the two orders differ: `file static` against `static file`) and every StyleBro rule for something CSharpier decides, and the
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
  sections). `init` also writes `stylebro_layout_by = csharpier` and CSharpier's line width
  (`stylebro_layout_print_width`: `printWidth` from the `.csharpierrc`, else `max_line_length`, else 100): an `if` merged
  into the enclosing one ([BRO1149](rules/BRO1149.md)) gets its condition the way CSharpier writes it, and the brace
  rules count a statement CSharpier will split over lines as multi-line, so its braces come in the same run. When
  CSharpier formats only one folder (its tool or config there, with C# code outside it), these settings go into that
  folder's section (`[Source/**.cs]`) and the rest of the code is judged as usual. CSharpier
  sorts using directives itself (System first), so `init` writes no sort key (`dotnet format` sorts usings whenever
  `dotnet_sort_system_directives_first` or `dotnet_separate_import_directive_groups` is set, and its order differs from
  CSharpier's in places: `App3._0` against `App3_0`). Where your `.editorconfig` sets one (`false`, or `true` for the
  groups), `init` writes the closer value in its block: a key can't be unset. `bro format` runs
  CSharpier itself on the C# files each run changed (the repository's `csharpier` tool, restored first, or the copy the
  CSharpier.MsBuild package brings; whole folders where CSharpier would change nothing else, since each file named on
  its command line costs it ~80 ms): other fixes that rewrite code (attributes split, parentheses added, `if`s merged)
  leave lines in StyleBro's layout, which CSharpier then lays out its way, and StyleBro leaves that alone, so one
  `format` leaves a tree `dotnet csharpier check` accepts. `--verify-no-changes` checks both: StyleBro's rules, and
  CSharpier's check on the C# files it formats (and on the Directory.Build.props and Directory.Packages.props files
  `init` edits). Where `end_of_line` is `lf` or `crlf`, `format` also runs CSharpier on the C# files whose line endings
  differ from it, since CSharpier's check fails on those too. Where CSharpier puts back what a fix changed (IDE0036's
  `static file` against CSharpier's `file static`), no run can settle it: `format`, `--verify-no-changes` and the
  preview say so (the preview exits 2 like `format`), name the files and the rules whose edits CSharpier undoes, and
  say how to turn those off (`init` turns IDE0036 off with CSharpier). With only a `.csharpierrc` and no tool or package, `format` says to run CSharpier.
  `bro migrate --write` (from StyleCop) does the same. To have StyleBro format instead, remove CSharpier and run
  `init` again.
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
  when an enclosing namespace has a namespace or type your code declares with its first part (`using H = Helper;` next
  to a class `App.Helper`), or, inside a `System` or `Microsoft` namespace, one of the runtime's (`using Text;` inside
  `namespace System.IO.Abstractions`); a namespace from a package doesn't count.
- **Charset** and **final line breaks**: when your `.editorconfig` sets `charset` (`utf-8` or `utf-8-bom`) or
  `insert_final_newline` but the C# files don't clearly follow it (BOM or not, last line break or not, the same 75 %),
  nothing ever enforced it, and `format` would add or strip the BOM or the last line break in every file. Like a line
  ending the files contradict, `init` writes the files' value for C# files (`unset` when they're mixed: each file keeps
  its own), and the report says how to switch (remove the line and run `bro format`). Ical.Net:
  `charset = utf-8` with 108 of 223 files starting with a BOM, all 108 stripped; the starter kit:
  `insert_final_newline = false` with 1,092 of 1,597 files ending in a line break, all 1,092 removed. `bro
  migrate --write` does the same. When the files do follow it but a few don't (BaGet: `charset = utf-8`, 16 of 248 files
  with a BOM), the key stays and the report says how many files `format` changes (with IDE0055 on), so you can change
  them in a commit of their own first; the same for a few files against your `end_of_line`. `trim_trailing_whitespace = false` can't be kept this way: `dotnet format` doesn't read the key
  and trims trailing whitespace on every line it formats, so the report only says so (with IDE0055 off nothing is trimmed).
  These keys (and `end_of_line`) are judged per file against the `.editorconfig` that decides it, as the compiler reads
  them: the nearest one that sets the key wins, within it the last matching section. A nearer file overrides anything
  the root block says, so when a folder's own `.editorconfig` (one without `root = true`, like Bogus's `Source/`) sets
  the key, `init` writes its line there, in a block between `# BEGIN stylebro-folder` and `# END stylebro-folder`
  (Bogus: the root block's `insert_final_newline = unset` never reached `Source/`, and `format` stripped 48 files' last
  line break).
- **Spacing** that IDE0055 enforces counts places on one line: a space after `if`/`for`/`foreach`/`while`/`switch`/
  `using`/`lock`/`catch` or not (`if (x)` or `if(x)`), spaces inside parentheses (control-flow statements, parenthesized
  expressions, casts), after a cast (`(int) x`), and inside call and parameter lists (`F( x )`). A clear form other than
  the SDK's default is written (`csharp_space_after_keywords_in_control_flow_statements`,
  `csharp_space_between_parentheses`, `csharp_space_after_cast`,
  `csharp_space_between_method_call_parameter_list_parentheses`,
  `csharp_space_between_method_declaration_parameter_list_parentheses`). When any of them is mixed, IDE0055 is turned
  off with one `# init: spacing:` note, since it can't be limited to one option, and `format` skips the whitespace pass
  (Bogus: `foreach( var x in y )` in 341 places, 65 %, which IDE0055 had rewritten). With CSharpier, column alignment or
  an IDE0055 severity of your own, nothing is written.
- **Blank lines between members** ([BRO1505](rules/BRO1505.md)) count the neighbouring members of a type that need one
  (one spans several lines, or is documented), with a blank line between them or without; **blank lines above doc
  comments** ([BRO1513](rules/BRO1513.md)) count doc comments right below code or after a blank line. Mostly with: the rule
  stays on; mostly without or mixed: it's turned off, so `format` doesn't add blank lines to compact code (Kavita: 392
  and 312 files).
- **Blank lines below `//` comments** ([BRO1506](rules/BRO1506.md)) count the comments it checks (the last line of a
  comment group; not the file header, trailing comments or comments below code) right above the code or with a blank line
  below. Mostly right above: the rule stays on; mostly with a blank line or mixed: it's turned off (RecyclableMemoryStream
  keeps one below commented-out tests).
- **`this.`**: when your `.editorconfig` requires it (a `dotnet_style_qualification_for_*` key `= true:warning`, or
  `= true` with IDE0009 at `suggestion` or above, by its id, the `Style` category's severity or the bulk one), the block
  leaves IDE0009 alone instead of turning it off.
- **Interface members**: `dotnet_style_require_accessibility_modifiers = always` asks for `public` on interface members
  too. When your code doesn't have them and the build doesn't enforce it (`EnforceCodeStyleInBuild` with IDE0040 at
  warning or error by id, category or bulk; a `:warning` after the value doesn't count in the build), `init` writes
  `for_non_interface_members`, so [BRO1404](rules/BRO1404.md) doesn't add them.
- **`else`/`catch`/`finally` placement** counts only blocks that span lines: in a one-line method
  (`{ if (x) { return 1; } else return 2; }`) the layout isn't a choice.
- **Blank lines below a type's `{`** ([BRO1503](rules/BRO1503.md)) count the `{` of namespaces and types (not blocks in
  members, empty or one-line bodies, or `{ // note`): right above the code inside or with a blank line below. Mostly right
  above: the rule stays on; mostly with one or mixed: it's turned off (cecil puts one below 543 of 720).
- **Multi-word private field names** ([BRO1308](rules/BRO1308.md)) count private fields whose name has two words or more:
  camelCase (`breakTarget`) or snake_case (`break_target`); one-word names fit both, and `m_`-style prefixes are the
  prefix rules'. Mostly camelCase: the rule stays on; mostly snake_case or mixed: it's turned off (mono.linq.expressions
  names 11 of its 13 multi-word fields snake_case).
- **Blank lines below doc comments** ([BRO1511](rules/BRO1511.md)) count doc comments right above their element or with
  a blank line between. Mostly right above: the rule stays on; mostly with one or mixed: it's turned off (MoreLINQ).
- **Primary-constructor parameters** ([BRO1302](rules/BRO1302.md)) count those named like parameters (`logger`) or like
  the fields they replace (`_logger`). Mostly `_logger` or mixed turns BRO1302 off: it has no switch for these
  parameters alone, so it's then off for every parameter (the `# init:` line says so).
- **Tuple element names** ([BRO1311](rules/BRO1311.md)) count `(int Count, ...)` and `(int count, ...)`. Mostly camelCase
  writes `stylebro_tuple_element_name_casing = camelCase`; mixed turns the rule off.
- **Preset version**: `init --write` sets `<StyleBroPreset>` in each `Directory.Build.props` your projects read, but not
  in one whose projects no solution lists and that doesn't mention StyleBro.Analyzers (example or test-fixture folders).
  A `Directory.Build.props` it has to create below another one imports that one, so nothing the projects read gets lost.
- **Run from a subfolder**: when the folder you run `init` (or `bro migrate --write`) in is part of a git repository
  whose build reaches past it (a `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig` or `.globalconfig`
  above it, a solution in it that lists projects outside it, like `build/App.sln`, a solution above it that lists its
  projects, a project in it that references one outside it, or no project in it at all), it works on the repository's
  root and says so. A folder with its own `Directory.Build.props` or `root = true` `.editorconfig`, or a self-contained
  folder no solution above lists, stays on its own.
- **`.globalconfig`**: the root's `.globalconfig` counts like its `.editorconfig` for what your team set and enforces (a
  `this.` it requires, its IDE0055 severity, the conventions' keys). With `AnalysisMode` `All` and
  `EnforceCodeStyleInBuild`, every code-style rule is a warning in the build, so a `dotnet_style_qualification_for_* = true`
  without a severity counts as enforced.
- **A folder that turns the Style category off** (`dotnet_analyzer_diagnostic.category-Style.severity = none` in its
  `.editorconfig`): a rule's own entry wins over a category, so `init` turns the built-in rules its block sets by id off
  there too (in the folder's own block), and says so.
- **A folder that turns every analyzer off** without `root = true` inherits the block's using order key, and `dotnet
  format` sorts usings wherever one is set: `init` writes the key into a section for the other C# files instead
  (`[{src,tests}/**.cs]`), and says so. With C# files directly in the root no section can leave the folder out; the
  report says its usings get sorted.
- **Column alignment**: when your `.editorconfig` aligns code in columns for ReSharper or Rider
  (`resharper_int_align_variables = true`, `resharper_csharp_int_align_fields = true` and the other `int_align` keys;
  the comments' alignment aside), IDE0055's whitespace formatting would flatten it (RestSharp:
  `var doc      = CreateAttributesXml();` became `var doc = CreateAttributesXml();` in 171 files). `init` turns IDE0055
  off, with a `# init:` comment, so `bro format` skips the whitespace pass. No StyleBro rule changes the
  spaces inside a line, so they stay on. An IDE0055 severity of your own wins.
- **One-line jumps** (`if (x) return;`, also `break`, `continue`, `throw`, `goto`, `yield break` on the `if` line): from
  3 of them on, `init` writes `stylebro_allow_single_line_jump_statements = true`, majority or not, so
  [BRO1514](rules/BRO1514.md) doesn't brace them (the starter kit: 878 of 1,130 bodies with braces, and BRO1514 braced
  194 one-line jumps). Not when your .editorconfig enforces braces (IDE0011 at warning or error, with
  `csharp_prefer_braces = true` or unset): those jumps are your build's warnings, and BRO1514 braces them.
- **`#region`s inside members** ([BRO1113](rules/BRO1113.md), the starter kit's `#region Arrange` / `Act` / `Assert` in
  its tests): from 3 of them on, the rule is turned off, so `format` doesn't remove them. [BRO1112](rules/BRO1112.md)
  (regions between members) is off in the preset already.
- **Using order** counts files with 2 or more usings: sorted with System first, sorted without, or not sorted. `dotnet
  format` sorts usings whenever `dotnet_sort_system_directives_first` is set (to any value), so the preset doesn't set
  it: `init` writes it when your usings are sorted (or there are too few files to tell), and only a comment when they
  aren't, or when some files put System first and others don't. When your `.editorconfig` sets the key itself and 3 or
  more files aren't in its order, the report says how many `format` will reorder and that removing the key keeps them
  (the starter kit: `false`, 262 of 1,120 files); `init` can't do it for you, since `unset` sorts them too.
- **No root `.editorconfig` without code for it**: when all your C# sits in folders whose `.editorconfig` says
  `root = true` (the starter kit's `src/`) and the root has no `.editorconfig`, `init` writes none there. Likewise the
  preview adds the package reference only where projects with C# files read it (a template pack project without code
  doesn't get a root `Directory.Build.props`).

| Setting | What's counted | Rule | Key |
|---|---|---|---|
| Private field names | `field` or `_field` | [BRO1303](rules/BRO1303.md) | `stylebro_private_field_naming` |
| Multi-word private field names | camelCase or snake_case (`break_target`) | [BRO1308](rules/BRO1308.md) | `dotnet_diagnostic.BRO1308.severity` |
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
| Leading multi-line lambdas | calls whose first argument is a multi-line lambda, anonymous method or anonymous object: the other arguments on its last line (`}, Kind);`) or one argument per line (mixed: both allowed; where they're allowed, those calls don't count for the split-list rows above) | [BRO1107](rules/BRO1107.md), [BRO1108](rules/BRO1108.md) | `stylebro_allow_leading_multiline_lambda` |
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
| `s_`/`t_` prefixes | private static fields with or without the `s_`/`t_` prefix (mostly with: `s_camelCase`) | [BRO1307](rules/BRO1307.md) | `dotnet_diagnostic.BRO1307.severity`, `stylebro_private_static_field_naming`, `stylebro_private_mutable_static_field_naming` |
| Implicit `private` | members that are private by default, with or without `private` | [BRO1404](rules/BRO1404.md) | `dotnet_style_require_accessibility_modifiers` |
| Implicit `internal` | types that are internal by default, with or without `internal` | [BRO1404](rules/BRO1404.md) | `dotnet_diagnostic.BRO1404.severity` |
| Partial parts | `partial class X` parts with or without an access modifier | [BRO1007](rules/BRO1007.md) | `dotnet_diagnostic.BRO1007.severity` |
| `case` blocks | a block below a `case` label indented or at the label's level | SDK formatter (IDE0055) | `csharp_indent_case_contents_when_block` |
| Combined attributes | `[A, B]` or one attribute per list | [BRO1102](rules/BRO1102.md) | `dotnet_diagnostic.BRO1102.severity` |
| Combined locals | `int a, b;` or one local per declaration | [BRO1142](rules/BRO1142.md) | `dotnet_diagnostic.BRO1142.severity` |
| Combined fields | `int _a, _b;` or one field per declaration | [BRO1114](rules/BRO1114.md) | `dotnet_diagnostic.BRO1114.severity` |
| Type names | files with only keywords (`int`) or framework names (`Int32`) | IDE0049 | `dotnet_diagnostic.IDE0049.severity` |
| Private constants and static readonly fields | `PascalCase`, `camelCase` or `_camelCase` | [BRO1306](rules/BRO1306.md) | `stylebro_private_static_field_naming` |
| Usings inside a namespace | fully qualified or relative to an enclosing namespace | [BRO1126](rules/BRO1126.md) | `dotnet_diagnostic.BRO1126.severity` |
| One-line jumps | `if (x) return;` on the `if` line, or a lone jump in braces | [BRO1514](rules/BRO1514.md) | `stylebro_allow_single_line_jump_statements` |
| Empty blocks | `{ }` on one line or on two (mixed: both allowed; on one line also writes `csharp_preserve_single_line_statements = true`, else IDE0055 splits `else { }`) | [BRO1508](rules/BRO1508.md), [BRO1509](rules/BRO1509.md) | `stylebro_allow_empty_single_line_blocks` |
| One-line properties and methods | properties with a block-bodied accessor on one line (`X { get { return x; } }`) or on several; method bodies with one statement that fits a line (`int Next() { return ++count; }`), counted apart: the one that keeps one-line bodies decides | [BRO1509](rules/BRO1509.md) | `dotnet_diagnostic.BRO1509.severity` |
| One-line statement blocks | blocks with one statement that fits a line (`if (x) { return; }`) on one line or on several (on one line also writes `csharp_preserve_single_line_statements = true`, else IDE0055 splits them) | [BRO1508](rules/BRO1508.md) | `dotnet_diagnostic.BRO1508.severity` |
| Accessor bodies | multi-line properties whose accessors all have block bodies: all one-line or all multi-line, or mixed | [BRO1510](rules/BRO1510.md) | `dotnet_diagnostic.BRO1510.severity` |
| Auto-property accessors | `{ get; set; }` on the declaration's line or on several lines | [BRO1527](rules/BRO1527.md) | `dotnet_diagnostic.BRO1527.severity` |
| `#region`s inside members | the `#region`s inside members (3 turn the rule off) | [BRO1113](rules/BRO1113.md) | `dotnet_diagnostic.BRO1113.severity` |
| Column alignment | your `resharper_*int_align_*` keys set to `true` | SDK formatter (IDE0055) | `dotnet_diagnostic.IDE0055.severity` |
| Charset | files with or without a BOM, when the `charset` that applies to them doesn't match them | SDK formatter | `charset` |
| Final line breaks | files with or without a last line break, when the `insert_final_newline` that applies to them doesn't match them | SDK formatter | `insert_final_newline` |
| Line endings | files with LF or CRLF line endings (not files git converts on checkout) | SDK formatter | `end_of_line` |
| Using order | files with 2 or more usings (each run between `#if`/`#endif` lines on its own): sorted with System first, sorted without, or not sorted | `dotnet format` | `dotnet_sort_system_directives_first` |
| Spacing | `if (x)` or `if(x)`; spaces inside parentheses, after casts, inside call and parameter lists (mixed: IDE0055 off) | SDK formatter (IDE0055) | `csharp_space_after_keywords_in_control_flow_statements`, `csharp_space_between_parentheses`, `csharp_space_after_cast`, `csharp_space_between_method_call_parameter_list_parentheses`, `csharp_space_between_method_declaration_parameter_list_parentheses` |
| Statements after blocks | statements right after a block's `}` with or without a blank line above (mixed or mostly without: BRO1519 is off) | [BRO1519](rules/BRO1519.md) | `dotnet_diagnostic.BRO1519.severity` |
| Nested `if`s | `if`s nested alone in another `if` that BRO1149 would merge (3 turn the rule off) | [BRO1149](rules/BRO1149.md) | `dotnet_diagnostic.BRO1149.severity` |
| Namespace braces | files whose block namespace has its `{` where `csharp_new_line_before_open_brace` doesn't put it (`types` covers namespaces): when most do, IDE0055 is off until you decide (the report says how many files would change) | SDK formatter (IDE0055) | `dotnet_diagnostic.IDE0055.severity` |
| Member separation | neighbouring members that need a blank line, with or without one (without: [BRO1519](rules/BRO1519.md) goes off too, else it adds the blank line after a member's `}`) | [BRO1505](rules/BRO1505.md) | `dotnet_diagnostic.BRO1505.severity` |
| Blank line below a type's `{` | the `{` of namespaces and types right above the code inside or with a blank line below | [BRO1503](rules/BRO1503.md) | `dotnet_diagnostic.BRO1503.severity` |
| Blank line above doc comments | doc comments right below code or after a blank line | [BRO1513](rules/BRO1513.md) | `dotnet_diagnostic.BRO1513.severity` |
| Blank line below comments | `//` comments right above code or with a blank line below | [BRO1506](rules/BRO1506.md) | `dotnet_diagnostic.BRO1506.severity` |
| Blank line above comments | `//` comments right below code or after a blank line | [BRO1504](rules/BRO1504.md) | `dotnet_diagnostic.BRO1504.severity` |
| Doc comment periods | summary, param, returns, ... texts ending with a period or not | [BRO1603](rules/BRO1603.md) | `dotnet_diagnostic.BRO1603.severity` |
| Doc element order | doc comments with `<typeparam>`/`<param>`/`<returns>` and later elements (`<exception>`, `<remarks>`, ...): in BRO1619's order or another; with fewer than 3 of those, the comments with only `<typeparam>`/`<param>`/`<returns>` decide | [BRO1619](rules/BRO1619.md) | `dotnet_diagnostic.BRO1619.severity` |
| Keywords in doc comments | `<see langword="null"/>` or `<c>null</c>` | [BRO1618](rules/BRO1618.md) | `dotnet_diagnostic.BRO1618.severity` |
| Non-private field names | public, internal and protected fields an enforced naming rule (`dotnet_naming_rule.*`) covers: named as it says or not (a rule your code doesn't follow turns BRO1306 off); without such a rule, public and internal fields that aren't constants or static readonly: PascalCase or not (camelCase code turns BRO1306 off) | [BRO1306](rules/BRO1306.md) | `dotnet_diagnostic.BRO1306.severity` |
| Blank line below doc comments | doc comments right above their element or with a blank line between | [BRO1511](rules/BRO1511.md) | `dotnet_diagnostic.BRO1511.severity` |
| Primary-constructor parameters | named `logger` or `_logger` (classes and structs) | [BRO1302](rules/BRO1302.md) | `dotnet_diagnostic.BRO1302.severity` |
| Tuple element names | `(int Count, ...)` or `(int count, ...)` | [BRO1311](rules/BRO1311.md) | `stylebro_tuple_element_name_casing` |

The keys are described in [Settings](configuration.md). `init` prints each verdict (`kept`, `default`, `off`, `both`,
`mixed`, `too few`, or `set` when your configuration decides) with the value that applies, then a summary that names the keys
(RealWorld):

```
Conventions in the code (a convention needs 3 places or more: below 10 they must all agree, from 10 on 75% of them).
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
  Kept your style for 5 settings: wrote stylebro_trailing_comma; 4 were already StyleBro's default.
  Turned 2 rules off because your code mixes both forms: BRO1133 (null checks), BRO1107 (split lists).
  To choose later: set the key in .editorconfig, remove its init line, run 'bro format'.
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

## Changing a detected setting later

Set the key in your own part of `.editorconfig`, outside the block (for example `stylebro_null_check_style =
equality_operator`), remove the rule's `severity = none` line from the block, and run `bro format` to bring
the code in line. The block is rewritten each time `init` runs, but a key you set yourself wins over what it counts: it
then writes neither the setting nor the `severity = none` line.
