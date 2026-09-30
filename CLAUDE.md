# StyleBro

Roslyn analyzers + code fixes for C#: a modern replacement for StyleCop.Analyzers
(last stable release 1.1.118, stuck in 1.2.0-beta for years). Name: **StyleBro** (the friendly successor to StyleCop: it fixes things for you instead of writing tickets). Rule prefix `BRO`, config keys `stylebro_*`.
The project was renamed from "Kempt" (pure text/file-name replacement). Repo: `github.com/bisforboman/stylebro`.

## Core idea

Only analyzers **with code fixes that `dotnet format` can apply**. The goal is that running
`dotnet format` fixes everything StyleBro reports. Formatting-only rules are out of scope where the SDK
already covers them (IDE rules, `.editorconfig`); the preset enables those built-in rules instead.

## Status

Re-verified after the rename to StyleBro (clean tree, SDK 10.0.401, 2026-09-29): `dotnet build StyleBro.slnx`
(0 warnings), `dotnet test StyleBro.slnx` (11/11 passed), `scripts/verify-format.ps1` (both passes OK, output
matches `Expected/`) and `dotnet pack src/StyleBro.Package` (`StyleBro.Analyzers.0.1.0-alpha.1.nupkg` with both
DLLs, targets and globalconfig) are all green. The rename needed no fixes. Since then: real-world testing
(see the log below) and 44 rules; 251 unit tests (incl. every doc example), all green (2026-09-30).
0.1.0-alpha.3 (18 rules) is on nuget.org.

**Release policy (user's decision, 2026-09-30):** don't publish a version for every batch of rules while the project
is starting out. Keep adding rules on `main` and release again once there's a critical mass of new rules. Don't
suggest or push a release tag after each batch; mention it only when a release looks worth it.

## Layout

- `src/StyleBro.Analyzers`: netstandard2.0, references Microsoft.CodeAnalysis.CSharp only.
  Must never reference Workspaces (RS1038).
- `src/StyleBro.CodeFixes`: netstandard2.0, Workspaces, code fixes + Fix All providers. References Analyzers.
- `src/StyleBro.Package`: packs both DLLs into `analyzers/dotnet/cs`, plus `build/StyleBro.Analyzers.targets`
  that adds `stylebro.recommended.globalconfig` (global_level -1; opt out with `<StyleBroPreset>none</StyleBroPreset>`).
- `tests/StyleBro.Tests`: xunit + Microsoft.CodeAnalysis.CSharp.CodeFix.Testing 1.1.2 (`DefaultVerifier`).
- `samples/Messy`: dotnet format integration sample. `Input/*.cs` is copied to `Generated/` (gitignored),
  formatted, compared with `Expected/`, then a second run with `--verify-no-changes` must pass.
- Central package management in `Directory.Packages.props`. Roslyn pinned to **4.8.0** for broad SDK/VS
  compatibility; don't raise it without a reason.
- `scripts/stylecop-survey`: generates `docs/stylecop-mapping.md` (inventory of StyleCop rules, repo surveys,
  SDK coverage check). Hand-written decisions in `decisions.psd1`, measured data in `data/`. Not part of the slnx.
- `samples/MultiTarget`: net10.0 + net8.0 with `#if NET10_0_OR_GREATER` code; verify-format checks it like Messy and
  also that the fixed sample builds. It does NOT reproduce the old linked-file bug (couldn't find a minimal repro);
  the reference for that bug is Newtonsoft.Json (8 target frameworks) with all rules in the real-world run.
- `scripts/verify-format.ps1` reads the rule IDs from `AnalyzerReleases.Unshipped.md` and runs every sample.
- C# files under `src/` and `tests/` use LF line endings (normalized 2026-09-30; a few had become mixed from scripted
  edits inserting CRLF, which then made exact-text replacements fail). Keep new edits LF.
- `.github/workflows/ci.yml`: ubuntu-latest, .NET 10; runs test, verify-format, pack, uploads the nupkg.
- `.github/workflows/release.yml`: on a `v*` tag, runs the same checks, packs with the version from the tag
  (`v0.1.0-alpha.1` -> `0.1.0-alpha.1`; overrides `<Version>` in the csproj), pushes to nuget.org via Trusted
  Publishing (`NuGet/login@v1`, no stored API key) and creates a GitHub release (prerelease if the version has a `-`).
  Set up: nuget.org Trusted Publishing policy (repo owner bisforboman, repo stylebro, workflow `release.yml`,
  environment `release`, packages `StyleBro.*`, new packages allowed), repo *variable* `NUGET_USER` = `bisforboman`
  (nuget.org profile name; a variable, not a secret, so logs aren't masked), and the `release` environment
  requires the owner's approval. The first failure (HTTP 401
  "No matching trust policy") was simply a missing policy.

## Design rules for every rule

1. Every diagnostic has a code fix, and **Fix All works**. `dotnet format` applies fixes through Fix All.
   Use `LinkedFileFixAllProvider.Create(FixDocumentAsync)` (src/StyleBro.CodeFixes), never plain
   `FixAllProvider.Create`: in multi-targeted projects each target framework has a linked copy of every file, `#if`
   makes the copies need different edits, and the plain provider left the merge to `dotnet format`, which wrote
   conflict markers (and, with a diff-based merge, duplicated members) into Newtonsoft.Json. The linked-file provider
   merges exact edits per physical file (tree rewrites count as one whole-text edit) and gives every copy the same text.
   Also avoid decisions that depend on `#if`-conditional code (e.g. "which item is last"): skip such cases.
2. Fixes are **deterministic and idempotent**: after a fix, the analyzer reports nothing, and a second
   `dotnet format` run changes nothing. Analyzer and fix share one piece of logic so they can't disagree.
3. When a fix could be unsafe (e.g. preprocessor directives between members), skip the case: no diagnostic
   rather than a wrong fix.
4. Configuration goes through `.editorconfig` keys prefixed `stylebro_`, read via `AnalyzerConfigOptionsProvider`
   in both the analyzer and the fix.
5. Every rule gets a `docs/rules/BROxxxx.md`, a row in `AnalyzerReleases.Unshipped.md`, unit tests covering
   the single fix + Fix All, and a case in `samples/Messy`. Rule pages share one structure: intro, `## Example`
   with `### Before`/`### After` csharp blocks, `## Why`, then as needed How the fix works / Configuration /
   Not reported / Compared with StyleCop. `DocExamplesTests` runs every example (Before compiles and gets the
   diagnostic, one Fix All pass gives exactly After, After compiles clean), so examples can't go stale.

## BRO1001: member ordering (implemented, unit + dotnet format tests pass)

- Replaces SA1201-SA1204 and SA1214. Sort key: kind -> accessibility -> const -> static -> readonly.
  Defaults match StyleCop so migration is painless (kind order verified pairwise against StyleCop 1.1.118).
- One diagnostic per type, on the first out-of-place member. The fix sorts the whole type.
- Trivia: blank-line layout stays with the position; comments, docs and attributes move with the member.
  Exception: a member led by a `//` comment moved into a later slot without a blank line gets one, otherwise the
  sort creates a BRO1504 violation and `dotnet format` needs a second run (seen in Newtonsoft.Json).
- The Fix All rewriter sorts nested types before their parents in one pass.
- Types with directives between members (`#region`, `#if`, `#pragma`) are skipped.
- Types where sorting would swap dependent field/auto-property initializers are skipped (`InitializerOrder.cs`).
- Code: `src/StyleBro.Analyzers/Ordering/MemberOrdering.cs` (shared logic), `MemberOrderOptions.cs`,
  `MemberOrderingAnalyzer.cs`, `src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs`.

## BRO11xx: readability (numbered in blocks like StyleCop: BRO11xx readability, BRO16xx documentation)

- **BRO1101** (SA1106) empty statements: `;` in blocks/switch sections and after a type's or namespace's `}`.
  Not reported: embedded (`while (x) ;`, left to the compiler's CS0642; a `{ }` fix would hide a likely bug),
  labeled (`end: ;`, removal can break the build), bodiless records. The fix is text-based and computed per line
  (`EmptyStatements.GetChanges`), so single fix and Fix All agree: whitespace-only lines go completely, otherwise
  the `;` takes the adjacent whitespace. Directives always have their own line, so no directive skip is needed.
- **BRO1102** (SA1133) combined attributes: `[A, B]` -> `[A]` + `[B]`, target repeated. Own line -> one line per
  attribute; shares a line with code -> `[A] [B]`. Like StyleCop, parameters/type parameters are not reported.
  Lists with comments between attributes are skipped (StyleCop's fix drops the comment).
- **BRO1103** (SA1131) constant on the left of a comparison: swap operands, flip `<`/`>`/`<=`/`>=`. "Constant" as in
  StyleCop (incl. `static readonly` fields). Only built-in operators and operators from the core library (same
  assembly as `object`) are swapped; a type's own `==` may not be symmetric -> not reported (deviation). Same-precedence
  left operand gets parentheses (`1 == 2 == b` -> `b == (1 == 2)`). Fix All is a bottom-up rewriter (nesting).
- **BRO1104** (SA1129) `new T()` for value types -> `default(T)`, or like StyleCop `CancellationToken.None`,
  `Guid.Empty`, `IntPtr/UIntPtr.Zero`, an enum's zero member; `default(T)` in parameter defaults (must be constant).
  Not reported: explicit parameterless struct ctor (like StyleCop), type parameters and `new S();` statements
  (deviations; StyleCop's fix for the statement doesn't compile). Needs the semantic model.
- For both, StyleCop and StyleBro were run on the same probe files: positions and fixed output are identical except
  the three documented deviations.
- **BRO1105** (SA1128) `: base(...)`/`: this(...)` on its own line, one level deeper than the constructor
  (indentation from `.editorconfig` via `Indentation.GetUnit`). Only the stretch colon..keyword is rewritten, so
  arguments/body/comment-before-colon stay; skipped when a comment sits in that stretch.
- **BRO1002** (SA1005, numbered in the 10xx block next to BRO1001, which predates the block scheme) space after `//`.
  Exempt like StyleCop 1.2: already spaced (incl. `//  two`), empty, `///`/`////`, `//--`. StyleCop 1.1.118 also
  reported `//  two spaces` (the only difference in the private app: 77 vs 78). Whitespace-only -> `//`.
- Shared logic in `src/StyleBro.Analyzers/Readability/`, fixes in `src/StyleBro.CodeFixes/Readability/`.
- **BRO1106** (SA1122) `""`/`@""` -> `string.Empty`, except constant contexts (const, attribute args, parameter
  defaults, case labels, patterns). Same results as StyleCop.
- **BRO1401** (SA1413, maintainability block) trailing comma in multi-line array/object/collection/with initializers,
  anonymous objects, enums, switch expressions (not collection expressions/patterns/`{k, v}` pairs, like StyleCop 1.2).
  Inserts `, ` when code follows directly (StyleCop's `,}` broke SA1001 in the private app). Skips lists with
  directives between the braces (the multi-targeting conflict). Nested lists fixed in one pass (StyleCop needs two).
- **BRO1107** (SA1116) first item on the line after `(` when the list is split, and **BRO1108** (SA1117) items all
  on one line or each on its own (`Readability/ParameterLayout.cs`). Lists: parameter/bracketed parameter/argument/
  bracketed argument/attribute argument lists and array rank specifiers. SA1117 as probed: the first two items'
  START lines pick the mode; same line -> every item must start on item 0's line; different -> no item may start on
  the line where the previous item ENDS. BRO1107 only when item 1 starts below `(` (first two on one line = BRO1108).
  One fix: BRO1107 = the first edit of BRO1108's "each item on its own line" (never joins lines; items already
  starting a line keep their indentation). Skipped: syntax errors, comments/directives in the gaps. StyleCop
  1.2.0-beta.556 misses records/primary constructors (documented deviation) and has no working SA1117 fix. Nested
  split lists: single fix and Fix All differ in the inner list's indentation (test uses `batchFixedSource`).
- **BRO1109** (SA1110) `(`/`[` on the name's line and **BRO1110** (SA1111) `)`/`]` on the last item's line
  (`Readability/ParenthesisPlacement.cs`, same list kinds as BRO1107). BRO1109 only for lists that belong to a name
  (declarations, calls, `new`, element access, attributes, constructor initializers; not lambdas); the fix swaps the
  gap and the token (`Method` / `(int a)` -> `Method(` / `int a)`), empty lists are joined. BRO1110 moves the token
  after the last item and keeps a trailing comment after it (`int b) // last`); skipped for empty lists (SA1009's),
  comments on their own line/directives, and a trailing comment when code follows the token on its line.
- **BRO1111** (SA1127) each `where` on its own line, indented one unit deeper than the name's line; for an
  expression-bodied member the `=>` after the last constraint moves too (like StyleCop's fix).
- **BRO1112** (SA1124, regions between members/types; OFF in the preset like StyleCop's default) and **BRO1113**
  (SA1123, regions inside member bodies): the fix removes each `#region`/`#endregion` line pair, one edit per run of
  removed lines, and drops a neighboring blank line that would double a blank line, follow `{` or precede `}`. A run
  at the end of the file takes the preceding line break along. Removing regions lets BRO1001 see the type, so a
  region-heavy codebase needs a second `dotnet format` run (documented in BRO1112.md).
- Parity: `parenthesis-placement` 13/13 and `constraints-regions` 15/15 positions, identical fixed output.
- **StyleCop parity check:** `scripts/stylecop-survey/Compare-WithStyleCop.ps1` + `parity/parity.psd1`. Runs both
  tools on edge-case files, compares positions and fixed output; every difference must be a documented deviation.
  StyleBro's fixed output must also be clean ("StyleBro fix leaves: ..." otherwise; this found a BRO1506 bug:
  the empty line after a file's final line break counted as a blank line below a comment). `CompareOutput = $false`
  skips the output diff when StyleCop has no fix (SA1117) or a broken one (SA1312/SA1313: under `dotnet format`
  its rename fix applies a different subset of renames per run). All 21 sets pass. Add a set for every new rule.
- Tests use the generic `Verifier<TAnalyzer, TCodeFix>`; each skip condition was checked by disabling it and
  confirming a test fails.

## BRO13xx: naming

- **BRO1301** (SA1312) variables and **BRO1302** (SA1313) parameters begin with a lower-case letter
  (`Naming/CamelCaseNames.cs`, `CamelCaseNamingAnalyzer`, fix `Naming/CamelCaseRenamer.cs`). New name: leading `_`
  stripped, leading capital run lowered except the capital starting the next word (Newtonsoft's ToCamelCase:
  `URL`->`url`, `HTMLParser`->`htmlParser`, `IDs`->`iDs`); none for only-underscores, digit start, keywords.
  The new name travels in the diagnostic's properties (`NewName`).
- Safety (`CanRename`, syntax only, conservative, over the containing member; primary ctor param = whole type;
  top-level statements = compilation unit): skip when any identifier there is already the new name or another name
  maps to it, when the old name is an inferred anonymous-member/tuple-element name, or when the member has `#if`.
  XML element names (`<param>`) don't count.
- BRO1302 skips record parameters (properties), partial methods, and parameters that keep their base's name
  (override/implicit/explicit implementation, like StyleCop). The fix renames via `SymbolFinder.FindReferencesAsync`
  (finds named arguments in other files and `<param>`/`<paramref>` docs), cascades to same-named parameters of
  overrides/implementations (a conflicting one keeps its name and is then not reported either), handles linked files
  by processing every copy and merging edits per file (`LinkedFileFixAllProvider.Merge`). Own Fix All provider
  (all renames computed on the original solution, applied at once). Deviation: `_`/`__` params not reported.
- **BRO1303** (SA1306 + SA1309, private fields only; user's decision: configurable, `camelCase` default)
  `stylebro_private_field_naming = camelCase | _camelCase` (`Naming/FieldNames.cs`, `FieldNamingAnalyzer`, a symbol
  action; same fix provider/renamer). Checked: private, not const, not static readonly. One-letter prefixes (`m_`,
  `s_`) left alone in both styles (SA1308; `s_` is the runtime's static convention). Analyzer skips (whole type, all
  partial parts via DeclaringSyntaxReferences): member with the new name in the type or a base, another checked field
  with the same new name, attributes or [Serializable], old name as a string literal in the type, in disabled `#if`
  text, or as an inferred anonymous/tuple name. The fix qualifies references a local/parameter would hide
  (`LookupSymbols` at the reference: `this.count`, `Type.count` for statics), except `x.Name`, `Name = ` in object
  initializers, named arguments.
- **Reflection guard (fix side):** a field whose old name is a string literal ANYWHERE in the solution is not renamed
  and its warning stays (deliberate exception to design rule 1: the analyzer can't see other projects, and the rename
  compiles but breaks at run time). Found because Polly's tests read `_blockedUntil`/`_registry` via GetField: the
  first run compiled cleanly but failed 7 tests. Test helper `VerifyNotFixedAsync`.
- **BRO1304** (SA1302, interfaces begin with `I`) and **BRO1305** (SA1314, type parameters begin with `T`)
  (`Naming/PrefixNames.cs`, `PrefixNamingAnalyzer`, same renamer). Like StyleCop only the first letter is checked
  (`Item` interface, `Type` type parameter are fine). `Shape`/`iShape`/`_Shape` -> `IShape`; `Item` -> `TItem`,
  `t` -> `T`, `tKey` -> `TKey`. Analyzer skips: a source type named the new name anywhere in the compilation, or
  `LookupSymbols(newName)` non-empty at the declaration (this covers members, type parameters and imported types in
  scope; the explicit container walk was redundant and removed after mutation testing); type parameters inherited
  from a base method (cascade like parameters, generalized `AddRelatedAsync`); partial methods. Fix: aborts when
  any reference would see something else under the new name (warning stays; e.g. `using System.Collections;` in
  another file + interface `Enumerable`), and interfaces get the string guard (name or qualified name in a string).
  Parity: 14/14 positions. Files aren't renamed (`dotnet format` can't).
- **BRO1306** (SA1303 const, SA1311 static readonly, SA1307 public/internal/protected internal, SA1304) fields are
  PascalCase; same analyzer (`FieldNamingAnalyzer`), `FieldNames.IsPascalChecked`/`GetPascalName`/`GetNewName(field)`.
  Protected fields are camelCase in StyleCop (SA1306) and left out of both BRO1303 and BRO1306; enum members are
  SA1300's. One diagnostic per field (StyleCop may report SA1307 + SA1311/SA1304 on the same one). `_x` consts are
  renamed too (StyleCop leaves them to SA1309). Public instance fields are data (serializers write them): the string
  guards (in the type and solution-wide) also match the name inside a string. Fix: when a reference would see
  another MEMBER with the new name (a derived type's property hiding it), the rename is skipped (only locals and
  parameters are qualified around); this also applies to BRO1303.
- **BRO1307** (SA1308, `m_`/`s_`/`t_` prefixes, lowercase only) and **BRO1308** (SA1310, underscore inside a field
  name): `FieldNames.GetRename` gives every field at most ONE rule and the COMPLETE new name (prefix > underscore >
  casing), so one rename converges and Fix All never gets two names for one field. Words are joined in the field's
  casing (Pascal for const/static readonly/non-private, the BRO1303 style for private, camel for protected); an
  all-caps word of 2+ letters counts as a word (`MAX_VALUE` -> `MaxValue`). Skipped: digit_digit (`Int32_0` would
  become `Int320`, found in Newtonsoft.Json), results that are keywords (`s_static`) or empty (`m_`); `t_` fields are
  usually `[ThreadStatic]` (attributed -> skipped).
- **BRO1309** (SA1300, not namespaces): types (not interfaces), methods, properties, events (incl. field-like), enum
  members, local functions (`PascalCaseNamingAnalyzer`, syntax node actions). Skips: extern/DllImport/LibraryImport,
  attributed properties/enum members, partial methods, indexers/operators, inherited names (explicit
  implementations always), new name visible at the declaration (lookup covers members of the type and bases, types,
  namespaces; for types also any source type). Fix: members cascade to overrides/implementations incl. explicit ones
  (`AddRelatedMembersAsync`, simple-name match); types also rename constructors/finalizer declarations and
  `new T(...)` (constructor references with the type's text; `this(...)`/`base(...)` skipped); abort when a derived
  type has a member with the new name (`HasDerivedMemberAsync`) or a simple-name reference would see something else.
- **Guards added after running repos' TESTS (compiling was not enough):** (1) a type with a serializer attribute
  (name contains Serializ/Json/DataContract/MessagePack/Proto, `FieldNames.IsSerialized`) keeps its fields' and
  properties' names: Newtonsoft.Json's `[JsonObject(MemberSerialization.Fields)]` MyTuple broke 2 tests. (2) A member
  paired with another by a naming convention (`ShouldSerializeX`, `ResetX`, `XSpecified`, `XChanged`/`XChanging`,
  `OnXChanged`/`OnXChanging`) keeps its name (`FieldNames.HasRelatedMemberName`): Json.NET's ShouldSerialize
  convention broke a test. (A first version matched any member name CONTAINING the name; that also skipped
  `publicLower` because of `publicLowerReadonly` and didn't converge once the longer name was renamed.) (3) Names are matched inside strings, not only as whole strings: in the
  field's own type for every field (`DebuggerDisplay("{_count}")`), and solution-wide for names serializers write
  (public instance fields, properties, events, enum members; test JSON). Private fields are matched solution-wide
  only by their exact name (reflection): matching the word inside any string made common names like `count` block
  every rename, and the Messy sample stopped converging.
- Remaining naming: SA1305 (Hungarian, StyleCop has no fix), protected fields, namespaces.

## BRO16xx: documentation

- **Scope (user's decision, 2026-09-30): no generated stubs.** Missing documentation (SA1600, SA1602, SA1611,
  SA1615, SA1609, ...) can only be "fixed" by inserting placeholder text, which satisfies the rule but documents
  nothing (SA1600 alone: ~28,000 findings in the three surveyed repos). StyleBro reports missing documentation only
  where the fix is real: overrides and interface implementations get `/// <inheritdoc/>` (BRO1601). Everything else
  in this block corrects existing documentation. File headers (SA1633-SA1641) are the SDK's IDE0073.
- Done (one `DocumentationAnalyzer` + `DocumentationCodeFixProvider`, logic in `src/StyleBro.Analyzers/Documentation/`):
  - **BRO1601** (SA1600 subset) `/// <inheritdoc/>` on undocumented overrides/implementations (implicit and explicit,
    via `FindImplementationForInterfaceMember`), not in private types; inserted right before the member (above its
    attributes, below a plain comment). **BRO1602** (SA1626) `///` that documents nothing -> `//` (+ space if text
    follows, for BRO1002). Both work without doc generation (text-based).
  - **BRO1603** (SA1629) period at the end of summary/remarks/param/typeparam/returns/value/exception text; after an
    inline element; nested containers (`<remarks>` inside `<param>`, `<para>`, `<placeholder>`) judged by their own
    text; deviations: `?`, `!`, `:` endings are fine; content ending in `<inheritdoc/>`/`<include/>` isn't checked
    (OpenTelemetry uses `<param name="key"><inheritdoc cref=... path="/param"/></param>`: 72 false positives before).
  - **BRO1604/BRO1605** (SA1623/SA1624) property summary verb matches the accessors; private/internal setter =
    restricted, protected isn't (as StyleCop). Deviation: a bool summary may use the plain verb ("Gets the open
    state"); only summaries that already say "whether" get "a value indicating whether" (StyleCop's fix writes
    "Gets a value indicating whether gets the return value condition"). `init` properties and indexers skipped.
  - **BRO1606/BRO1607** (SA1642/SA1643) standard constructor/static constructor/finalizer sentence first (+ a space;
    StyleCop's fix has none), a near miss of it ("Initializes a new instance of the X class.") is replaced; private
    constructors may say "Prevents a default instance ..."; records skipped; members without a summary aren't reported.
  - **BRO1608** (SA1617) no `<returns>` on void methods/delegates; **BRO1609** (SA1651) `<placeholder>` unwrapped;
    **BRO1610** (SA1627) empty `<remarks>` removed; **BRO1611** (SA1612) stale `<param>` renamed (exactly one stale
    tag + one undocumented parameter) or removed, tags reordered to parameter order (only when each has its own lines).
  - Not implemented in StyleCop 1.2 (never report): SA1628, SA1644. Dropped (would need placeholder text): SA1602,
    SA1606, SA1609, SA1611, SA1614-SA1616, SA1618.
- XML-based rules (BRO1603-BRO1611) need `GenerateDocumentationFile` (the compiler only parses docs then), like
  StyleCop's SA0001. Messy and the parity projects enable it. Structured doc nodes on continuation lines include the
  `///` in their span: locate elements by their first token (`DocumentationTags.GetStart`).
- Parity sets `documentation-inherit`, `documentation`, `documentation-tags`, `documentation-params`. Enabling doc
  generation in the parity projects showed a StyleCop quirk: with docs parsed, SA1516 misses a member whose `///`
  directly follows the previous member (BRO1505 still reports it; documented in element-separation).

## BRO15xx: layout

- **BRO1501** (SA1509) blank line before `{` and **BRO1502** (SA1510) blank line before `else`/`catch`/`finally`.
  Same results as StyleCop, verified by running both on the same probe files (14 identical positions).
- "Blank lines directly above": only whole whitespace lines inside the token's leading trivia count, so string
  contents are never touched; the scan stops at a comment or directive line. The fix deletes those lines.
- StyleCop's exception, matched on purpose (user's decision): a `{` whose previous token is `}` is not reported
  (block after block, even with a comment in between). A standalone block after `;` IS reported, like SA1509.
  `do ... while`'s `while` is not a BRO1502 keyword.
- One analyzer (`Layout/BlankLineBeforeAnalyzer`, a single token pass) and one fix for both IDs.
- **BRO1503** (SA1505) blank lines after `{` (only whole lines up to the next token; skipped when a comment follows
  the brace on its line) and **BRO1504** (SA1515) blank line before a `//` comment that follows code. SA1515's
  exceptions, matched exactly: line above blank/comment/directive, directly after `{` or a `case`/`default` label
  (a plain `label:` IS reported), `///`/`////`, trailing comments. Both in `Layout/BlankLineAfterAnalyzer` +
  `BlankLineAfterCodeFixProvider`; parity set `blank-lines-comments`: 12/12 positions, identical output.
- **BRO1505** (SA1516) elements separated by a blank line (`Layout/ElementSeparation.cs`). Elements: usings/externs/
  assembly attributes/members of a file or namespace (file-scoped `namespace X;` counts before its first element),
  type members, accessors (only when either neighbour is multi-line). Exempt like StyleCop: field+field, using+using,
  extern+extern, attribute list+attribute list. A blank line ANYWHERE before the element's code counts (also below its
  comment or `#if`); the fix inserts above the element's first line (comment/doc/attribute/directive), diagnostic at
  that line's start (FullSpan.Start). Same positions as StyleCop (28/28); StyleCop's fix misses doc comments and
  mangles two-members-on-one-line.
- BRO1505 accessors: only two accessors that BOTH have block bodies, when either is multi-line (an expression-bodied
  `get => x;` next to a multi-line `set { }` is fine, like StyleCop; found via 26 false positives in OpenTelemetry).
- **BRO1506** (SA1512) no blank line below a `//` comment (not the file header, `///`/`////`, trailing comments, or
  when the next non-blank line is another comment) and **BRO1507** (SA1518, StyleCop's default "allow": at most one
  line break at the end; `insert_final_newline` covers require/omit). `Layout/TrailingBlankLines.cs`; parity set
  `comment-and-file-endings`: 10/10, identical output. Fixing BRO1506 and BRO1507 together must not overlap: blank
  lines below a comment at the end of the file are left to the ending removal. Two of three surveyed teams turn SA1512 off; it's in the
  preset like StyleCop's default, docs say how to turn it off.
- BRO1001 + blank lines: the sort never CREATES a BRO1505 (non-field below where two fields were) or BRO1504 (a `//`
  comment arriving below code) violation; it adds the blank line only then. Pre-existing ones are left to those rules,
  so compact interfaces stay compact.

## Real-world testing log

- **FFMpegCore** (open source, 6 projects, 180 files, no StyleCop; 2026-09-29):
  27 diagnostics in 26 files. One `dotnet format` run fixed all 26 files, each diff a pure line permutation;
  the solution built with TreatWarningsAsErrors, and `--verify-no-changes` passed. Diffs reviewed by hand: docs,
  attributes and the blank-line layout are handled correctly.
- **Bug (fixed): initializer order.** Sorting changed the order initializers run in:
  `static int A = 5; static readonly int B = A + 1;` was reordered so B became 1 instead of 6, with no warning.
  Now `Ordering/InitializerOrder.cs` (called from `MemberOrdering.GetKeys`, so analyzer and fix agree) skips a type
  when the sort would swap two static or two instance initializers and either one mentions a member of the type or
  creates an instance of it (syntax only, conservative; ignores `typeof`, `nameof`, `other.Name`). Covered by
  6 unit tests and `samples/Messy/Input/Initializers.cs`. FFMpegCore results unchanged (no new skips).
- **Gotcha:** `<WarningsNotAsErrors>BRO1001</WarningsNotAsErrors>` makes `dotnet format` skip the rule entirely
  (0 files formatted), while the build still reports it. `TreatWarningsAsErrors` alone is fine. Documented in BRO1001.md.
- **StyleCop comparison on a private app** (30 projects, ~6,500 tests, StyleCop.Analyzers 1.1.118 with defaults,
  no stylecop.json; 2026-09-29). The ordering rules SA1201-SA1204 and SA1214 were all enabled as warnings for the test.
  - Before: 1076 StyleCop ordering diagnostics (SA1201 839, SA1202 185, SA1204 50, SA1214 2), 877 BRO1001.
  - One `dotnet format` pass: 868 files, all line permutations, second run no changes, 27 s. Build 0 errors,
    all of the app's tests pass.
  - After: StyleCop quiet everywhere except two large files that use `#region`, which StyleBro skips on purpose.
    StyleBro flagged no file StyleCop was happy with.
  - Found and fixed: (1) default order had operators after methods; StyleCop wants conversion operators, then
    operators, then methods. Added the `conversion` kind. The full 14-kind order was checked against StyleCop
    with all 182 pairs (both orders), 0 differences. (2) The initializer guard treated constants as state and
    skipped a type for `Prop { get; set; } = SomeConst`; constants are now ignored.
  - Not covered by StyleBro (by design): namespace-level ordering of types (part of SA1201).
- **BRO1101/BRO1102** (2026-09-30): 0 findings in OpenTelemetry, Polly and the private app, which all keep SA1106
  and SA1133 on (so StyleBro is not stricter than StyleCop there; BRO1001 firing 674x in Polly confirmed the hook
  loaded). FFMpegCore and Serilog: 0 findings. Newtonsoft.Json: 66 findings (3 BRO1101, 63 BRO1102) in 4 files, all
  fixed in one pass, no new compile errors, second run clean, diffs reviewed by hand.
- **BRO1501/BRO1502** (2026-09-30): 0 in the three StyleCop repos. First version flagged block-after-block too
  (22 of 23 real findings); StyleCop doesn't, so that was fixed. Now: FFMpegCore 2, Serilog 1, Newtonsoft.Json 0,
  all standalone blocks after a statement; fixed, builds, second run clean.
- **BRO1103/BRO1104** (2026-09-30): 0 in Polly and OpenTelemetry. Private app: 1 BRO1104 on a target-typed
  `new()` (`DateTime X { get; set; } = new();`), which StyleCop 1.1.118 predates and misses; the 1.2 beta reports it.
  FFMpegCore 1, Serilog 6, Newtonsoft.Json 30. All fixed in one pass, builds, second run clean.
- **BRO1106/BRO1401 + all rules together** (2026-09-30). Found three problems, all fixed:
  1. Multi-targeting (Newtonsoft.Json, 8 target frameworks): the plain `FixAllProvider.Create` let `dotnet format`
     merge each framework's copy of a file, which wrote merge conflict markers into the source (84 compile errors);
     a first merge fix based on computed diffs duplicated members. Now `LinkedFileFixAllProvider` (design rule 1).
     StyleCop's own SA1413 fix produced no conflicts, which pointed at our Fix All implementation.
  2. BRO1401 inserted `,` directly before `}` ("x"},) -> SA1001 error in the private app's build. Now `, `.
  3. BRO1001 treated static constructors as private and moved them below public constructors (5 cases in
     OpenTelemetry's tests, which enforce SA1201-SA1204). StyleCop treats them as public; fixed.
  Final run, all rules: FFMpegCore 153, Polly 498, OpenTelemetry 0, private app 3688, Newtonsoft.Json 2037,
  Serilog 202 findings; all fixed in one pass, no new compile errors, no conflict markers, second run clean.
  Also: local clones in the scratchpad (under %TEMP%) keep getting damaged by a temp cleanup that deletes files with
  old timestamps. Hard-linked AND `--no-hardlinks` clones both copy git's object files with their old timestamps;
  clone local repos via `git clone file:///C:/dev/<repo>` instead, which writes fresh packs (network clones are fine). The same cleanup damages the test framework's reference-assembly cache in
  `%TEMP%\test-packages` (tests fail en masse with "package is missing the required nuspec file"): delete the
  damaged package folder under `%TEMP%\test-packages` and rerun.
- **BRO1002/BRO1105** (2026-09-30): Polly and OpenTelemetry 0. Private app (SA1005/SA1128 off there): BRO1105 408
  (= StyleCop's SA1128 count), BRO1002 77 (StyleCop 1.1.118: 78, see above). FFMpegCore 58, Newtonsoft.Json 257,
  Serilog 26. All fixed in one pass, builds, second run clean. Hooking analyzers into a repo
  without editing it: set the env var `CustomAfterMicrosoftCommonTargets` to a targets file with `<Analyzer>` items.
- **BRO1503-BRO1507 + all 16 rules together** (2026-09-30): every repo converges in one pass, no conflict markers.
  Private app: 9,701 findings; BRO1505/1506/1507 counts equal StyleCop's except 41 extra BRO1505 on file-scoped
  namespaces, which StyleCop 1.1.118 misses (the 1.2 beta reports them). OpenTelemetry: 4 findings left after the run,
  all in `#if NETFRAMEWORK` code: `dotnet format` doesn't process the net462 target on this machine, so those lines
  are never analyzed or fixed. An environment limitation, not a StyleBro bug.
- **BRO1107/BRO1108** (2026-09-30): OpenTelemetry 0 (enforces SA1116/SA1117). FFMpegCore 89, Polly 741, private app
  613, Newtonsoft.Json 1602, Serilog 118. All fixed in one pass (20/109/150/143/23 files), no new compile errors,
  second run clean. Items containing `#if` (Newtonsoft.Json) are fine: only the gaps between items are rewritten.
- **BRO1301/BRO1302** (2026-09-30): Polly, OpenTelemetry, Serilog 0. FFMpegCore 8, private app 7, Newtonsoft.Json 14
  (13 constructor/factory parameters of serialization test types). All fixed in one pass, no new compile errors,
  second run clean. Parameter names can matter at runtime (serializers bind constructor parameters by name,
  case-insensitively in Newtonsoft.Json and System.Text.Json): Newtonsoft.Json's related tests (156) pass after the fix.
- **BRO1303, default `camelCase`** (2026-09-30), the biggest rename test so far: FFMpegCore 65, Polly 463, private
  app 1041, Newtonsoft.Json 356, Serilog 193, OpenTelemetry 0 (already camelCase). All compile after one pass
  (Polly 242 references qualified with `this.`). Compiling is not enough for renames: run the repos' tests.
  Polly's tests found the reflection case (see the guard above); with it, Polly 3065/3065 and the private app
  6454/6454 tests pass, 5 + 2 fields keep their names with the warning. Polly's tests use Microsoft Testing
  Platform: `dotnet test` there reports "Zero tests ran" (exit 5); run the test executables in artifacts/bin instead.
  Newtonsoft.Json: its MemberSearchFlags test (serializes private fields, reads `_privateString` via GetField) failed
  on the pre-guard run and passes with the guard (7 fields kept); 3613/3617 pass, the 4 failures (Issue2768, decimal
  parsing) fail on the untouched code too (machine locale). Serilog: all tests pass on every target framework.
- **BRO1303 with `_camelCase`** (via a global config): FFMpegCore 1, Polly 1, private app 32 (fields that break the
  repo's own `_field` convention); all fixed, compile, the private app's tests pass. Gotcha for the real-world hook:
  `GlobalAnalyzerConfigFiles` added from `CustomAfterMicrosoftCommonTargets` is too late (the SDK has already turned
  them into `EditorConfigFiles`); add `EditorConfigFiles` directly. The NuGet package isn't affected (its targets are
  imported earlier).
- **BRO1304/BRO1305** (2026-09-30): 0 everywhere except Newtonsoft.Json (14 single-letter type parameters,
  `U` -> `TU`); fixed in one pass, compiles, second run clean.
- **BRO1306** (2026-09-30): FFMpegCore 7, private app 15 (5 kept by the string guard), Newtonsoft.Json 25 (14 kept,
  mostly public test-object fields named in JSON strings), Serilog 4, Polly and OpenTelemetry 0. All compile; tests
  pass (Newtonsoft.Json 3613/3617 with the same 4 locale failures as untouched code, private app all, Serilog all
  including its public API approval test).
- **BRO1307-BRO1309 + all nine naming rules together** (2026-09-30): OpenTelemetry has 203 protobuf-mirroring
  constants (`AnyValue_String_Value`, BRO1308), Newtonsoft.Json showed the `Int32_0` -> `Int320` bug (fixed: skip
  digit_digit) and, through its tests, the serializer-attribute and ShouldSerialize cases (fixed: guards above).
  Final run of BRO1301-BRO1309: every repo compiles; Polly 3065/3065, private app all, Serilog all, Newtonsoft.Json
  3613/3617 (the 4 locale failures). The string guard for private fields was narrowed afterwards (see above); rerun
  the naming rules with the repos' tests before the next release.
- **BRO1109-BRO1113** (2026-09-30): FFMpegCore 15, Polly 411 (BRO1112 264 = its SA1124 count, BRO1113 147),
  OpenTelemetry 0, private app 1146 (BRO1110 1044, BRO1111 82), Newtonsoft.Json 1415 (1102 regions, incl. the
  `#region License` around every file header; the BOM stays), Serilog 5. All fixed in one pass, no new compile
  errors, second run clean. Open question: the private app has 1044 BRO1110 but StyleCop 1.1.118 counted 740 SA1111
  there, while the parity set (1.2 beta) is identical; likely a version difference, not yet checked.
- **BRO1601-BRO1611** (2026-10-01): FFMpegCore 154, Polly 256, OpenTelemetry 400, private app 2920 (almost all
  BRO1601 `<inheritdoc/>`), Newtonsoft.Json 1186, Serilog 404. All fixed in one pass, second run clean, no new
  compile errors (doc edits only). Found and fixed on the way: BRO1603 put a period after nested `</remarks>` (Polly,
  103) and flagged inherited text (OpenTelemetry, 72); BRO1604 turned bool summaries into non-sentences (Polly
  polyfills, which suppress StyleCop with #pragma). Open question: Polly enforces SA1612 but has 22 out-of-order
  `<param>` tags that BRO1611 reports (their texts are swapped too, so the names were mixed up; the fix only
  reorders); why StyleCop stays quiet there wasn't checked.

## Known open questions

- Answered: `dotnet format` does pick up code fixes from analyzers referenced as
  `ProjectReference OutputItemType="Analyzer"` (as in `samples/Messy`).
- Answered: help links and `PackageProjectUrl` point to `github.com/bisforboman/stylebro`.
- Name checked 2026-09-29: NuGet IDs `StyleBro` and `StyleBro.Analyzers` are free (0 search hits);
  a `stylebro` GitHub user/org is free; no similar C# projects on GitHub. `StyleBro.Analyzers` is now taken by us.

## Next steps (in order)

1. ~~Make everything build and pass (tests + verify-format).~~ Done, re-verified after the rename.
2. Real-world testing: run `dotnet format analyzers --diagnostics BRO1001` on some real repos and check they
   still build and the second run changes nothing. Compare with StyleCop's SA1201-SA1204 on a repo that uses
   StyleCop; differences are bugs or deliberate, documented choices.
3. ~~Check the name, create the repo, publish.~~ Done: public at `github.com/bisforboman/stylebro` (MIT);
   `StyleBro.Analyzers 0.1.0-alpha.1` published to nuget.org on 2026-09-30 by release.yml (tag at `bc053c2`).
   To release: bump nothing in the csproj, just push a tag `vX.Y.Z[-suffix]` and approve the `release` environment.
4. Scope = migration parity, not "port every StyleCop rule". `docs/stylecop-mapping.md` (draft, 2026-09-30) maps
   all 197 StyleCop diagnostics to SDK / StyleBro / drop, from: the rule inventory read from the StyleCop DLLs
   (1.1.118 + 1.2.0-beta.556), config surveys and all-rules-on counts in OpenTelemetry, Polly and the private app,
   and an SDK check (75 rules: 49 fixed by `dotnet format` with SDK settings alone). Key findings:
   - `dotnet format` cannot fix naming (IDE1006: "doesn't support Fix All"), so a StyleBro rename fix is a real gap.
   - Done: SA1133 (BRO1102), SA1106 (BRO1101), SA1509/SA1510 (BRO1501/BRO1502), SA1131 (BRO1103), SA1129
     (BRO1104), SA1128 (BRO1105), SA1005 (BRO1002), SA1413 (BRO1401), SA1122 (BRO1106), SA1505 (BRO1503),
     SA1515 (BRO1504), SA1516 (BRO1505), SA1512 (BRO1506), SA1518 (BRO1507), SA1116/SA1117
     (BRO1107/BRO1108), SA1312/SA1313 (BRO1301/BRO1302), SA1306/SA1309 for private fields (BRO1303), SA1302/SA1314
     (BRO1304/BRO1305), SA1303/SA1304/SA1307/SA1311 (BRO1306), SA1308/SA1310/SA1300 (BRO1307-BRO1309),
     SA1110/SA1111/SA1127/SA1124/SA1123 (BRO1109-BRO1113), documentation rules (BRO1601-BRO1611, see BRO16xx).
     Left: SA1305 (Hungarian, no StyleCop fix), SA1613 (unnamed `<param>`), and the untested rest (77).
   - 107 rules untested yet, mostly documentation (SA16xx).
   - The preset only claims IDE0011 + IDE0055 today; the SDK settings verified in the check should go into it.
     Some are opinionated (SA1101 `this.` is off in 2 of 3 repos), so decide per setting.
5. Documentation rules (BRO16xx): XML doc stubs, `<inheritdoc/>` on overrides/interface implementations,
   `<param>` kept in sync with the parameters. These need the semantic model.
6. StyleCop migration tool: `stylecop.json` + rulesets -> equivalent `.editorconfig` (the mapping is its spec).
7. Later: blank-line layout rules (the SDK's IDE2000 series is only experimental), baseline support
   (fail only on new violations).
