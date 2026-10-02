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
(see the log below) and 71 rules; 388 unit tests (incl. every doc example), all green (2026-10-02). Migration tool
`stylebro-migrate` added (2026-10-01, see below).
0.1.0-alpha.7 (71 rules, 2026-10-02) is the latest on nuget.org with StyleBro.Migrate; earlier: alpha.6 (54 rules,
the first prerelease published without an approval step), alpha.5 (43 rules + the tool), alpha.4 (44 rules), alpha.3 (18 rules). From alpha.5 on, release.yml also packs `stylebro-migrate` (package StyleBro.Migrate, a .NET
tool) with the same version; CI packs both too.

**Release policy (user's decision, 2026-09-30):** don't publish a version for every batch of rules while the project
is starting out. Keep adding rules on `main` and release again once there's a critical mass of new rules. Don't
suggest or push a release tag after each batch; mention it only when a release looks worth it.

## Layout

- `src/StyleBro.Analyzers`: netstandard2.0, references Microsoft.CodeAnalysis.CSharp only.
  Must never reference Workspaces (RS1038).
- `src/StyleBro.CodeFixes`: netstandard2.0, Workspaces, code fixes + Fix All providers. References Analyzers.
- `src/StyleBro.Package`: packs both DLLs into `analyzers/dotnet/cs`, plus `build/StyleBro.Analyzers.targets`
  that adds `stylebro.recommended.globalconfig` (global_level -1; opt out with `<StyleBroPreset>none</StyleBroPreset>`).
- `src/StyleBro.Migrate`: net8.0 console tool `stylebro-migrate` (PackAsTool, published from alpha.5 with the analyzers;
  its package README is `src/StyleBro.Migrate/README.md`), see "Migration tool".
  References StyleBro.Analyzers (reads every rule's "Replaces StyleCop SAxxxx" from its description) and embeds
  `scripts/stylecop-survey/data/inventory-1.2.0-beta.556.csv` and `data/mapping.csv` (written by New-Mapping.ps1).
- `tests/StyleBro.Tests`: xunit + Microsoft.CodeAnalysis.CSharp.CodeFix.Testing 1.1.2 (`DefaultVerifier`).
- `samples/Messy`: dotnet format integration sample. `Input/*.cs` is copied to `Generated/` (gitignored),
  formatted, compared with `Expected/`, then a second run with `--verify-no-changes` must pass.
- Central package management in `Directory.Packages.props`. Roslyn pinned to **4.8.0** for broad SDK/VS
  compatibility; don't raise it without a reason.
- `scripts/stylecop-survey`: generates `docs/stylecop-mapping.md` and `docs/skipped-rules.md` (inventory of StyleCop rules, repo surveys,
  SDK coverage check). Hand-written decisions in `decisions.psd1`, measured data in `data/`. Not part of the slnx.
  **Every StyleCop rule StyleBro doesn't cover has an entry in `skipped.psd1`** (status Candidate / SdkLater / Drop /
  NotInStyleCop / Variant / NotApplicable, why, what revisiting would take), rendered as `docs/skipped-rules.md`
  (user's request, 2026-10-01: "so they can easily be revisited later"). When a rule gets implemented, move it to
  `decisions.psd1`; when a new reason to skip one turns up, write it there. New-Mapping warns about missing entries.
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
  requires the owner's approval. Since 2026-10-01 (user's decision: "prereleases auto"): tags with a `-` use the
  `prerelease` environment instead, which has no reviewers and only accepts tags matching `v*-*` (deployment tag
  policy), so alphas publish as soon as the tag is pushed and stable tags still wait for approval. It needs its own
  nuget.org Trusted Publishing policy (same repo/workflow, environment `prerelease`), added by the owner on nuget.org. The first failure (HTTP 401
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
   Not reported / Compared with StyleCop. Every rule is also listed in `docs/differences-from-stylecop.md` (under
   "Same as StyleCop" if nothing differs), and every preset value that differs from StyleCop's defaults is explained
   there (user's request, 2026-10-01: "so it's obvious later"); `MigrationTests` enforces both. `DocExamplesTests` runs every example (Before compiles and gets the
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
- **BRO1112** (SA1124, regions between members/types; OFF in the preset although StyleCop has SA1124 ON by default (both inventories; the earlier "like StyleCop" was wrong)) and **BRO1113**
  (SA1123, regions inside a `{ }` block; a region in an expression body, e.g. between switch-expression arms after
  `=>`, is SA1124 in StyleCop 1.2, probed; the private app had one, StyleCop quiet because SA1124 is off there): the fix removes each `#region`/`#endregion` line pair, one edit per run of
  removed lines, and drops a neighboring blank line that would double a blank line, follow `{` or precede `}`. A run
  at the end of the file takes the preceding line break along. Removing regions lets BRO1001 see the type, so a
  region-heavy codebase needs a second `dotnet format` run (documented in BRO1112.md).
- **BRO1003/BRO1004** (SA1212/SA1213, `Ordering/AccessorOrder.cs`): swap the two accessors; one-line lists swap the
  accessor text, otherwise "slots" (comments above + the accessor + a trailing comment) so blank lines stay put;
  auto-properties `{ set; get; }` count like in StyleCop. **BRO1114** (SA1132, `Readability/CombinedFields.cs`): one
  declaration per field/event field (locals not reported, like StyleCop); every copy gets the attributes, modifiers,
  type and doc comment (StyleCop's fix drops the attributes from all but the first: a real semantic change); a line
  break between type and first name is joined (Serilog's `const string` + one constant per line); event fields get
  BRO1505's blank line. **BRO1115** (SA1125, `Readability/NullableShorthand.cs`, semantic): only
  `System.Nullable<T>`, only where `SyntaxFacts.IsInTypeOnlyContext` (that alone leaves out is/as, patterns, using
  aliases, crefs, nameof, `Nullable<int>.Equals`; explicit checks were dead code per mutation testing); nested ones
  in one edit. **BRO1402** (SA1411, `Maintainability/EmptyAttributeParentheses.cs`): removes `()` incl. `( )`. Parity
  set `accessors-fields-attributes`: 24/24 positions, output differences documented.
- Parity: `parenthesis-placement` 13/13 and `constraints-regions` 15/15 positions, identical fixed output.
- **StyleCop parity check:** `scripts/stylecop-survey/Compare-WithStyleCop.ps1` + `parity/parity.psd1`. Runs both
  tools on edge-case files, compares positions and fixed output; every difference must be a documented deviation.
  StyleBro's fixed output must also be clean ("StyleBro fix leaves: ..." otherwise; this found a BRO1506 bug:
  the empty line after a file's final line break counted as a blank line below a comment). `CompareOutput = $false`
  skips the output diff when StyleCop has no fix (SA1117) or a broken one (SA1312/SA1313: under `dotnet format`
  its rename fix applies a different subset of renames per run). All 29 sets pass. Add a set for every new rule.
  Sets may map several SA ids to one BRO id (StyleCop's reports at one position count once) and set `StyleCopJson`/
  `EditorConfig` (file-header).
- Tests use the generic `Verifier<TAnalyzer, TCodeFix>`; each skip condition was checked by disabling it and
  confirming a test fails.

- **Batch 3, semantic readability** (2026-10-02; StyleCop's source read from GitHub for each rule and fix):
  **BRO1122** (SA1139, `Readability/LiteralSuffixes.cs`) `(long)1` -> `1L` only when the new literal's VALUE equals the
  cast's constant (StyleCop checks the type only: `(decimal)0.1234567890123456789` rounds through double, `(decimal)1.50`
  loses its scale (compare `decimal.GetBits`), `(float)1.00000005960464488641292746251565404236316680908203125` double-
  rounds); skips `a-(long)-1` (would be `a--1L`) and comments. **BRO1123** (SA1141, `TupleSyntax.cs`) every type-only
  position (StyleCop: declarations only), outermost ValueTuple per edit; creations/`ValueTuple.Create` -> literal only
  when no argument would infer an element name (else BRO1124 changes code on a second run), args == arity (StyleCop's
  fix writes `()` for `new ValueTuple<int, int>()`), not in expression trees; casts only on literal args.
  **BRO1124** (SA1142, `TupleElementNames.cs`) `t.Item1` -> name; not in `nameof`; after `?.` reported on `.Item1`
  like StyleCop. **BRO1125** (SA1130, `LambdaSyntax.cs`) StyleCop's output (types dropped, `delegate { }` gets the
  delegate's names made unique against scope AND identifiers in the body), checked by `Speculation.BindsTheSame`
  (speculative model on the enclosing statement/initializer/arrow clause: same converted type, same invoked symbol);
  that also rejects `var f = delegate (int x) {...}`. **BRO1403** (SA1410) only where BRO1125 is off or skips the
  method (`Severities.IsOn`, extracted from `SingleLineBlocks.WantsTrailingComma`), so the two fixes never overlap.
  **BRO1126** (SA1135, `QualifiedUsings.cs`) also skips a qualified name whose root an enclosing namespace hides.
  Under `dotnet format`, StyleCop's SA1141 fix has no Fix All and its SA1142 fix throws (checked in the log this time).
  Parity `readability-semantics`: 38/37 positions, 44 documented differences. Mutation-tested: every guard has a test
  (the value check needed the double-rounding float case; an explicit `global::` check was dead, `GetAliasInfo` covers it).

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
  in this block corrects existing documentation. Plain file headers (SA1633, `xmlHeader: false`) are the SDK's IDE0073;
  the XML header is BRO1615.
- Done (one `DocumentationAnalyzer` + `DocumentationCodeFixProvider`, logic in `src/StyleBro.Analyzers/Documentation/`):
  - **BRO1601** (SA1600 subset) `/// <inheritdoc/>` on undocumented overrides/implementations (implicit and explicit,
    via `FindImplementationForInterfaceMember`); which members by effective accessibility, like stylecop.json:
    `stylebro_document_exposed_elements`/`_internal_elements`/`_private_elements` (true/true/false; Polly sets
    documentInternalElements false, and BRO1601 added 93 inheritdocs to internal classes before); inserted right before the member (above its
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
    Like StyleCop only the BEGINNING counts (no period needed: "... class representing X." is fine; the first version
    replaced such sentences, losing text, in Polly), crefs compare without spaces (`{TResult, TArgs}`), and the
    near-miss replacement only applies to a sentence ending in class/struct/type; anything else is kept after it.
  - **BRO1608** (SA1617) no `<returns>` on void methods/delegates; **BRO1609** (SA1651) `<placeholder>` unwrapped;
    **BRO1610** (SA1627) empty `<remarks>` removed; **BRO1611** (SA1612) stale `<param>` renamed (exactly one stale
    tag + one undocumented parameter) or removed, tags reordered to parameter order (only when each has its own lines).
    Methods, indexers, delegates only: StyleCop 1.2's SA1612 doesn't check constructors or operators (probed: neither
    stale nor out-of-order tags; this was the Polly open question, 22 out-of-order constructor tags).
  - **BRO1612** (SA1613) / **BRO1614** (SA1621) unnamed `<param>`/`<typeparam>` tags, **BRO1613** (SA1620)
    `<typeparam>` tags match the type parameters (methods, delegates, classes/structs/interfaces/records). One
    generalized `ParameterDocumentation.GetFinding(member, text, TagKind)`: all problems of a member's tags of one kind
    are found and fixed together (every rule's fix gives the same edits). An unnamed tag is named only when certain
    (one unnamed + one undocumented, or all unnamed with one per (type) parameter, in order; `name=""` counts as
    unnamed); otherwise not reported (StyleCop reports all, no fix). Parity `documentation-typeparams`: 15/15.
  - Not implemented in StyleCop 1.2 (never report): SA1628, SA1644. Dropped (would need placeholder text): SA1602,
    SA1606, SA1609, SA1611, SA1614-SA1616, SA1618.
- **BRO1615** (SA1633 with the XML header, SA1634-SA1638, SA1640, SA1641; 2026-10-02; `Documentation/FileHeaders.cs`,
  `FileHeaderAnalyzer`, `FileHeaderCodeFixProvider`): one rule, one diagnostic per file. Does nothing until
  `stylebro_file_header_company` is set (preset: warning; StyleCop's default company is `PlaceholderCompany`).
  `stylebro_file_header_copyright` (default StyleCop's text; `\n`, `{companyName}`, `{fileName}`),
  `stylebro_file_header_decoration`. Header read exactly like StyleCop's `FileHeaderHelpers` (source read from GitHub:
  `//` comments up to a blank line, `//-` borders skipped, `<root>`-wrapped XML, no element = malformed). Text compared
  line by line, trimmed; file name ordinal. Fix: rewrites only the `<copyright>` tag's lines; no tag -> inserted at the
  header's first non-border line; plain header equal to the copyright text -> replaced; any other plain comment -> new
  header above + blank line (StyleCop's fix DELETES it). Skipped: `/* */` headers, a malformed header containing
  `<copyright` (a person repairs it), a tag sharing its first/last line with other text, whitespace-only files.
  Missing header reported at the first token (StyleCop: 0,0), so a `#pragma warning disable` at the top suppresses it
  (the test framework checks exactly that). Parity `file-header`: positions and fixed output identical to StyleCop's
  apart from the documented skips/kept comments. Migration: on when xmlHeader (default true) and all eight SA rules are
  on; writes company/copyright (custom `variables` expanded; also for IDE0073's template now)/decoration. Doc examples
  can now carry an ```ini block (between `## Example` and `### Before`) used as their .editorconfig.
  GOTCHA from probing: `dotnet format` printed "Unable to fix SA1633. Code fix SettingsFileCodeFixProvider doesn't
  support Fix All" but StyleCop's FileHeaderCodeFixProvider still ran; a backup copy of the probe files INSIDE the
  probe project got rewritten too and hid it. Keep probe backups outside the project folder.
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
- **BRO1508** (SA1501, statement blocks) and **BRO1509** (SA1502: types, namespaces, method/ctor/operator/local
  function bodies, accessor lists with a block-bodied accessor) on a single line (`Layout/SingleLineBlocks.cs`, one
  analyzer + one fix). Probed with StyleCop 1.2: empty braces count; lambda/anonymous-method bodies, `{ get; set; }`,
  `{ get => x; }` and single-line accessors inside a multi-line property don't; nested blocks are all reported; a local
  function gets SA1501 AND SA1502 (we report BRO1509 once). The fix rewrites only whitespace GAPS (before `{`, after
  `{`, between items, before `}`, before a following else/catch/finally), each computed assuming every enclosing
  single-line block is expanded too (`StartIndent` walks up), so nested blocks give disjoint edits and single fix ==
  Fix All. Respects `csharp_new_line_before_open_brace` (per kind) and `_else/_catch/_finally`; keeps `} while (x);`
  and the file's line endings; enum members stay on one line like StyleCop. Skipped: a comment in a gap, a block whose
  owner shares its line with code that stays (`switch (x) { case 1: { ... } }`). Parity set `single-line-blocks`:
  56/52 positions, 4 documented; output not compared (StyleCop's fix misindents nested blocks, writes CRLF into LF
  files). Replaces the SDK option the preset couldn't use (`csharp_preserve_single_line_blocks = false` also expands
  `{ get; set; }`).
- **BRO1510** (SA1504) accessors with block bodies all single-line or all multi-line (`Layout/AccessorLayout.cs`).
  Probed: only lists where EVERY accessor has a block body (`get => x;`/`get;` next to a multi-line `set {}` is fine),
  only multi-line lists (a one-line list is BRO1509's), diagnostic on the first accessor's keyword. StyleCop's fix has
  "single line" and "multiple lines" actions and under Fix All applies one of them to the whole project (whichever
  the first diagnostic offers), and drops comments in a body it collapses. Ours decides per list: collapse when every
  multi-line accessor has <= 1 single-line statement and no comments, else expand the single-line ones; adds the
  BRO1505 blank line between accessors when BRO1505 wants it on the original (order-independence). Parity set
  `accessor-layout`: 8/8 positions, output not compared.
- **Batch 2** (2026-10-01). Lists (`Readability/ListGaps.cs`, same list kinds as BRO1107): **BRO1116** (SA1112) `()`
  not split, not attributes (like StyleCop); **BRO1117** (SA1113) comma ends the previous item's line (its edit also
  drops blank lines before the item, else BRO1119 needs a 2nd run); **BRO1118/BRO1119** (SA1114/SA1115, no StyleCop
  fix) blank lines after `(`/after a comma removed (only whole blank lines, so a comment on its own line blocks it).
  Comments (`Readability/CommentText.cs`): **BRO1005** (SA1004) a missing space after `///` anywhere, but several
  spaces only before a TOP-LEVEL tag (`///   <param>`); indented text/nested tags and `<code>` are fine (probed; the
  first version flagged 367 lines in Polly, which enforces SA1004);
  **BRO1120** (SA1120) empty `//`/`/* */` reported at the start/end of a group of consecutive comment lines (a comment
  after code starts a group; `////` isn't empty), the fix removes the whole empty run at that end (StyleCop one per
  run). **BRO1121** (SA1136, `Readability/EnumValueLines.cs`) one enum value per line; a one-line enum gets BRO1509's
  expansion, and BRO1509 now puts every enum value on its own line (StyleCop's SA1502 fix leaves `A, B`, which its
  SA1136 then reports). `Layout/DocumentationBlankLines.cs`: **BRO1511** (SA1506) blank lines between docs and element,
  also between two `///` blocks; **BRO1512** (SA1511) blank before do-while's `while`; **BRO1513** (SA1514) blank line
  before docs, not after `{`/file start, not after `#if`/`#else`/`#pragma` (but after `#endif`/`#region`/`#endregion`;
  probed after 66 false positives in Polly), and NOT below a `//` comment (StyleCop reports it, but its SA1512 forbids
  the blank line: the fixes would undo each other). GOTCHA: a doc comment trivia's `Span` starts AFTER its first
  `///`; use `FullSpan.Start` (BRO1513 reported nothing until that was found). Parity set `lists-comments-docs`:
  34/32 positions, 19 documented differences.
- **Fix ORDER under `dotnet format` is not fixed** (found with BRO1509 in the Messy sample: 4 of 5 runs failed). Two
  interactions had to be made order-independent: (1) BRO1505's fix on members of a ONE-LINE type now applies the
  whole BRO1509 expansion (identical edits, merged), instead of splitting one gap at column 0, which left the braces
  on the line and BRO1509 blind; BRO1509's expansion adds the blank lines BRO1505 wants. (2) BRO1509 adds the
  trailing comma BRO1401 wants on an expanded enum, unless BRO1401 is off. Severity keys (`dotnet_diagnostic.X.severity`)
  are NOT in AnalyzerConfigOptions; read them from `CompilationOptions.SyntaxTreeOptionsProvider`
  (`SingleLineBlocks.WantsTrailingComma`). Lesson: a fix that changes a node's shape (single-line -> multi-line) must
  also produce what other rules want for the new shape. Run verify-format several times when adding a layout rule.
- BRO1001 + blank lines: the sort never CREATES a BRO1505 (non-field below where two fields were) or BRO1504 (a `//`
  comment arriving below code) violation; it adds the blank line only then. Pre-existing ones are left to those rules,
  so compact interfaces stay compact.

## Migration tool (`stylebro-migrate`, 2026-10-01)

- User's decision: porting = "generate config from theirs". The preset stays the default for new projects; the tool
  reads a repo's StyleCop setup and writes matching settings, so a StyleCop-clean repo stays (almost) unchanged.
  Usage and rules: `docs/migrating.md`. Code: `StyleCopSetup.cs` (reading), `Migration.cs` (mapping, plan, render),
  `Suppressions.cs`, `Program.cs` (report). Tests: `MigrationTests.cs`.
- Reading: defaults (inventory) <- rulesets <- globalconfigs (strictest wins within a kind) <- root .editorconfig
  sections for all C#. Sub-directory .editorconfigs and path sections are *scopes*: each gets, in its own file and
  section, only the generated lines that differ from the main block.
- BRO rules: on only when ALL replaced SA rules are on (weakest severity); stylecop.json can make some moot
  (elementOrder without constant/static/readonly -> SA1203/SA1204/SA1214; `_camelCase` -> SA1309). The first version
  used the strongest, which turned on BRO1001's full sort in Polly (SA1201/SA1202 off) and changed 244 files.
- SDK rules: strongest severity of the covered SA rules, options from stylecop.json; keys the root .editorconfig
  already sets for C# are left out (OpenTelemetry sets `csharp_preserve_single_line_statements = true`; overriding
  it split `case X: a(); break;` lines).
- Suppressions carried over with `--write`: `#pragma warning disable SA...` (+ BRO/IDE ids), `[SuppressMessage]`
  (a sibling attribute per replacing id, own attribute list so BRO1102 stays quiet), `<NoWarn>` in
  .csproj/.props/.targets (Polly's Snippets project has `SA1123` in NoWarn: its `#region`s are doc snippets, and
  BRO1113 removed 147 of them before). The StyleCop ids stay. Idempotent.
- `--write` turns the preset off (`<StyleBroPreset>none</StyleBroPreset>` in the root Directory.Build.props) and the
  block writes every preset key (test `TheGeneratedSettings_CoverEveryPresetKey`). Reason, probed: `dotnet format`
  sorts usings whenever `dotnet_sort_system_directives_first` is SET (any value, even `false`; `unset` doesn't help),
  and an .editorconfig can't remove a key the preset's global config sets. The private app has SA1208/SA1210 off and
  unsorted usings: the block writing `true` changed ~95 files, `false` ~1,650. Now the sort keys are written only
  when SA1208 or SA1210 is on.
- SX ids are read too (the reader used to accept only SA ids, so SX settings were silently ignored): SX1101 -> IDE0003,
  SX1309 -> `_camelCase`; SA1412 on -> `charset = utf-8-bom`.
- Also read: the StyleCop.Analyzers version (1.1.x: SA1141/SA1142/SA1316/SA1414 off) and whether any project sets
  GenerateDocumentationFile. Without it StyleCop's XML doc rules never run in the build (SA0001), but `dotnet format`
  parses docs anyway, so BRO1603-BRO1611 stay off (the private app: 5 `<placeholder>` unwraps before).
- Report: rules on but not enforced afterwards, grouped: partly covered / not expressible (with the reason),
  dropped by design (from mapping.csv), not covered yet (rule titles).
- Found on the way, fixed in the preset too: `csharp_preserve_single_line_blocks = false` expands every
  `{ get; set; }` (StyleCop allows them), so it's `true` and SA1501/SA1502 aren't SDK-covered (mapping: SDK 49 -> 47);
  IDE0040 `always` adds `public` to interface members, SA1400 doesn't ask for that: `for_non_interface_members`.
  Also: StyleCop has SA1124 ON by default (both DLL inventories); BRO1112 stays off in the preset (opt-in, large
  one-time change), the docs no longer claim it matches StyleCop.
- Measuring (`scripts/stylecop-survey/Measure-MigrationDelta.ps1`, needs `-ResetTargetRepo` on a throwaway clone): plain `dotnet format` committed as baseline (it has its own noise:
  conflict markers in multi-targeted files, line endings), then migrate --write, then `dotnet format` with StyleBro
  and the preset; the remaining diff is StyleBro's. The hook uses COPIES of the DLLs (in %TEMP%), otherwise the run
  locks the build output, and it drops StyleCop.Analyzers' analyzers (a real migration removes the package; its SA1651
  fix ran under `dotnet format`, which parses docs even where the build doesn't).
- Results (2026-10-01), files StyleBro changes on StyleCop-clean repos after migrating: Polly 244 -> 3 (2 IDE0047 lines
  removing parentheses in `a ?? (b ?? c)`, which SA1119 accepts; 1 file plain `dotnet format` had already broken with
  conflict markers), OpenTelemetry 22 -> 1 (a line ending in a mixed-EOL file), private app 1,659 (mid-way) -> 10
  (9 IDE2000 blank lines at a file's start / before `}`, which SA1507 leaves to SA1517/SA1508, both off there; 1
  BRO1104 on target-typed `new()`, which StyleCop 1.1.118 misses). The IDE0047/IDE2000 differences are documented
  in docs/migrating.md; the SDK rules can't be narrowed.
- StyleBro bugs found this way (fixed): BRO1606 replaced "Initializes ... class representing X." (text lost); BRO1606
  flagged `{TResult, TArgs}` crefs; BRO1601 ignored documentInternalElements; BRO1611 checked constructors/operators
  (StyleCop doesn't); BRO1113 took regions in expression bodies (StyleCop: SA1124, so BRO1112).

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
  3613/3617 (the 4 locale failures). The string guard for private fields was narrowed afterwards (see above).
- **Naming rerun with the narrowed guard** (2026-10-01, after alpha.4 had shipped without it): BRO1301-BRO1309 on all
  repos, default `camelCase`. FFMpegCore 80, Polly 463, OpenTelemetry 203, private app 1070, Newtonsoft.Json 455,
  Serilog 198 renames; all compile; left with the warning on purpose: Polly 5, private app 7, Newtonsoft.Json 49
  (guards). Tests: Polly all (4 frameworks), private app 6454/6454, Serilog all (5 frameworks, API approval
  included), Newtonsoft.Json 3613/3617 (the 4 locale failures), FFMpegCore 204/209 with the same 4 cancellation-test
  failures on the untouched code (machine timing/ffmpeg, checked with git stash), OpenTelemetry (first time with its tests): the only failures (Prometheus HTTP listener, net472 serializer and
  self-diagnostics tests) are identical on the untouched code (machine environment).
  Humanizer (not a reference repo) doesn't build here, so it says nothing.
- **BRO1109-BRO1113** (2026-09-30): FFMpegCore 15, Polly 411 (BRO1112 264 = its SA1124 count, BRO1113 147),
  OpenTelemetry 0, private app 1146 (BRO1110 1044, BRO1111 82), Newtonsoft.Json 1415 (1102 regions, incl. the
  `#region License` around every file header; the BOM stays), Serilog 5. All fixed in one pass, no new compile
  errors, second run clean. Answered 2026-10-02: the private app's 1044 BRO1110 vs 740 SA1111 (1.1.118) are 739 shared +
  305 extra (297 target-typed `new(...)`, 8 `: this(...)`/`: base(...)`), which StyleCop 1.2 doesn't check either
  (added to the parity set); kept as a documented "reports more", like BRO1107/BRO1108's records. 1 StyleCop-only:
  a trailing comment before `);` (skipped on purpose).
- **BRO1601-BRO1611** (2026-10-01): FFMpegCore 154, Polly 256, OpenTelemetry 400, private app 2920 (almost all
  BRO1601 `<inheritdoc/>`), Newtonsoft.Json 1186, Serilog 404. All fixed in one pass, second run clean, no new
  compile errors (doc edits only). Found and fixed on the way: BRO1603 put a period after nested `</remarks>` (Polly,
  103) and flagged inherited text (OpenTelemetry, 72); BRO1604 turned bool summaries into non-sentences (Polly
  polyfills, which suppress StyleCop with #pragma). Polly's 22 out-of-order `<param>` tags under SA1612: all on
  constructors, which StyleCop doesn't check (answered 2026-10-01; BRO1611 now skips constructors and operators).

- **BRO1508/BRO1509 + all 46 rules together** (2026-10-01): BRO1508/BRO1509 found Polly 0 and OpenTelemetry 0 (both
  enforce SA1501/SA1502), private app 1 + 356 (= StyleCop's SA1501/SA1502 counts there exactly), Newtonsoft.Json 57,
  Serilog 4, FFMpegCore 22. Totals: FFMpegCore 656, Polly 2477, OpenTelemetry 682, private app 15807, Newtonsoft.Json
  8369, Serilog 1021 findings; no new compile errors anywhere, no conflict markers (Newtonsoft.Json, 8 frameworks).
  Second run clean for OpenTelemetry and Serilog; the leftovers elsewhere are all known: rename guards (Polly 5,
  private app 7, Newtonsoft.Json 49) and BRO1001 in files whose `#region`s BRO1112 removed in the same run
  (FFMpegCore 2, Polly 10, private app 2), which BRO1001 can only sort on a second run.

- **BRO1510, BRO1612-BRO1614** (2026-10-01): 0 in Polly, OpenTelemetry, the private app (StyleCop repos) and
  FFMpegCore; Newtonsoft.Json 13 (11 BRO1510, 2 BRO1613), Serilog 1. All fixed in one pass, compile, second run clean.

- **BRO1003/BRO1004/BRO1114/BRO1115/BRO1402** (2026-10-01): 0 in FFMpegCore, Polly, OpenTelemetry, private app;
  Newtonsoft.Json 3 (1 BRO1003, 2 BRO1402), Serilog 8 (BRO1114; found the multi-line `const string` layout, fixed).
  All fixed in one pass, compile, second run clean.

- **Batch 2** (2026-10-01): Polly 0 and OpenTelemetry 0 (both enforce these rules) after the two fixes above; private app
  356 BRO1509 (= SA1502), FFMpegCore 23, Newtonsoft.Json 387, Serilog 21. All fixed in one pass, compile, second run
  clean (OpenTelemetry: 1 left in net462-only code, the known environment limit).

- **BRO1615** (2026-10-02): none of the repos uses the XML header (OpenTelemetry has a plain one, Polly and the private
  app have SA1633 off), so every repo got a company via a global config and a header in every file: FFMpegCore 161,
  Polly 790, private app 2632 (= its SA1633 count with the rule on), Newtonsoft.Json 1100 (header above its
  `#region License`, no conflict markers across 8 frameworks), Serilog 221; OpenTelemetry with its own text configured
  (`Copyright The OpenTelemetry Authors\nSPDX-License-Identifier: Apache-2.0`): all 876 plain headers became the XML
  header around the same lines. All in one pass, no new compile errors, second run clean, BOMs and line endings kept.

- **Batch 3** (BRO1122-BRO1126, BRO1403; 2026-10-02): 0 in FFMpegCore, Polly, OpenTelemetry, the private app and
  Serilog; Newtonsoft.Json 38 (20 BRO1122, 9 BRO1123, 9 BRO1125) in 11 files, fixed in one pass, compiles, second run
  clean (lambdas keep their Allman layout).

- **Migration re-measured with 71 rules** (2026-10-02): unchanged from 46 rules: Polly 3 files (2 IDE0047, 1 broken by
  plain `dotnet format`), OpenTelemetry 1 (line ending), private app 10 (9 IDE2000, 1 BRO1104 target-typed `new()`).
  None of the 25 newer rules changes anything in these StyleCop-clean repos.

- **SA1206/SA1207 -> IDE0036** (2026-10-02): the SDK's IDE0036 enforces the whole modifier order (SDK default list)
  where StyleCop only wants access first and `static` next. Measured with IDE0036 alone at warning: Polly, OpenTelemetry,
  private app, FFMpegCore, Serilog 0 changes; Newtonsoft.Json 27 lines, all real SA1206 violations (`public new static`,
  `static private`). A planted `static public` was fixed, so the zeros are real. So no BRO rule: IDE0036 + the default
  `csharp_preferred_modifier_order` in the preset and in stylebro-migrate (strongest of SA1206/SA1207; a repo's own
  order is kept). Listed under "covered with known differences".

- **SA1205 -> IDE0040** (2026-10-02): probed with StyleCop 1.2 (9 SA1205 on partial parts without an access modifier:
  classes, structs, interfaces, records, nested, static) and `dotnet format style --diagnostics IDE0040` with
  `for_non_interface_members`: all 9 fixed (the other part's modifier or the default), StyleCop quiet afterwards.
  stylebro-migrate maps SA1205 to IDE0040 with SA1400; the preset already had IDE0040 on. Case added to
  sdk-check-cases.ps1 and the row to data/sdk-check.csv.

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
   To release: move the Unshipped rules to Shipped, bump `<Version>` in StyleBro.Package.csproj (as for alpha.3/alpha.4),
   push a tag `vX.Y.Z[-suffix]`; a prerelease publishes on its own, a stable version needs the `release` approval.
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
6. ~~StyleCop migration tool~~ Done (`src/StyleBro.Migrate`, see "Migration tool"); published as a .NET tool from alpha.5.
7. Later: blank-line layout rules (the SDK's IDE2000 series is only experimental), baseline support
   (fail only on new violations).
