# Decisions

Design questions that came up while building StyleBro, the choices considered, and what was decided. Newest first.

## Which conventions `stylebro-migrate init` detects (2026-10-09)

### Question

A trial of 0.3.0-alpha.1 on Ocelot, eShop, AutoMapper and vs-threading: `init` only looked at private field names, and
skipped even that whenever any `dotnet_naming_rule.*` key existed (Ocelot's and eShop's are for interfaces and
constants), so Ocelot's 1,058 `_x` fields were renamed in 293 files. StyleBro's other defaults overrode conventions just
as clear: Ocelot writes `=>` at the start of a wrapped line, and [BRO1521](rules/BRO1521.md)'s default (end of line)
moved it in 115 files.

```csharp
// Ocelot before init
private static string FormatFormCollection(IFormCollection reqForm)
    => new StringBuilder()

// After init until now (BRO1521, stylebro_arrow_placement_when_wrapping = end_of_line)
private static string FormatFormCollection(IFormCollection reqForm) =>
    new StringBuilder()

// After: init wrote stylebro_arrow_placement_when_wrapping = beginning_of_line, nothing moved
private static string FormatFormCollection(IFormCollection reqForm)
    => new StringBuilder()
```

### Choices

1. Detect the main ones: count each setting's two forms in the code and write the majority when it is clear.
2. Only fix the field detection; document the rest.
3. Ask for each setting interactively.

### Decision

The owner: "Detect the main ones" (choice 1). Details chosen while building it: a form is written when it has at least
75 % of at least 10 places (the field threshold of the earlier decision, now with a minimum: a convention seen fewer
than 10 times says little) and isn't StyleBro's default; every count is printed, also when nothing is written. Counted
with Roslyn's syntax trees over the repository's own C# (generated code, EF Core migrations, vendored folders and
submodules don't count): private field naming, brace placement and else/catch/finally, braces on one-line bodies,
operator/`=>`/`=` placement when wrapping, trailing commas, `""` or `string.Empty`, null checks, one-line summaries,
`<inheritdoc/>` spacing, `default` literal, the closing parenthesis and first item of split lists, constructor
initializer and `where` placement, `new T()` parentheses with an initializer, blank lines between switch sections.
Keys the root `.editorconfig` sets win, then a Sonar setup's; the field style is skipped only when a naming rule
covers private instance fields (the rule BRO1303 itself follows). The same trial also made `init` and `--write` mark EF
Core migration folders `generated_code = true` (eShop: 7 migration files reformatted), and the migration write
`csharp_preserve_single_line_statements = true` (vs-threading: `delegate { /* ... */ }` split), listing SA1107 as not
expressible.
## SonarQube setups (2026-10-09)

### Question

Many teams run SonarAnalyzer.CSharp (in the build or through SonarQube's scanner). A survey of the 12 reference repos
with Sonar's default "Sonar way" profile (SonarAnalyzer.CSharp 10.35, 8,170 diagnostics on 176 rules) showed which of
its findings StyleBro or the SDK already fixes and which have no fix anywhere. What should StyleBro do for such teams?

### Choices

1. `stylebro-migrate` reads a Sonar setup (rule severities in .editorconfig, rulesets, globalconfigs) and turns on the
   StyleBro and SDK rules that fix what the enabled Sonar rules report.
2. A StyleBro rule for S1066 (mergeable `if` statements: 287 findings in 11 repos, no fix in Sonar).
3. StyleBro rules for S2971 (`Where(p).Count()` -> `Count(p)`, 61 findings) and S3878 (an array created for a
   `params` parameter, 91 findings), neither with a Sonar fix.
4. Code fixes registered for Sonar's own ids (a probe showed `dotnet format` applies a third-party fixer to a Sonar
   diagnostic), so `dotnet format` fixes what Sonar reports where Sonar has no fix itself.

### Decision

All four. The rules are BRO1149 (S1066), BRO1150 (S2971) and BRO1151 (S3878): on in the preset, off after
`stylebro-migrate` (no StyleCop counterpart), written from Sonar's public rule descriptions and StyleBro's own
reasoning (SonarAnalyzer.CSharp is under the Sonar Source-Available License; its source isn't used). Their fix logic
works on the reported syntax alone (`NestedIfs.GetChange`, `WhereCalls.GetChanges`, `ParamsArrays.GetChanges`), so a
later PR can register the same fixes for S1066, S2971 and S3878 (never for an id Sonar fixes itself: `dotnet format`
would pick either fixer). The migration reading Sonar setups comes in its own PR.

## Rule docs as a website (2026-10-09)

### Question

The docs are Markdown files read on GitHub, and every diagnostic's help link opens a GitHub file view. Publish them as
a website? With what, which pages, and where do the help links go?

### Choices

1. Generator: MkDocs Material (Markdown as it is, search, dark mode); GitHub's built-in Jekyll (no build setup,
   plain look, no real search); DocFX (.NET's own, aimed at API docs we don't need).
2. Pages: user docs only (rules, getting started, migrating, configuration, baseline, CI, StyleCop comparison); or
   everything in `docs/`, the decision log, backlog and proposals included.
3. Help links: to the site's rule pages; or keep the GitHub file views.

### Decision

MkDocs Material; user docs only (`decisions.md`, `backlog.md`, `beyond-stylecop.md` and `proposals/` stay on GitHub;
site pages link to them with absolute GitHub URLs); help links point to the site
(`https://bisforboman.github.io/stylebro/rules/BROxxxx/`). Built with `--strict` on every pull request, deployed to
GitHub Pages from `main`.

## Parentheses in patterns, and fading unnecessary parentheses (2026-10-09)

### Question

StyleCop's SA1119 (BRO1405) only looks at parenthesized expressions, so `x is (> 0)` or `o is (string s)` were never
reported. And StyleCop also reports a hidden SA1119_p on the `(` and `)` of each finding so the IDE fades them, which
StyleBro didn't. Should StyleBro check patterns, and should it fade the parentheses?

```csharp
// Before
public bool IsSmall(int x) => x is ((1 or 2)) or (> 5 and < 9);

// After
public bool IsSmall(int x) => x is 1 or 2 or (> 5 and < 9);
## A preview before writing: which commands, and what it shows (2026-10-09)

### Question

Teams want to judge a migration or `init` before it touches their code. The backlog idea was a `--diff` that copies
the repository to a temporary folder, runs there and reports. Which commands get it, and what does it print?

```
Settings: 34 lines in .editorconfig (block), StyleBroPreset=none
First format run: 128 files, 412 changes, clean after 1 run
  BRO1001  member order        61 files
  BRO1514  braces              40 files
  IDE0055  whitespace          22 files
Sample (BRO1514, src/Lib/Parser.cs):
  -    if (x) return;
  +    if (x)
  +    {
  +        return;
  +    }
Full diff: stylebro-preview.patch (1,906 lines)
```

### Choices

1. Commands: only the migration (`stylebro-migrate <path> --diff`); the migration and `init`; all three (`format
   --diff` too).
2. Output: a console summary only; the full diff on the console; a console summary plus the full diff in a .patch file.

### Decision

The owner: all three commands, and a console summary (the example above) plus the full diff in a .patch file. Built
as `--diff[=file]` (default `stylebro-preview.patch` in the current folder) and `--keep`. The copy holds git's tracked
and untracked, not ignored files (submodules' working trees as repositories of their own, so format still skips them),
gets the StyleBro.Analyzers reference when the repository has none, and format runs until a run changes nothing (at
most 3). Rules come from a `--verify-no-changes --report` run before the fixes. On FFMpegCore the preview's patch for
`init --diff` was byte for byte what `init --write` plus two `stylebro-migrate format` runs wrote.

## What `stylebro-migrate format` fixes (2026-10-09)

### Question

A trial of 0.2.0-alpha.1 on Kavita ran plain `dotnet format` as the docs said. Besides StyleBro's fixes it applied
every other analyzer's and the compiler's: CS8618's fix added `required`, a Sonar fix (S1144) removed `init;`
accessors, and the repository no longer built (9 errors). Should StyleBro's tooling limit what gets fixed?

```
# Before: the docs said 'dotnet format'; 'stylebro-migrate format' passed everything through
dotnet format                          # StyleBro + SDK + Sonar + compiler fixes

# After
stylebro-migrate format                # dotnet format --diagnostics BRO1001 ... IDE0055 IDE0036 ... (+ whitespace)
stylebro-migrate format --all          # everything, like before
```

### Choices

1. Patterns: extend BRO1405 (but then `stylebro-migrate`'s SA1119 -> BRO1405 mapping would turn on more than StyleCop
   checked), a new rule beyond StyleCop next to it, or leave patterns alone.
2. Fading: a hidden, not configurable companion like StyleCop's SA1119_p, or none (the warning alone).

### Decision

The owner: a new rule, [BRO1410](rules/BRO1410.md), on in the preset and off after `stylebro-migrate` (like every rule
beyond StyleCop). It only removes parentheses whose removal parses to the same pattern, and leaves the ones
[BRO1407](rules/BRO1407.md) (SA1408) wants between `and` and `or` while BRO1407 is on, so the two converge in one run in
any order. And fading like StyleCop: `BRO1405_p` and `BRO1410_p` (hidden, `Unnecessary` and `NotConfigurable` tags)
on the `(` and `)` of each finding, reported only where their rule is on, no fix of their own, and no baseline,
migration or rule-page entry of their own.

A survey of the 12 reference repositories found 32 parenthesized patterns and no finding: 24 put an `and` inside an
`or`, 8 follow `not`.
1. Limit `stylebro-migrate format` to StyleBro's ids and the SDK rules `init`/the migration turn on, `--all` for
   everything; the docs warn about plain `dotnet format` and show the `--diagnostics` equivalent.
2. Only warn in the docs; keep passing everything through.
3. Leave it to the user.

### Decision

The owner: "Limit format, warn in docs" (choice 1). Whitespace formatting still runs (it isn't filtered by
`--diagnostics`), and the docs now recommend `stylebro-migrate format` for every repository, not only multi-targeted
ones. In the same change `format` excludes git submodules (`--exclude`), since a project can compile files from one.

## What `stylebro-migrate init` does in an existing codebase (2026-10-09)

### Question

`init` applied the preset as is. On Kavita (309 of 309 private fields named `_x`) BRO1303 renamed 337 fields to
`x`; on Spectre.Console, whose `.editorconfig` turns SA1309, SA1201 and SA1202 off, it applied the full BRO1001 sort
and BRO1306. Should `init` look at the repository first?

```csharp
// Kavita before init
private readonly ILogger<SeriesService> _logger;

// After init until now (BRO1303, camelCase)
private readonly ILogger<SeriesService> logger;

// After: init wrote stylebro_private_field_naming = _camelCase, nothing renamed
private readonly ILogger<SeriesService> _logger;
```

### Choices

1. Detect and adapt: keep a clear `_` majority, and stop in a repository with a StyleCop setup, pointing to
   `stylebro-migrate --write`.
2. Only document both cases.
3. Ask interactively.

### Decision

The owner: "Detect and adapt" (choice 1). The threshold is three quarters of the private fields (instance and
non-readonly static; generated code, EF Core migrations, vendored folders and submodules don't count), and the
repository's own `stylebro_private_field_naming` or `dotnet_naming_rule.*` settings win. StyleCop setup means
`stylecop.json`, StyleCop rule ids in `.editorconfig`/rulesets/global configs, or a StyleCop.Analyzers reference, also
in files the MSBuild files import; `init` prints what it found and exits with 1 without writing. After
`stylebro-migrate --write` (its block present) `init` runs as before, e.g. for `--modernize`.
## BRO1604: which summaries get 'Gets' in front (2026-10-09)

### Question

A trial on new repositories (Kavita) found BRO1604 putting the accessor words in front of summaries that aren't noun
phrases:

```csharp
// Before
/// <summary>This is the Koreader hash</summary>
public string Hash { get; set; }

/// <summary>Not used - For parity</summary>
public string Document { get; set; }

// After (until now)
/// <summary>Gets or sets this is the Koreader hash</summary>
/// <summary>Gets or sets not used - For parity</summary>
```

Until now only summaries starting with a condition or a verb from a list (`If`, `Returns`, `Is`, ...) were left alone.

### Choices

1. Only fix noun phrases: add the words only when the summary starts with an article (`The`, `A`, `An`), a lower-case
   word, or one of the known prefixes the rule already replaces; anything else isn't reported (there is no good fix).
2. Keep reporting everything StyleCop reports, and grow the skip lists as cases turn up.
3. Report everything, but fix only noun phrases (a warning without a fix).

### Decision

The owner: "Only fix noun phrases" (choice 1). A summary that starts with any other capitalized word gets no
diagnostic, since StyleBro reports only what it can fix:

```csharp
/// <summary>The order number.</summary>        -> Gets or sets the order number.
/// <summary>number of pages.</summary>          -> Gets number of pages.
/// <summary>Gets the total.</summary>           -> Gets or sets the total. (a known prefix, replaced)
/// <summary>This is the Koreader hash</summary> -> not reported
/// <summary>Device id of the user</summary>     -> not reported
```

Lower-case conditions and verbs (`if true, ...`, `returns ...`) stay skipped. StyleCop reports all of these.

## More options where preferences differ (2026-10-07)

### Question

Should rules offer more settings where teams reasonably prefer something else than StyleCop's behavior, and where
should they live: `.editorconfig` keys (as today), a `stylebro.json` like StyleCop's `stylecop.json`, or both?

### Choices

1. `.editorconfig` only: one place with the SDK's settings, per-folder sections, read natively by `dotnet format` and
   the IDEs; `stylebro-migrate` keeps translating `stylecop.json`.
2. `stylebro.json`: familiar to StyleCop users, but one file per project and a second place to look.
3. Both, `.editorconfig` winning.

Then a survey of every rule against the 12 public reference repositories listed candidates with usage counts.

### Decision

The owner: `.editorconfig` only, and all ten candidates plus the two higher-risk ones. Defaults stay StyleCop's
behavior; the preset doesn't set the new keys.

| Rule | Setting | Alternative |
|------|---------|-------------|
| BRO1104 | `csharp_prefer_simple_default_expression` (SDK key) | `default` instead of `default(T)` |
| BRO1106 | `stylebro_empty_string_style = string_empty \| literal` | `""` instead of `string.Empty` |
| BRO1135 | `stylebro_upper_case_literal_suffixes = all \| l_only` | only `l` must be upper case |
| BRO1401 | `stylebro_trailing_comma = include \| omit` | remove trailing commas |
| BRO1508/BRO1509 | `stylebro_allow_empty_single_line_blocks` | `{ }` may stay on one line |
| BRO1110 | `stylebro_closing_parenthesis_placement = last_item \| own_line` | `)` on its own line |
| BRO1107 | `stylebro_split_list_first_item = next_line \| same_line` | first item stays on the `(` line |
| BRO1505 | `stylebro_allow_adjacent_single_line_members` | single-line members without a blank line |
| BRO1601 | `stylebro_inheritdoc_style = compact \| spaced` | `<inheritdoc />` |
| BRO1105 | `stylebro_constructor_initializer_placement = own_line \| same_line` | `: base(x)` on the declaration line |
| BRO1111 | `stylebro_constraint_placement = own_line \| same_line` | `where` on the declaration line |

Considered and left out (nobody writes it the other way, or turning the rule off covers it): BRO1103, BRO1123,
BRO1136, BRO1102, BRO1146, one-line blocks with statements, and wording rules (BRO1603, BRO1606/BRO1607). The keys'
exact names and values are on the rule pages.

## BRO1134: a comment ending the header's line (2026-10-06)

### Question

A first run on new repositories found BRO1134 moving notes that belong to the declaration's name or signature:

```csharp
// Before
public int GetHashCollissions() // legacy incorrect spelling, oops
{
    return collissions;
}

// After (until now)
public int GetHashCollissions()
{
    // legacy incorrect spelling, oops
    return collissions;
}
```

Keep moving such comments into the body?

### Choices

1. **Skip a trailing comment**: a comment that ends the header's last line, with `{` on the next line, stays where it
   is. A comment on a line of its own between the header and `{` still moves, and so does one before a `{` on the
   header's line (`void M() /* note */ {`).
2. Keep moving every comment (the rule as built, like SA1108 for statements).

### Decision

The owner: **skip a trailing comment** (choice 1). BRO1132 (statements, StyleCop's SA1108) is unchanged.

## BRO1601 without documentation generation (2026-10-06)

### Question

A first run on new repositories (CleanArchitecture: 49 files) added `/// <inheritdoc/>` to projects that don't generate
documentation at all, where nothing reads it. StyleCop's documentation rules stay quiet there (SA0001 says the
documentation isn't parsed). Report BRO1601 in such projects?

```csharp
// Before, in a project without <GenerateDocumentationFile>true</GenerateDocumentationFile>
public override string ToString() => Name;

// After (until now); with choice 1 the code stays as it was
/// <inheritdoc/>
public override string ToString() => Name;
```

### Choices

1. **Skip without doc generation**: no BRO1601 unless the project generates documentation, like StyleCop.
2. Report everywhere (the rule as built; it's text-based and works without parsed documentation).

### Decision

The owner: **skip without doc generation** (choice 1). The signal has to be the same in a build and under
`dotnet format`, which parses documentation comments even where the build doesn't: the package's build targets make
`GenerateDocumentationFile` compiler-visible (the SDK always sets it, `true` also when only `DocumentationFile` is set).
Without the package's targets (the analyzer referenced directly), the compiler's documentation mode decides.
## README: where the rule list goes (2026-10-06)

### Question

The README's rule table had grown to 124 rows, too long for a start page. Where should the list go, and should the
contributor sections stay?

### Choices

1. The full table on its own page (`docs/rules/README.md`, grouped by area), a six-row summary in the README.
2. Keep the table in the README, each area collapsed.
3. Only a link from the README.

And: move Repository layout / Developing / Roadmap to `CONTRIBUTING.md`, or keep them.

### Decision

The owner: choice 1, and move the contributor sections to `CONTRIBUTING.md`. The index adds a Default column (on,
off, off in the preset); `DocExamplesTests.TheRuleIndex_ListsEveryRulePage` keeps it complete, and New-Backlog.py
reads the titles from it.

## Migrate skips submodules (2026-10-06)

### Question

A first run on Nerdbank.Streams: `stylebro-migrate --write` wrote a settings block into `ext/MessagePack/.editorconfig`
and added suppressions to 68 files there, all inside a git submodule (another repository's code). And the files a
project links from that submodule got StyleBro and SDK warnings (IDE0073, IDE0065, BRO1514, BRO1306) that
`stylebro-migrate format` never fixes, because it formats each project's own folder only. What should the tools do
with nested repositories, and how do users exclude vendored code?

### Choices

1. Skip every folder that is its own git repository (a `.git` file or folder below the root) in migrate, `init`,
   `format` and `baseline`; document `generated_code = true` for vendored folders and linked files.
2. Keep walking submodules and leave it to the user to revert those changes.
3. A `--exclude` option for migrate.

### Decision

The owner: choice 1, "docs + migrate skips submodules". Before, `--write` in a repository with a submodule at
`ext/MessagePack`:

```
 M .editorconfig
 M ext/MessagePack/.editorconfig                     <- another repository's file
 M ext/MessagePack/benchmark/.../Answer.cs           <- and 67 more
```

After: only the repository's own files change. Agent's choices: all four commands walk files through one function
(`StyleCopSetup.EnumerateFiles`), so the skip is there; `format` and `baseline` don't walk files themselves. Found on the
way: the linked files' warnings came from the root block turning rules on by id, which beats the
`dotnet_analyzer_diagnostic.severity = none` that `ext/.editorconfig` set for vendored code. Migrate now reads that bulk
key (it didn't before) and writes every replacing rule as `none` into such a folder, IDE0073 included. Verified
with dotnet format and the build that `generated_code = true` makes StyleBro, the SDK rules and the whitespace pass
skip a folder; getting-started.md documents it.

## Migrate pins StyleCop casing (2026-10-06)

### Question

Nerdbank.Streams has a `dotnet_naming_rule` asking for camel case on static fields (at `suggestion`) and IDE1006 off
("StyleCop handles these for us"). After `stylebro-migrate --write`, BRO1306 followed that naming rule and renamed 178
fields StyleCop was happy with. Should migrate follow the repository's naming rules or StyleCop's casing?

### Choices

1. Pin StyleCop's casing: `--write` writes explicit `stylebro_*` keys, which win over `dotnet_naming_rule`; a new
   project without migrate keeps following its naming rules.
2. Follow the naming rules (what happened), and document how to opt out.
3. Follow a naming rule only when IDE1006 is on.

### Decision

The owner: choice 1. Before:

```csharp
private static readonly Version ProtocolVersion = new(1, 0);   // StyleCop-clean
private static readonly Version protocolVersion = new(1, 0);   // after migrate + dotnet format
```

After: unchanged. Agent's choices: private instance fields already had a key (`stylebro_private_field_naming`, written
by migrate); private constants and `static readonly` fields had none, so `stylebro_private_static_field_naming`
(`PascalCase`, `camelCase`, `_camelCase`, for both) was added, documented on [BRO1306](rules/BRO1306.md); migrate
writes `PascalCase`. Nerdbank.Streams after the fix: 0 renames, 0 changes from `stylebro-migrate format`.

## BRO1302 and parameter names that reach run time (2026-10-05)

### Question

BRO1313 now keeps a parameter whose name reaches run time (`nameof(x)`, or `x` passed to a `[CallerArgumentExpression]`
parameter such as `ThrowIfNull(x)`): its rename changed an exception's `ParamName` and broke a Newtonsoft.Json test.
BRO1302 (parameters start lower-case, on by default) changes `ParamName` the same way. Should it get the same guard?

```csharp
public void Load(string Path)
{
    ArgumentNullException.ThrowIfNull(Path);   // after BRO1302: ThrowIfNull(path), ParamName "path"
}
```

### Choices

1. Skip those parameters, like BRO1313 (they keep their wrong-cased name).
2. Report, but don't fix (like the string guard).
3. Keep renaming, like StyleCop's SA1313; document it.

### Decision

The owner: keep renaming (choice 3). BRO1302's rename fixes the casing the team asked for; the changed `ParamName` is
the new name of the parameter. BRO1313 differs because its rename only aligns a name with the base. Documented on
[BRO1302.md](rules/BRO1302.md).

## IDE0370 in `init --modernize` (2026-10-05)

### Question

The SDK 10's IDE0370 (redundant `!`) does work in builds and `dotnet format` when its severity is set explicitly.
Should `init --modernize` turn it on next to BRO1147?

### Choices

1. No, BRO1147 only.
2. Yes, in tier B.

### Decision

The owner: no, keep BRO1147 only (choice 1). IDE0370 decides all-or-nothing per member, and in a multi-targeted probe
its fix left the `!` while the build still reported it; BRO1147 skips multi-targeted projects. The comparison is on
[BRO1147.md](rules/BRO1147.md).

## Renames to C# 14 contextual keywords (2026-10-05)

### Question

In C# 14 `field` inside a property accessor is the backing field: BRO1303 renaming `_field` to `field` made
`get => field;` read other storage (CS9258), BRO1301 renaming a local `Field` in a getter didn't compile (CS9273).
Which new names should the rename rules refuse, and should they write `@field` instead?

### Choices

1. **Never rename to `field`**, in any rule and any language version (StyleBro's Roslyn 4.8 can't tell C# 14), and
   never to `value` for a local or parameter inside a property, indexer or event (the implicit setter parameter,
   CS0136, an older bug found on the way). The name is simply not reported.
2. Rename and write `@field`/`this.field` where an accessor uses it: correct, but a name that reads as the keyword.
3. Refuse only when the member has a property accessor in scope: more precise, more code, same result in practice.

### Answer

**Choice 1** (the agent's call while fixing the bug; design rule 3, skip rather than risk). `extension`, `scoped` and
`partial` need no guard: only the camelCase rules can produce a lower-case name, and those names are only keywords
where a type or modifier stands.
## Redundant `!` and `HasValue` (newer-rules survey #5, #6) (2026-10-05)

### Question

The survey of newer analyzer rules found two candidates: a redundant null-forgiving `!` (Sonar S8969/S8970) and
`x.HasValue` -> `x is not null` (Meziantou MA0171). Build them, and on by default?

### Choices

1. Both, off by default (also in the preset), off after `stylebro-migrate`.
2. Both on in the preset.
3. Leave the `!` to the SDK's IDE0370.

### Decision

The owner: both, off by default (choice 1). Agent's choices: BRO1147 decides per `!` from the operand's flow state
without the `!` (a speculative copy of the enclosing statement; the operand's own type info reports the state after the
`!`), skips `null!`/`default!`, types with type arguments, arrays, tuples and type parameters, ref/out arguments,
`#nullable` changing the project's setting, and projects with several target frameworks (`StyleBroTargetFrameworks`,
like the modernize guard). Found while building it: IDE0370 does work in SDK 10 builds and `dotnet format` when its
severity is set explicitly (the survey's probe hadn't), all-or-nothing per member; the rule page compares the two.
BRO1148 writes BRO1133's preferred form (`stylebro_null_check_style`) so the two converge in one run, skips the places
BRO1133 skips, and judges `(x.HasValue)` without its parentheses while BRO1405 is on (else the result depended on the
fix order).
## C# 14: keywords and extension blocks (2026-10-05)

### Question

A survey of StyleBro on C# 14 code found (1) identifiers C# 14 reads as keywords (`field` in a property accessor,
`extension`, `partial`) silently change meaning or break the build, and nothing fixes them; (2) BRO1001 skipped every
type containing an extension block (`extension(string s) { ... }`, a member kind Roslyn 4.8 doesn't know), so C# 14
`*Extensions` classes got no ordering at all; (3) BRO1509 didn't expand a one-line extension block. Add a rule for (1),
and where do extension blocks sort in (2)?

### Choices

1. Escaping rule: on in the preset (off after `stylebro-migrate`, as for every rule StyleCop doesn't have), or off.
2. Extension blocks in BRO1001: with the methods, first (right before them); last (after every other kind); keep skipping
   such types.

### Decision

The owner: BRO1144, on in the preset and off after migrate, kept narrow to the cases where C# 14 changes the meaning or
breaks the build (Roslyn's breaking-changes list, each checked with the C# 14 compiler). Extension blocks are their own
kind `extension`, right before methods ("with methods, first"), and their members are sorted by the normal rules; BRO1509
expands a one-line block like a type. Agent's choices: `@` rather than renaming (it means the same in every C# version,
so multi-targeted projects stay consistent); in C# 14 code a `field` keyword is escaped only when a member, local or
parameter named `field` of the property's type is in scope (otherwise `@field` wouldn't compile, and the keyword is
clearly meant); `scoped` as a lambda parameter's type (Sonar S8381) is left out; extension blocks are recognized by
their runtime type, since StyleBro compiles against Roslyn 4.8.
## Declaration forms: auto-accessors, empty type bodies, `record class` (2026-10-05)

### Question

Three candidates from the survey of newer rules (Roslynator RCS0042, Meziantou MA0206 beyond records, MA0174/MA0175):
build them, and with which defaults? The empty type body (`class Marker;`) needs C# 12, which a multi-targeted project
without `LangVersion` doesn't have in its older frameworks.

### Choices

1. BRO1527 auto-accessors on one line: on in the preset, or off.
2. BRO1145 empty class/struct/interface body: on with a multi-target guard, or off by default.
3. BRO1146 `record class` -> `record` (or the opposite style as an option): on, or off.

### Decision

The owner: BRO1527 on in the preset (off after `stylebro-migrate`), only lists of auto-accessors without bodies,
attributes or comments; it must converge with BRO1505/BRO1510/BRO1509 and not fight IDE0360. BRO1145 off by default
(`isEnabledByDefault: false`, preset `none`), skipped below C# 12 and, through the package's
`StyleBroTargetFrameworks`, in a project with several frameworks unless every one defaults to C# 12 or the project sets
`LangVersion`. BRO1146 on in the preset (off after `stylebro-migrate`), `record` only, no option for the opposite
style. Agent's choices: the package also passes `LangVersion` and `MaxSupportedLangVersion` (equal when the project
didn't set `LangVersion`); BRO1527 adds the blank lines BRO1505 (or BRO1519 when BRO1505 is off) wants next to the
multi-line property, so every fix order gives the same text, and is skipped when
`csharp_preserve_single_line_blocks = false` (the SDK formatter would expand the list again).

## Multi-target guard for the newer-API rules (2026-10-05)

### Question

`init --modernize` wrote tier C (the SDK rules whose fixes need a newer API) as `suggestion` in any repository with a
multi-targeted project, because the rules report in the newer framework's compilation and `dotnet format` writes the
fix into the file the older framework compiles too. Build the guard suppressor the 2026-10-04 decision left on the
backlog?

### Choices

1. Build it: a `DiagnosticSuppressor` that knows all of a project's target frameworks and hides a tier C diagnostic
   when one of them lacks the API; tier C becomes `warning` everywhere.
2. Keep the `suggestion` fallback.

### Decision

The owner: build it ([modernizing.md](modernizing.md#the-multi-target-guard)). Agent's choices: the package passes
`StyleBroTargetFrameworks` (`TargetFrameworks` with `,` for `;`, since `;` starts a comment in the generated
editorconfig); a hand-kept table of minimum .NET (Core) / .NET Standard versions per rule, set to the newest API the fix
can write (CA1872 .NET 9 for `ToHexStringLower`, CA1850 .NET 6 for HMAC, CA2263 .NET 5); unknown frameworks count as
lacking everything; single-target projects are never touched. `init --modernize` writes tier C at `warning` always, no
`suggestion` fallback: the guard ships in the same package as the preset `init` sets up, and a note says projects
without the package have no guard. A MigrationTests check keeps the table and tier C in sync.
## Public members of internal types (StyleCop #2981) (2026-10-05)

### Question

StyleCop issue [#2981](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2981) asks for `internal` instead of
`public` on members of an internal type. It was rejected in [beyond-stylecop.md](beyond-stylecop.md) (#11): the rewrite
can change run-time behavior. The owner first questioned whether reflection cares about public vs private at all. It
does: reflection's defaults (`GetMethods()`, `GetProperties()`, `BindingFlags.Public`) and the libraries built on them
(System.Text.Json, Newtonsoft.Json, XmlSerializer, data binding, model binding, AutoMapper, Dapper, `Activator`,
dependency injection, test frameworks) see public members only.

### Choices

- Keep it rejected.
- A narrow variant, off by default: ordinary methods only, everything reflection, frameworks or the compiler look for
  left out.
- The full rule (every member kind).

### Decision

The narrow variant, off by default (owner): **BRO1409**, because reflection's and serializers' defaults see public
members only. Agent's choices: properties, fields, events, indexers, constructors, operators, finalizers and nested
types aren't reported; neither are overrides, `virtual`/`abstract`, interface implementations (also an inherited method a
derived class implements an interface with), methods of interfaces, attributed methods, methods of attributed types or of
types with a base type from another assembly (other than `object`/`ValueType`), convention names (`Main`, `Dispose`,
`GetEnumerator`, `GetAwaiter`, `Add`, `Deconstruct`, `Configure`, `Invoke`, record members, ...), `extern`/`partial`
methods, and `#if` in the method or a type header. Extension methods are reported (`internal` works for them). A name
that is a string literal or a `nameof` anywhere in the solution is reported but not fixed (like the naming rules'
reflection guard), and so is a method of a type another project (`InternalsVisibleTo`) derives from. Off after
`stylebro-migrate` (no StyleCop rule).

## Switch section blank lines, no `else` after a jump, overloads together (2026-10-05)

### Question

Three candidates from [beyond-stylecop.md](beyond-stylecop.md) (#6, #16, #17): Roslynator's RCS0061 (blank line between
switch sections), Meziantou's MA0071 / Roslynator's RCS1211 (no `else` after a branch that ends in a jump) and Sonar's
S4136 (overloads next to each other). Defaults, and how each agrees with the rules that touch the same code: BRO1519
(SA1513) wants a blank line after a case block's `}` before the next label (kept in "StyleCop's open bugs", #3568), and
BRO1001 sorts by StyleCop's access order.

### Choices

- RCS0061: `include` / `omit` / `omit_after_block`, on or off in the preset; what to do where BRO1519 wants the opposite.
- MA0071: on or off; `else if` chains (whole chain, or skip); how much to recognize as "ends in a jump".
- S4136: a new rule or a BRO1001 option; which members count as overloads.

### Decision

- **BRO1526** (owner): option `stylebro_blank_line_between_switch_sections = include | omit | omit_after_block`, default
  `include`, on in the preset, off after `stylebro-migrate`. Agent's resolution of the conflict: after a section whose
  `}` BRO1519 judges (a multi-line block or if/else at the end), BRO1526 reports nothing when BRO1519 is on, so the
  two never undo each other; with `omit`, a blank line stays there unless BRO1519 is turned off (documented). A
  one-line block isn't BRO1519's and follows the setting. Comments and directives between sections are skipped; switch
  expression arms aren't sections.
- **BRO1143** (owner): off by default (`isEnabledByDefault: false`, preset `none`), it's a matter of taste. Agent's
  choices: `else if` chains skipped entirely (removing the first `else` would only make a new candidate); "ends in a
  jump" is syntactic (a jump statement, or a block whose last statement is one); skipped when a name declared in the
  body is used in the enclosing block, for `using` declarations, comments around the `else`, `#if`, and lines that
  can't simply be moved out (multi-line strings and comments, unusual indentation). An `if` branch without braces next
  to an `else` block is BRO1516's first (the chain is inconsistent; without the `else` it would become BRO1514's, which
  may already have run), so the `else` goes on the next run.
- **BRO1001 option** (owner): `stylebro_keep_overloads_together = false | true`, default `false` (StyleCop's order).
  Agent's choices: methods only (constructors and indexers are kinds of their own, so always together; operators
  aren't grouped by symbol, so `==`/`!=` pairs stay); a group sorts at the place of its overload that sorts first, the
  other overloads right behind it in their own order; within a region; explicit interface implementations group among
  themselves.
## Readability batch B: eight rules beyond StyleCop (2026-10-05)

### Question

Which of these candidates (StyleCop issues #762 and #760, and survey #7, #8, #10, #12, #13 and #14 in
[beyond-stylecop.md](beyond-stylecop.md)) should StyleBro build, and how should they be configured: `(x) => x`,
redundant `return;`/`yield break;`, unneeded `$`/`@`/raw strings, `else if`, empty record bodies, object creation
parentheses, one local per declaration, redundant base types?

### Choices

1. Build them all, on in the preset (`warning`), off after `stylebro-migrate` like the other rules beyond StyleCop.
2. Build them off by default (opt-in, like BRO1310).
## Naming batch: camelCase constants, base parameter names, Async suffix (2026-10-05)

### Question

Three naming candidates from [beyond-stylecop.md](beyond-stylecop.md): camelCase private constants and static readonly
fields (StyleCop #2641, #3793; tracker #10), a parameter named like the one it overrides or implements (StyleCop's
proposed SA1315, #1949, the SDK's CA1725 without a fix; tracker #12), and the `Async` suffix on asynchronous methods
(Roslynator RCS1046, Meziantou MA0137; survey #18). All three are renames, so the rename guards apply, and two of them
change names other code depends on (named arguments, public method names).

### Choices

1. camelCase constants: a new rule; an option for BRO1306 (a `stylebro_*` key, or following the SDK's
   `dotnet_naming_rule.*` like BRO1303 does); skip.
2. Base parameter names: on by default; off by default; skip (CA1725 reports it).
3. `Async` suffix: on by default; off by default; skip (renames public API, frameworks bind some names).

### Decision

The owner: BRO1306 follows the SDK's naming rules, no new id: a naming rule that singles out private constants
(`required_modifiers` with `const`) or private static readonly fields (`static`/`readonly`) and asks for camel case
(no prefix or `_`) makes BRO1306 rename them that way; without one, PascalCase like StyleCop. BRO1313 (base parameter
names) and BRO1314 (`Async` suffix) are built and off by default (`stylebro-migrate` turns them off too; StyleCop never
shipped either). BRO1313 matches bases in libraries too and skips discards, disagreeing bases, partial methods and
clashing names; BRO1314 counts `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>` and `IAsyncEnumerable<T>` return types
and skips `Main`, names containing `Async`, attributed methods (tests and frameworks), controllers, hubs and event
handlers. The reverse (no `Async` on synchronous methods) is left out.
## Documentation batch A: crefs, langword, element order, tool markers (2026-10-05)

### Question

Four candidates from [beyond-stylecop.md](beyond-stylecop.md) (StyleCop issues #758 and #3546/#1490, Meziantou
MA0154, survey #15): should they be built, on by default, and what does `stylebro-migrate` do with them?

### Choices

1. All on in the preset, off after `stylebro-migrate` (like the other rules StyleCop doesn't have).
2. Off by default (opt-in, like BRO1310/BRO1312).
3. Leave them in the backlog.

### Decision

The owner: **choice 1**, all eight, as BRO1136-BRO1142 and BRO1408. Object creation parentheses get
`stylebro_object_creation_parentheses = omit | include` with `omit` as the default; target-typed `new() { }` keeps its
parentheses. One rule for the three string forms (BRO1138). BRO1408 covers `enum E : int` and `class C : object` only.
The owner: build all four, on in the preset and off after `stylebro-migrate`. BRO1617 (`cref="List{T}"`), BRO1618
(`<see langword="null"/>` for a keyword alone in `<c>`; reserved keywords plus MA0154's contextual ones, kept
conservative), BRO1619 (top-level elements in the order summary, typeparam, param, returns, value, exception, remarks,
example, seealso; Roslynator's RCS1232 turned out to cover `<param>` order only, so the order was picked from Visual
Studio's `///` stub and Microsoft Learn's API pages, see the rule page). The BRO1504 option is
`stylebro_comment_blank_line_exempt_prefixes`, default empty, so BRO1504 behaves like SA1515 until a team sets it.

## Modernizing code for newer runtimes (2026-10-04)

### Question

Can StyleBro make code "nicer for newer runtimes" (target-typed `new`, collection expressions, `ThrowIfNull`, ranges,
`System.Threading.Lock`, ...)? A survey found a Fix All fixer in the SDK for every important rewrite, so no StyleBro
rule is needed. The SDK lacks one thing: in a multi-targeted project the rules fire per target framework and
`dotnet format` writes the newer framework's edit into the shared file. Probed: 14 of 15 API rules broke the net48
build (CA1847 silently bound `Contains(char)` to LINQ there), and 15 of 18 language rules broke it unless LangVersion
was set explicitly.

### Choices

1. `stylebro-migrate init --modernize`: an opt-in block of the SDK's rules in tiers, at `suggestion` where they aren't
   safe in the repository's multi-targeted projects.
2. A multi-target guard: a `DiagnosticSuppressor` that knows all of a project's target frameworks and hides the API
   rules an older one lacks, so every tier works in multi-targeted repositories.
3. Backlog only.

### Decision

The owner: `init --modernize` now ([modernizing.md](modernizing.md)); the guard suppressor not now (backlog, Maybe).
The tiers: older C# (A) at warning everywhere; newer C# (B) at warning unless a multi-targeted project leaves
LangVersion unset; newer APIs (C) at warning only without multi-targeted projects. Debated rules (IDE0290, IDE0066,
IDE0063, IDE0305) and IDE0251 are left to the user; rules without a `dotnet format` fix are skipped.

## StyleCop's open bugs that StyleBro shares (2026-10-04)

### Question

The sweep of StyleCop's open bug reports (results in
[differences-from-stylecop.md](differences-from-stylecop.md#stylecops-open-bugs)) found 8 that StyleBro shares. Five are
plain bugs and were fixed. Three are StyleCop's behavior that its users call a bug: should StyleBro keep matching
StyleCop there?

- [#3392](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3392) (BRO1504): a comment right after `=>` (a
  switch expression arm, also lambdas and expression bodies, #3550) gets a blank line between the arrow and the comment.
- [#2832](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2832) (BRO1501): a blank line grouping the
  `{ k, v }` entries of a dictionary initializer is removed.
- [#3568](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3568) (BRO1519): a blank line is wanted between a
  case block's `}` and the next `case` label.

### Choices

For each: change StyleBro (and say so under "Compared with StyleCop"), or keep StyleCop's behavior.

### Decision

The owner: #3392 changed, `=>` counts like `{` for BRO1504 (this reverses the earlier rejection of #3550 in
[beyond-stylecop.md](beyond-stylecop.md)). #2832 changed too, after seeing code examples: BRO1501 leaves the brace of
an initializer's entry alone, so blank lines grouping the entries stay (first answer was "keep"). #3568: keep
StyleCop's behavior (BRO1519 wants the blank line between a case block's `}` and the next label).
## Parity with StyleCop's master (2026-10-04)

### Question

StyleCop's unreleased `master` has ~60 commits since 1.2.0-beta.556 that change rules StyleBro replaces (a parity run
against a locally built `master` package confirmed them). Follow `master` everywhere, or only where it fixes a gap?

### Choices

For each change: follow `master`, or keep StyleBro's (beta.556-like) behavior and document the difference. The clear
gaps: BRO1104 `new nint()` (beta's `nint.Zero` doesn't compile before .NET 7), BRO1604/BRO1605 `init` accessors (and
the `Gets or sets or initializes` fix), BRO1611 primary constructor `<param>` tags, BRO1401 property patterns; also
BRO1606/BRO1607 missed multi-line whitespace-only summaries, which StyleCop reports. Changes that make `master` report
less: BRO1505 two single-line properties without a blank line (2aeb4e3d), BRO1601 explicit interface implementations (2959cac8), BRO1606 any summary starting
with `<para>` accepted without checking its text (29514d23); BRO1311 skipping overrides/implementations (d1b6183d) was
already a documented difference.

### Decision

The owner: the clear gaps follow `master`; StyleBro keeps its behavior for BRO1606 (`<para>` text checked), documented
on the rule pages and in [differences-from-stylecop.md](differences-from-stylecop.md). First also kept for BRO1505 and
BRO1601; after seeing code examples the owner changed both to follow `master` (same day): two single-line properties
may sit together, and explicit interface implementations get no `<inheritdoc/>`. The parity sets stay on beta.556;
`Compare-WithStyleCop.ps1 -StyleCopFeed` runs them against a local `master` build when needed (no CI job).

## Superseding StyleCop, and performance (2026-10-04)

### Question

StyleCop parity is done (every rule StyleBro, SDK or dropped with a reason). To properly supersede StyleCop, what else
goes on the backlog? And after the analyzer speed-ups (all 55 analyzers ~830 -> ~700 ms on Newtonsoft.Json), what
performance work?

### Choices

Backlog: (1) a sweep of StyleCop's open bug reports on the rules StyleBro replaces, each checked with a test; (2) the
remaining candidates from StyleCop's tracker (beyond-stylecop.md #6-#10, #12); (3) parity with StyleCop's unreleased
`master` (~20 commits that change what a replaced rule reports); (4) the remaining candidates from other analyzers
(survey #6-#10, #12-#18). Performance: a CI regression check, a FieldNamingAnalyzer deep dive (with a profiler), one
shared token pass for the layout analyzers, a per-file IDE responsiveness check.

### Decision

The owner: all four backlog items ("to properly supersede StyleCop we should have implemented most of their reported
issues/planned improvements too"), and the CI check, the FieldNamingAnalyzer deep dive and the shared token pass
("performance is an important metric for analyzers, you don't want them to be slow"). Not now: the per-file IDE check.
All are under Work in [backlog.md](backlog.md).

## Comments in declaration headers (StyleCop issue #605) (2026-10-04)

### Question

StyleCop issue #605 ([beyond-stylecop.md](beyond-stylecop.md#from-stylecops-issue-tracker) #4) proposes SA1108's check
for declarations: no comment between a class's, namespace's, member's or accessor's header and its `{`. Extend BRO1132
(same id) or a new rule, and which declarations?

Survey of the eight reference repositories (scripts/realworld/repos.psd1), 50,403 declarations with a `{` (7,989 types,
2,462 namespaces, 29,151 methods/constructors/operators, 9,864 properties/indexers/events, 734 accessors, 203 local
functions): 9 with a comment between the header and `{`. 7 single-line headers with a trailing comment (methods: CsvHelper
1, Jellyfin 1, Polly 1, Serilog 1, mostly a note or issue link after a test name; types: Newtonsoft.Json 3, e.g.
`class ErrorPerson2 //:IPerson - oops!`), and 2 multi-line headers (Jellyfin's comment after the last parameter, Serilog's
ReSharper markers around a constructor initializer), which the BRO1132 rule for multi-line headers (decision A' below)
skips. None on accessors, property accessor lists, namespaces or local functions.

### Choices

1. **A new id, BRO1134**, sharing BRO1132's code (same analyzer and fix, same skips): `stylebro-migrate` maps BRO1132 to
   SA1108 and turns it on for repositories with SA1108 on, which shouldn't start changing declarations StyleCop never
   checked; teams can turn the two on separately.
2. **Extend BRO1132:** one id, but SA1108 then means more in StyleBro than in StyleCop, and migrated repositories change.

Scope: types (incl. enums, records with a body), block-scoped namespaces, methods/constructors/finalizers/operators with a
block body, a property's, indexer's or event's accessor list, accessor bodies, local functions. Comments after an
attribute (`[Fact] // flaky`) aren't in the header. Found on the way, fixed for both rules: with blank lines right after
`{`, the moved comment went above them, and a run that had already applied BRO1506 ("no blank line below a comment") left
one behind; it now goes below them, where BRO1503 removes them (FixOrderTests).

Preset: warning; `stylebro-migrate` writes it as `none`.
## Call chain layout (BRO1523, Roslynator RCS0054) (2026-10-04)

### Question

[beyond-stylecop.md](beyond-stylecop.md) #4: when a call chain is split over several lines, every call starts its own
line. Which chains count, what may stay on the chain's first line, what is a "call", and which indentation do new lines
get?

Facts: Roslynator's [RCS0054](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS0054) (off by default;
[analyzer](https://github.com/dotnet/roslynator/blob/main/src/Formatting.Analyzers/CSharp/FixFormattingOfCallChainAnalyzer.cs),
[fix](https://github.com/dotnet/roslynator/blob/main/src/Formatting.Analyzers.CodeFixes/CSharp/CodeFixHelpers.cs))
starts at the outermost invocation, element access or conditional access and walks every `.` and `?.` back from the
end. Each must start a line indented one unit deeper than the chain's first line, until it reaches one on the chain's
first line; so the first line may hold anything, a property step is split too (`.WriteTo` + newline + `.Sink(x)`), and
a chain is reported as soon as any `.` is off its first line, also when only the arguments span lines
(`Task.Run(() => { ... }).ConfigureAwait(false)`). Its fix re-indents the lines inside a moved step (lambda bodies).

Survey of the eight reference repositories (scripts/realworld/repos.psd1, default branches, 2026-10-04), chains with two
or more `.`/`?.` and at least one call, outside interpolated strings: 42,705; split (a `.` or `?.` starts a line):
20,959 (Jellyfin 17,215, Polly 2,179, OpenTelemetry 922, FFMpegCore 248, FluentValidation 189, Serilog 182, CsvHelper 13,
Newtonsoft.Json 11).

- First line of the split chains: the receiver only (`people` + newline + `.Where(...)`) 2,652; the receiver and one
  call (`services.AddA()` + newline + `.AddB()`) 17,800; two or more calls 58 (mostly FluentValidation's
  `RuleFor(x => x.Name).NotNull()` + newline + `.WithMessage(...)`).
- Already one call per line below the first line (this rule as chosen): 20,880 (99.6%); 79 chains, 110 calls, 45 files
  would change (Jellyfin 49 calls, FluentValidation 22, CsvHelper 12, Polly 12, OpenTelemetry 10, Newtonsoft.Json 4,
  Serilog 1, FFMpegCore 0). Typical: `.Cast<T>().Single()`, `.GreaterThan(100).WithName("Foo")`, CsvHelper's
  `.Map(m => m.A).Name("A1").Default("WEW")`.
- RCS0054's view (every `.` off the first line starts a line): 711 split chains differ, mostly property steps (Serilog
  142 of 182: `.WriteTo.Sink(...)`, `.MinimumLevel.Debug()`); plus 404 chains that aren't split but have a `.` after
  multi-line arguments.
- A property after the last call (`.OutputToFile(...).Arguments`, `.Result`): 46 chains keep it on the call's line
  (FFMpegCore 38).
- Indentation of the lines that start with `.`, against the chain's first line (20,510 chains whose breaks all sit
  before a call step): one unit (4 columns) 19,887; aligned elsewhere, usually under the first `.` 516 (Polly 443,
  OpenTelemetry 46); 0 columns 55; 8 columns 27; 2 columns 12; mixed within the chain 13.
- No call that would move has a comment or a directive in its gap; 2 multi-line steps have lines less indented than
  their new line would be.

### Choices

1. **Which chains:** only split chains (a `.`/`?.` starts a line), like BRO1108 for lists (never split a one-line chain);
   or also chains where only the arguments span lines, like RCS0054 (404 more chains, e.g. every
   `Task.Run(() => { ... }).ConfigureAwait(false)` would be split).
2. **The first line:** free, any number of calls (RCS0054); or the receiver and at most one call (50 more chains, mostly
   FluentValidation tests; and `RuleFor(x).NotNull()` would be fine while `validator.RuleFor(x).NotNull()` wouldn't).
3. **What starts a line:** every `.` (RCS0054: Serilog's `.WriteTo` + newline + `.Sink(x)`); or each call step: the members
   from the one after a call up to and including the next call (`.WriteTo.Sink(x)` stays together), with a property
   after the last call left alone.
4. **Indentation:** enforce one unit deeper than the chain's first line from `.editorconfig` (RCS0054; Polly's 443
   aligned chains would be re-indented, and moved steps with lambda bodies need re-indenting); or don't check it and give
   new lines the indentation of the chain's own lines that already start with `.`.

### Answer

Defaults chosen from the survey (agent's proposal, 2026-10-04, for the owner to review): split chains only; the first
line free; call steps (a member that only leads to the next call stays with it, a trailing property stays on its call's
line); indentation not checked, new lines copy the chain's own. That reports 110 calls in 79 chains across the eight
repositories, each a call tacked onto a line where another call already sits. Only the gap before the `.` is rewritten;
a multi-line step moves only when its other lines are at least as deep as its new line, so argument contents never
change. Skipped: comments in the gap, directives anywhere in the chain, interpolated strings, syntax errors. Warning in
the preset; off after `stylebro-migrate` (no StyleCop rule asks for it). An indentation option (choice 4, RCS0054's)
could come later as a separate setting if someone asks.
## No blank line after attributes (StyleCop's proposed SA1521) (2026-10-04)

### Question

BRO1525 ([beyond-stylecop.md](beyond-stylecop.md#from-stylecops-issue-tracker) #2): StyleCop's issue #738 proposed SA1521,
"attributes defined for type or member should never be followed by blank line", and never implemented it (no
documentation page, no analyzer). Which gaps count (before the element only, or also between two attribute lists of
one element), what about comments in the gap, and does it conflict with BRO1505 (elements separated by a blank line)?

Survey of the eight reference repositories (scripts/realworld/repos.psd1), 25,160 attribute lists on members, 36 on
accessors, 2,539 on parameters:

| Gap after an attribute list | Count | Repositories |
|---|---|---|
| Blank line, then the element | 8 | Jellyfin 5, Newtonsoft.Json 1, OpenTelemetry 1, Polly 1 (mostly `[InlineData]` rows above a test method) |
| Blank line, then the next attribute list | 1 | Jellyfin |
| Blank line and a comment, then the next attribute list | 9 | Jellyfin (comments grouping `[InlineData]` rows) |
| Blank line and a comment, then the element | 1 | Jellyfin |
| On accessors or parameters | 0 | |

BRO1505 doesn't count these blank lines (an element's code starts at its first attribute), so a member whose only blank
line sits below its attribute is already a BRO1505 finding; the two fixes edit different lines and converge in either
order (FixOrderTests).

### Choices

1. **Every attribute list except assembly/module ones, before the element and between lists; gaps with a comment,
   documentation comment or directive skipped** (BRO1504 wants a blank line above a comment that follows code; an `#if`
   changes which element the attribute belongs to). The 9 commented `[InlineData]` groups stay.
2. **Only before the element:** stacked lists may be grouped with blank lines; 1 finding in the survey.
3. **Also comments:** remove the blank line above the comment; conflicts with BRO1504.

Preset: warning; `stylebro-migrate` writes it as `none` (StyleCop never shipped SA1521, so a StyleCop-clean repository
may have such blank lines).
## Null check style: `x == null` or `x is null` (2026-10-04)

### Question

[BRO1133](rules/BRO1133.md) ([beyond-stylecop.md](beyond-stylecop.md) #3, Roslynator RCS1248) writes null checks in one
form. Which form by default, and what about `null == x`?

Facts: the SDK has no rule or option for it. `dotnet_style_prefer_is_null_check_over_reference_equality_method` only
covers `ReferenceEquals(x, null)` (IDE0041). Probed with SDK 10.0.401 (`dotnet format style` and `analyzers`,
IDE0041/IDE0150/IDE0083/IDE0078 at warning with their options on): `x == null`, `x != null`, `null == x` and
`n == null` (an `int?`) stayed as they were; only `ReferenceEquals(o, null)`, `!(o is null)`, `o is object` and
`s == null || s == ""` were rewritten, all into patterns. Nothing turns `x is null` into `x == null`.

Survey of the eight reference repositories (scripts/realworld/repos.psd1, text count of `*.cs` lines that aren't
comments; `is null` also counts a few strings such as messages):

| Repository | `== null` | `!= null` | `null ==` | `is null` | `is not null` |
|---|---|---|---|---|---|
| CsvHelper | 127 | 83 | 0 | 16 | 1 |
| FFMpegCore | 21 | 21 | 0 | 3 | 3 |
| FluentValidation | 68 | 174 | 0 | 1 | 0 |
| Jellyfin | 38 | 94 | 0 | 1,035 | 1,371 |
| Newtonsoft.Json | 638 | 948 | 4 | 5 | 1 |
| OpenTelemetry | 193 | 364 | 0 | 65 | 74 |
| Polly | 372 | 52 | 0 | 85 | 45 |
| Serilog | 45 | 59 | 0 | 14 | 1 |
| Total | 1,502 | 1,795 | 4 | 1,224 | 1,496 |

Equality operators in seven of the eight repositories (3,301 checks); patterns in Jellyfin, which alone has most of the
2,720.

### Choices

1. **`equality_operator` by default**, `pattern_matching` as the option (`stylebro_null_check_style`).
2. **`pattern_matching` by default**: the newer form, never calls a user-defined `==`; needs C# 9 for `is not null`.
3. **Off by default**, on only when the option is set (like Roslynator).

`null == x`: with `pattern_matching` it becomes `x is null` directly (there is no `null is x`); with
`equality_operator` it is left to BRO1103. Both rules report `null == x` in pattern mode, and their fixes converge in
one `dotnet format` run (BRO1133 finds the check again by its start, which the swap keeps; samples/Messy).

### Answer

**Choice 2:** `pattern_matching` (owner's decision, 2026-10-04): the form that never calls a user-defined `==`;
configurable; records skipped. The survey's majority (choice 1, the agent's proposal) stays available as
`equality_operator`. At warning in the preset; off after `stylebro-migrate`.
## Split conditional expressions (StyleCop issue #651) (2026-10-04)

### Question

BRO1524 ([beyond-stylecop.md](beyond-stylecop.md#from-stylecops-issue-tracker) #1): once a conditional expression is
split, the condition, `? a` and `: b` each start their own line. Which side of the line break do `?` and `:` go on, what
counts as split, and what about chains (`a ? x : b ? y : z`), the open question in #651 (a contributor wanted
`cond ? value :` per line allowed, the maintainer preferred the strict rule or a switch expression)?

Survey of the eight reference repositories (scripts/realworld/repos.psd1), 2,283 conditional expressions, 719 with a line
break right next to `?` or `:`:

| Shape | Count | Repositories |
|---|---|---|
| Every part on its own line, `?`/`:` first | 631 | all eight (Jellyfin 332, OpenTelemetry 189, Newtonsoft.Json 53, Polly 23, CsvHelper 22, Serilog 7, FluentValidation 4, FFMpegCore 1) |
| Every part on its own line, `?`/`:` last | 41 | Jellyfin 20, OpenTelemetry 15, Serilog 5, Polly 1 |
| A token with its line break on the wrong side only (BRO1520's) | 8 | Jellyfin 4, OpenTelemetry 4 |
| Comment next to `?`/`:` | 8 | |
| `?` or `:` sharing its line with both neighbors (this rule) | 31 | Jellyfin 19, Newtonsoft.Json 6, OpenTelemetry 4, FluentValidation 1, Serilog 1 |
| ... of which in chains | 18 | Jellyfin 11 (both list styles), Newtonsoft.Json 5 (`cond ? value :` per line), OpenTelemetry 2 |

Ten more conditionals had a part spanning lines (a lambda, a wrapped call) with no line break next to `?`/`:`; none had
a moved part spanning lines.

### Choices

1. **Each conditional on its own, chains skipped** (a conditional whose `: b` is a conditional, or that is one's `: b`):
   the 13 non-chain findings are fixed; the 18 chain links stay as their authors laid them out.
2. **Each conditional on its own, chains included:** a chain laid out as a list becomes a staircase (each link one level
   deeper), which nobody in the survey writes.
3. **Chains as one list** (each `: condition ? value` on its own line): a second layout to define and fix, for 18 cases.

Side of the line break: BRO1520's setting (`dotnet_style_operator_placement_when_wrapping`, default `beginning_of_line`,
the survey's majority 631 to 41), so the two rules never disagree. Split = a line break right next to `?` or `:` (not
inside a part). Indentation of the new line: like the first part that already starts a line. Preset: warning;
`stylebro-migrate` writes it as `none` (no StyleCop rule asks for it).

### Answer

Choice 1 (agent's proposal, 2026-10-04, for the owner to review).
## Summary layout: one line or three (2026-10-04)

### Question

[beyond-stylecop.md](beyond-stylecop.md) #5: should a `<summary>` be written on one line
(`/// <summary>Gets the name.</summary>`) or with its tags on lines of their own, which elements does the rule check,
and what's the default?

Facts: Roslynator's [RCS1253](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1253) (off by default, needs
`roslynator_doc_comment_summary_style = single_line | multi_line`) checks only `<summary>`: `multi_line` reports a
summary written on one line; `single_line` reports a multi-line one whose text is one line, unless it has `<code>`,
`<list>` or `<para>`, with no length limit. Meziantou's
[MA0211](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0211.md) (off by default) reports
non-empty one-line summaries; [MA0177](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0177.md)
(off by default) reports any top-level element (summary, param, returns, ...) on several lines whose content is one
line and whose joined line fits within `max_line_length` (no limit when it isn't set). None of them moves the tags of a
summary whose text starts or ends on a tag's line (`/// <summary>Draws the shape` + `/// on the screen.</summary>`).

Survey of the eight reference repositories (scripts/realworld/repos.psd1), summaries whose text is one line (no
`<para>`/`<code>`/`<list>`), one-line form / tags on their own lines: Jellyfin 19/8,507, Newtonsoft.Json 11/1,650,
Polly 26/1,360, OpenTelemetry 75/1,163, CsvHelper 15/725, Serilog 1/456, FluentValidation 1/380, FFMpegCore 0/88;
in all 148/14,329. No summary had text on a tag's line. (StyleBro's own code is the other way round: 291/4.)

### Choices

1. **One rule, summary only, `stylebro_summary_layout = multi_line | single_line_when_fits`**, `max_line_length` for
   "fits" (no limit when unset, like MA0177); a summary with text on a tag's line gets its tags on lines of their own
   in both layouts.
2. Every top-level element (like MA0177): `<param>`, `<returns>` and `<remarks>` are usually written on one line even
   by teams that write summaries on three, so one option for all would fight most codebases.
3. A default limit (say 120) when `max_line_length` isn't set: the rule only joins text that is already one line, so
   the joined line is the text's line plus about 20 characters; a limit nobody configured would be a guess.

Proposed default: `multi_line` (the survey: 99% in every repository), BRO1616 at warning in the preset, off after
`stylebro-migrate` (no StyleCop rule asks for it). StyleBro's repository sets `single_line_when_fits` for its own code.

### Answer

**Choice 1**, default `multi_line`. Default chosen from the survey (agent's proposal, 2026-10-04, for the owner to
review); configurable. Implemented as [BRO1616](rules/BRO1616.md).

## Placement of operators, `=>` and `=` when a line wraps (2026-10-04)

### Question

The first rules beyond StyleCop ([beyond-stylecop.md](beyond-stylecop.md) #1 and #2) put a token on one side of a line
break: binary operators and `?`/`:` (Roslynator RCS0027/RCS0028), `=>` (RCS0032), `=` (RCS0052). What shape, which
tokens, and which defaults?

Facts: the SDK has `dotnet_style_operator_placement_when_wrapping` (default `beginning_of_line`) but no rule enforces
it. Probed with SDK 10.0.401: with the key set to `end_of_line`, `dotnet_diagnostic.IDE0055.severity = warning` and
beginning-of-line operators, `dotnet format --severity info` changed none of them, and the reverse (`beginning_of_line`
with end-of-line operators) didn't either; a build with `EnforceCodeStyleInBuild` reported no IDE diagnostic. Roslynator's
four rules are off by default and have no default value (their options must be set). There's no SDK key for `=>` or `=`.

Survey of the eight reference repositories (scripts/realworld/repos.psd1), tokens with a line break on one side,
beginning of line / end of line:

| Tokens | Beginning | End | Repositories (beginning / end) |
|---|---|---|---|
| `&&`, `\|\|` | 1,785 | 370 | Jellyfin 1,497/96, OpenTelemetry 165/175, Newtonsoft.Json 94/36, CsvHelper 17/17, FluentValidation 9/3, Serilog 3/33, Polly 0/10 |
| `+` | 134 | 168 | Newtonsoft.Json 41/15, Jellyfin 37/6, OpenTelemetry 56/62, CsvHelper 0/39, Polly 0/25, Serilog 0/18, FFMpegCore 0/3 |
| `??` | 128 | 27 | Jellyfin 63/18, OpenTelemetry 51/0, Newtonsoft.Json 10/5, FFMpegCore 4/2, Serilog 0/2 |
| other binary | 13 | 45 | Newtonsoft.Json 0/29, OpenTelemetry 1/9, Jellyfin 12/5, Polly 0/2 |
| `?` / `:` | 641 / 650 | 48 / 63 | beginning in every repository; Serilog closest (8/6) |
| `=>` expression bodies | 1,259 | 901 | beginning: OpenTelemetry 676/335, Jellyfin 398/14, FluentValidation 52/3, Serilog 2/0; end: Polly 130/543, FFMpegCore 0/3, CsvHelper 0/2; Newtonsoft.Json 1/1 |
| `=>` switch arms | 8 | 14 | OpenTelemetry 4/7, Jellyfin 3/5, Polly 0/2, Newtonsoft.Json 1/0 |
| `=>` lambdas | 9 | 5,299 | end everywhere (block bodies included) |
| `=` assignments and initializers | 6 | 518 | end in all eight |

All operators together: beginning in six repositories, end in Serilog, FFMpegCore even.

### Choices

1. **One rule per token kind, one analyzer and one fix:** BRO1520 operators (follows the SDK key), BRO1521 `=>`
   (`stylebro_arrow_placement_when_wrapping`), BRO1522 `=` (`stylebro_equals_placement_when_wrapping`); each can be
   turned off on its own.
2. **One rule with three options:** fewer ids, but a team that wants only the operator rule has to set the other two to
   "don't care", which needs a third option value.
3. **Two rules** like the survey's pairing (operators; `=>` and `=` together): `=>` and `=` have opposite majorities, so
   they need separate options anyway.

Proposed defaults: operators `beginning_of_line` (the SDK's default, and the survey's majority), `=>` `beginning_of_line`
(four repositories to three, 1,259 to 901 tokens), `=` `end_of_line` (518 to 6). Scope of `=>`: expression bodies and
switch arms; lambdas left out (5,299 to 9 at the end, and a block body's `{` belongs on its own line). Preset: all three
at warning; `stylebro-migrate` writes them as `none` (no StyleCop rule asks for them).

### Answer

**Choice 1** (user's decision, 2026-10-04), with these defaults, all configurable: operators `beginning_of_line` (the
SDK's default and the survey's majority), `=` `end_of_line` (the survey), `=>` `end_of_line` (the owner's choice over the
proposed `beginning_of_line`: one convention for every `=>`, like lambdas and StyleBro's own code). All three at warning
in the preset; off after `stylebro-migrate`.

## Namespace names (SA1300 for namespaces) (2026-10-04)

### Question

BRO1309 skipped namespaces, so SA1300's check on namespace names had no StyleBro rule. A namespace rename changes the
full name of every type in it and can break things a code rename can't see (details and experiments in
[proposals/namespace-names.md](proposals/namespace-names.md)): plain embedded resources follow `RootNamespace`, not the
code; strings and stored data with type names (`Type.GetType`, `$type`); Razor and XAML references; config files. In
22 public repositories every lower-case namespace part was deliberate (brand names like `iText`, `iOS`; culture
codes). Open questions: ship a rule at all, as a separate id and on or off by default; should `stylebro-migrate` turn
it on with SA1300; report the root namespace (which the fix can't rename) or not; rename public namespaces in
libraries; and what to do about resources in folders, config files and XAML.

### Choices

1. **Ship opt-in:** a separate rule (BRO1312), off in the descriptor and the preset, with the prototype's guards;
   `stylebro-migrate` turns it on when SA1300 is on; the root namespace isn't reported; public namespaces are renamed
   like BRO1309 renames public types; the resource, config and XAML limits are documented.
2. **Ship with the root namespace reported but not fixed** (a diagnostic `dotnet format` can't fix).
3. **Don't rename public namespaces** (skip namespaces with a public type, or packable projects).
4. **Keep the skip** and record it with the findings.

### Answer

Ship opt-in (user's decision, 2026-10-04). Choice 1: BRO1312 off by default, on through `stylebro-migrate` with
SA1300; root namespace unreported; public namespaces renamed; resources in folders, config files and XAML documented as
limits (no package change to expose embedded resources).

## SA1108: where a comment between a statement's header and its block goes (2026-10-04)

### Question

SA1108 reports a comment between a statement's header and its `{` (`if (x) // c`, `else // c`, `catch (E) // c`), and
StyleCop has no fix. StyleBro needs one, so where should the comment go? Found in the reference repos: 34 (all `//` at
the end of the header line); 4 of them end a condition split over lines and explain its last line, not the block.
Details: [proposals/sa1108.md](proposals/sa1108.md).

### Choices

- **A:** into the block, on its own line right after `{`; one behavior for every statement and clause.
- **A':** like A, but headers spanning several lines aren't reported (each clause judged on its own).
- **B:** on its own line above the statement; `else`/`catch`/`finally`/`else if` have no "above" and would need A.
- **C:** at the end of the `{` line (`{ // c`); the smallest move, an unusual layout.
- **D:** report only, no fix; not allowed by StyleBro's design, so the rule would stay dropped.

### Answer

**A'** (user's decision, 2026-10-04). Implemented as [BRO1132](rules/BRO1132.md); the multi-line header skip is a
documented difference from StyleCop.


## IDE0055 in multi-targeted repositories (2026-10-03)

### Question

With IDE0055 (the SDK's formatting rule) at warning, `dotnet format` crashes on multi-targeted repositories and writes
nothing (Serilog, Newtonsoft.Json; no StyleBro involved). `init` wrote it as a suggestion there, so formatting wasn't
enforced. Investigated: the crash is Roslyn's linked-file merge (`LinkedFileMergeConflictCommentAdditionService`:
"Changes must be within bounds of SourceText"); IDE0055's fix edits each framework's copy of a file, each copy sees
other `#if` code, the results can't be merged. The whitespace pass doesn't crash but only formats the first framework's
code (Serilog: 280 warnings left, all inside `#if FEATURE_...` blocks net471 doesn't compile). Loading the projects
for one framework at a time (`TargetFramework` as an environment variable) leaves nothing to merge: once per framework,
Serilog went from 505 IDE0055 warnings to 0, no crash, no conflict markers, builds. How should StyleBro offer that?

### Choices

1. **`stylebro-migrate format`:** a command that runs `dotnet format` once per target framework; `init` writes IDE0055
   at warning everywhere and the docs use the command.
2. **Document a script** for multi-targeted repos; `init` keeps the suggestion.
3. **Build-only enforcement:** the severity in a `.globalconfig` (the build honors it, `dotnet format`'s style pass
   doesn't): no crash, but `#if` code the first framework doesn't see has to be fixed by hand.
4. **Leave as is** and record the findings.

### Answer

**Choice 1** (user's decision). Each run uses a temporary solution filter with the projects that target that
framework, so no project is loaded for a framework it doesn't have. Known limit: code that needs different
indentation per framework (an `else` inside `#if`) can't satisfy IDE0055 for all of them (Newtonsoft.Json: 32 lines).

## After StyleCop parity (2026-10-03)

### Question

StyleCop parity is essentially done: of StyleCop's 197 rules, 104 are StyleBro rules, 45 SDK settings and 43 dropped by
design, with one rule (SA1316) and two small gaps left as "maybe". What should the backlog grow into next?

### Choices

1. **Hardening:** more real-world repos in the PR pipeline (each new one has found bugs), and making IDE0055 safe in
   multi-targeted repos.
2. **Finish the maybes:** SA1316 (tuple element casing), SA1108 (comments inside block statements), namespace names
   (BRO1309). Completes parity, little real-world impact.
3. **Adoption polish:** getting-started docs, the Visual Studio/Rider experience, a StyleCop migration sample.
4. **Beyond StyleCop:** survey other analyzers for fixable style rules the SDK doesn't cover.

### Answer

**All four** (user's decision), in this order. The items are in [backlog.md](backlog.md) under "Work beyond single
rules". IDE0055 is an investigation, not a reimplementation: it's the SDK's whole formatter, and analyzers can't
reference the formatting engine.

## Follow the SDK's .editorconfig settings where they exist (2026-10-03)

### Question

StyleBro reads the SDK's layout settings (`indent_*`, `csharp_new_line_before_*`) and rule severities, but for style
choices it follows StyleCop's behavior plus its own `stylebro_*` keys, even where the SDK has a key for the same choice:
naming rules (`dotnet_naming_rule.*`), `this.` qualification, `csharp_prefer_braces`,
`dotnet_style_require_accessibility_modifiers`, `dotnet_style_parentheses_*`, `end_of_line`. A team whose
`.editorconfig` says one thing can get a StyleBro fix that does another.

### Choices

1. **Keep StyleCop's behavior and `stylebro_*` keys only.** Predictable for StyleCop migrations; `stylebro-migrate`
   writes the keys. But teams configuring the SDK way have to say everything twice.
2. **Read the SDK keys where they exist**, with StyleCop's behavior as the default when they aren't set.

### Answer

**Choice 2** (user's decision). Precedence: a `stylebro_*` key, then the SDK key, then StyleCop's behavior. The list of
keys and what each changes is in [backlog.md](backlog.md).

To watch: the preset sets some of these SDK keys itself (`dotnet_style_qualification_for_* = false`,
`csharp_prefer_braces = true`, `dotnet_style_require_accessibility_modifiers = for_non_interface_members`,
`dotnet_style_parentheses_*`), and `stylebro-migrate` writes them too. Once they're read, those values decide behavior,
so each has to be checked against what the rule does today (e.g. qualification `false` would make BRO1131 drop `base.`
instead of writing `this.`; the preset and migrate values may need to change, or the rule may only follow an explicit
setting outside the preset).

## StyleBro versions of SDK rules that break multi-targeted projects (2026-10-03)

### Question

StyleBro leaves formatting to the .NET SDK where the SDK already covers it: `stylebro-migrate init` turns on the SDK's
IDE rules instead of StyleBro shipping its own. But in multi-targeted projects (`<TargetFrameworks>` with several
frameworks), some of the SDK's own fixers break `dotnet format`, even without StyleBro installed. Measured on Serilog
and Newtonsoft.Json:

| Rule | StyleCop rules it replaces | What goes wrong |
|------|----------------------------|-----------------|
| IDE0011 (braces) | SA1503, SA1519, SA1520 | Crashes Roslyn's linked-file merge; `dotnet format` writes nothing |
| IDE0055 (formatting, at warning) | SA1000-SA1028, SA1134, SA1137, SA1500, SA1107 | Crashes the same way |
| IDE0040 (access modifiers) | SA1400, SA1205 | Writes merge conflict markers into the source |
| IDE0047 (unnecessary parentheses) | SA1119 | Conflict markers |
| IDE0048 (parentheses for clarity) | SA1407, SA1408 | A broken edit in one framework's code |
| IDE2000/IDE2002/IDE2003 (blank lines) | SA1507, SA1508, SA1513 | One blank-line conflict per repo |

Today `init` writes these 8 rules as `suggestion` in multi-targeted repos, so they're reported in the IDE but never
fixed or enforced there.

### Choices

1. **Keep the SDK rules and the workaround.** No new code, and it stays true to "formatting belongs to the SDK". But
   multi-targeted repos (common for libraries) don't get these StyleCop rules enforced at all.
2. **StyleBro versions of the affected rules.** BRO rules with fixes built on `LinkedFileFixAllProvider`, which already
   handles multi-targeted files correctly. Enforced everywhere, at the cost of duplicating SDK functionality and
   maintaining it.
3. **Report the bugs upstream and wait.** Right thing to do in any case, but no timeline.

### Answer

**Choice 2** (user's decision). StyleBro ships its own versions of these rules; `init` and `stylebro-migrate` turn the
BRO rule on and the SDK rule off. The SDK bugs should still be reported upstream.

Scope, as far as a BRO version is feasible: braces (IDE0011), access modifiers (IDE0040), parentheses (IDE0047,
IDE0048) and the blank-line rules (IDE2000, IDE2002, IDE2003). IDE0055 is the SDK's whole formatter and isn't
reimplemented; the safe part (`dotnet format`'s whitespace pass, which isn't affected) keeps doing that job, and only
the rule's warning-level enforcement stays a suggestion in multi-targeted repos.

## Which literal suffixes BRO1135 upper-cases (2026-10-04)
## Braceless jump statements on the `if` line (2026-10-04)

Agent's proposal, 2026-10-04, for the owner to review.

### Question

StyleCop issue [#1563](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1563) asks for upper-case literal
suffixes (`50l` -> `50L`, `50.4m` -> `50.4M`). In the issue, maintainers and commenters wanted `L` but preferred lower
case for `m`/`f` (and the exponent's `e`); Sonar's S818 reports only `l` and leaves `ul` alone; the compiler warns
(CS0078) about `l` only. Which suffixes should the rule cover?

### Choices

1. **Integer suffixes** (`u`, `l` and their combinations): `1l` -> `1L`, `1u` -> `1U`, `1ul` -> `1UL`. One way to write
   an integer suffix; real suffixes stay as written.
2. **`l` only** (S818, CS0078): the one letter that looks like a digit.
3. **Every suffix** (the issue as written): also `f`, `d`, `m`.

### Answer (proposed)

**Choice 1** (BRO1135). Survey of the 8 reference repositories (line-based, outside strings and comments): lower-case
integer suffixes only in OpenTelemetry (24 `u`, 1 `ul`, e.g. `0x9E3779B1u`), no `l` anywhere; real suffixes are
written in lower case almost everywhere (e.g. Newtonsoft.Json 517 lower vs 40 upper, Jellyfin 92 vs 0), so choice 3
would rewrite hundreds of literals against the common style. Choice 2 would report nothing in these repos; choice 1
finds 25. Not a StyleCop rule, so `stylebro-migrate` turns it off; the preset turns it on.
StyleCop issue [#2252](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2252) (12 reactions; #1175 asked
the same) wants `if (x) return;` without braces while every other child statement still needs them. Which statements,
where, and how does it combine with the other brace rules?

### Choices

1. **Jump statements on the `if` line only** (the request as written): `return`, `throw`, `break`, `continue`, plus
   `goto` (also a jump statement in the C# spec) and `yield break` (leaves an iterator like `return`). Not on the next
   line (a statement added below looks like part of the `if`), not after `else`.
2. **Any single-line statement on the `if` line** (`if (x) a++;`). The issue's author ruled this out explicitly.
3. **A second option for "next line too"** (sharwell's question 2 in the issue). Nobody asked for it: those who wanted
   the next line wanted it for every statement, which `csharp_prefer_braces = when_multiline` already allows.

### Answer (proposed)

**Choice 1**, as `stylebro_allow_single_line_jump_statements = true|false` (default `false`: StyleCop's behavior). It
only exempts statements from BRO1514. In an `if`/`else` chain where another clause has braces or gets them in the same
run, BRO1516 still wants them on the jump too, so the chain ends up consistent in one `dotnet format` run.
`when_multiline` already allows all single-line children (the option adds nothing there), and BRO1508/BRO1509 aren't
involved (no block, so nothing to collapse or expand). No `stylecop.json` setting maps to it.

Survey of the 8 reference repos (line-based count of braceless `if (...) <jump>;` on one line / jump on the next line /
another statement on the `if` line): CsvHelper 27 / 0 / 0, FluentValidation 22 / 13 / 2, Newtonsoft.Json 19 / 33 / 1,
Serilog 31 / 81 / 0, FFMpegCore, Jellyfin, OpenTelemetry and Polly 0 / 0 / 0. Where braceless `if`s exist, the
same-line form is almost always a jump, which is what the option allows.
