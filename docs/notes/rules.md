# Rule implementation notes

Per-rule implementation notes: logic, skips, StyleCop parity, deviations and the bugs real repositories found. Moved from CLAUDE.md on 2026-10-23; add new rule notes here.

## BRO1001: member ordering (implemented, unit + dotnet format tests pass)

- Replaces SA1201-SA1204 and SA1214. Sort key: kind -> accessibility -> const -> static -> readonly.
  Defaults match StyleCop so migration is painless (kind order verified pairwise against StyleCop 1.1.118).
- One diagnostic per type, on the first out-of-place member. The fix sorts the whole type.
- Trivia: blank-line layout stays with the position; comments, docs and attributes move with the member. Since trial 27
  (owner: "the sort keeps blank lines"): two members that were neighbours and stay neighbours keep their own gap, and the
  "creates a BRO1505 violation" blank line is added also with BRO1505 off (a BRO1519 blank line of the old pair counts).
  Exception: a member led by a `//` comment moved into a later slot without a blank line gets one, otherwise the
  sort creates a BRO1504 violation and `dotnet format` needs a second run (seen in Newtonsoft.Json).
- The Fix All rewriter sorts nested types before their parents in one pass.
- Regions (2026-10-03, user's pick from the backlog): every `#region`/`#endregion` line between members is a fence
  (`GetSegments`); members are sorted within each stretch, compared as (segment, key), so the order ACROSS regions isn't
  checked (StyleCop's is; the fix would move members into another region; parity `member-order-regions`). Region lines
  and anything above them are slot layout (`SplitLeadingTrivia` starts after the last directive). `#pragma`/`#nullable` are
  fences too. Still skipped: conditional directives (multi-targeting: each TFM's copy has other members there), or a doc
  comment above a directive. A doc comment moved right below a region line gets BRO1513's blank
  line (found in Messy: depending on fix order `dotnet format` needed a second run).
- Types where sorting would swap dependent field/auto-property initializers are skipped (`InitializerOrder.cs`).
- Since 2026-10-03 also the members of a namespace (block or file-scoped) and a file: StyleCop's OuterOrder (namespace,
  delegate, enum, interface, struct, class) + access + static, a type without a modifier is internal there. Skipped: a
  file whose first type carries the header (nothing before it), top-level statements (no kind), directives between
  types. A modifier-less partial part gets its real accessibility from the semantic model (`GetPartialAccess`, read on
  the original nodes in the fix): with the modifiers alone, BRO1007 adding `public` in the same run made BRO1001 want
  a second run. The last slot keeps the file's ending (no final newline stays none). Parity `namespace-order`: output
  identical, positions once per container.
- Code: `src/StyleBro.Analyzers/Ordering/MemberOrdering.cs` (shared logic), `MemberOrderOptions.cs`,
  `MemberOrderingAnalyzer.cs`, `src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs`.

## BRO11xx: readability (numbered in blocks like StyleCop: BRO11xx readability, BRO16xx documentation)

- **BRO1101** (SA1106) empty statements: `;` in blocks/switch sections and after a type's or namespace's `}`.
  Not reported: embedded (`while (x) ;`, left to the compiler's CS0642; a `{ }` fix would hide a likely bug),
  labeled (`end: ;`, removal can break the build), bodiless records. The fix is text-based and computed per line
  (`EmptyStatements.GetChanges`), so single fix and Fix All agree: whitespace-only lines go completely, otherwise
  the `;` takes the adjacent whitespace. Directives always have their own line, so no directive skip is needed.
- **BRO1102** (SA1133) combined attributes: `[A, B]` -> `[A]` + `[B]`, target repeated. Own line -> one line per
  attribute; shares a line with code -> `[A][B]` (trial 40: the whitespace pass removes the space). Like StyleCop, parameters/type parameters are not reported.
  Lists with comments between attributes are skipped (StyleCop's fix drops the comment).
- **BRO1103** (SA1131) constant on the left of a comparison: swap operands, flip `<`/`>`/`<=`/`>=`. "Constant" as in
  StyleCop (incl. `static readonly` fields). Only built-in operators and operators from the core library (same
  assembly as `object`) are swapped; a type's own `==` may not be symmetric -> not reported (deviation). Same-precedence
  left operand gets parentheses (`1 == 2 == b` -> `b == (1 == 2)`). Fix All is a bottom-up rewriter (nesting). Not in
  expression trees (trial 58: ExpressionTreeToString's `Tuesday <= dow` became a `>=` node; owner's decision 2026-10-27).
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
- **BRO1404** (SA1400) and **BRO1007** (SA1205), 2026-10-03, `Maintainability/AccessModifiers.cs`, replace IDE0040:
  StyleCop's declaration kinds and skips (interface members, explicit implementations, static ctors, partial members;
  operators/finalizers aren't checked); partial types are BRO1007's. The fix inserts the symbol's DeclaredAccessibility
  (the default, or another part's) before the first modifier, after attributes. Parity `access-modifiers`: 15/15,
  identical. `init`/migrate write IDE0040 = none, which also fixes SA1205-without-SA1400 (only partial types now).
  Trial 57 (DapperAOT): no finding when the modifiers depend on `#if` (a skipped branch holding only modifiers before the
  declaration, or a directive between its modifiers, `HasConditionalModifiers`): an embedded-resource template's
  `#if !DAPPERAOT_INTERNAL` / `file` / `#endif` / `static class` got `internal` in the tests' copy, `file internal` (CS9052).
- **BRO1405** (SA1119, 2026-10-03, `Maintainability/Parentheses.cs`, replaces IDE0047): StyleCop's exact logic (simple
  expressions always, operator expressions only where nothing binds to them; IDE0047 also removed `a ?? (b ?? c)`, the
  old migration difference in Polly). Guard: the enclosing statement/member re-parsed without the parentheses must be
  `IsEquivalentTo` the tree with them replaced by their content (StyleCop's fix turned `F(a < b, (c > (d + 1)))` into a
  generic call: CS0307 in the parity set); the fix checks the pairs of one container together, adding them one by one.
  Parity `parentheses`: 14/13, 8 documented. The pattern variant (SA1119_p) isn't covered (backlog gap).
- **BRO1406/BRO1407** (SA1407/SA1408, 2026-10-03, `Maintainability/Precedence.cs`, replace IDE0048): StyleCop's
  families (`+ -`, `* /`, `<< >>`, `%` alone; `&&`/`and` vs `||`/`or`); the fix inserts `(`/`)` per operand, combined per
  position (`((`/`))`) so nested operands are fixed in one pass. Also `and`/`or` patterns, like StyleCop's current
  source; 1.2.0-beta.556 doesn't (parity `precedence`: 8/10, documented).
- **BRO1517-BRO1519** (SA1507/SA1508/SA1513, 2026-10-03, `Layout/BlankLineRuns.cs`, one tree analyzer + one fix,
  replace IDE2000/IDE2002/IDE2003): StyleCop's logic, minus places another blank-line rule fixes when it's on (after
  `{` BRO1503, below `//` BRO1506, before `}` BRO1518 for BRO1517; a comment/doc/member after `}` BRO1504/BRO1513/
  BRO1505 for BRO1519), so two fixes never edit the same lines. The fix recomputes findings and matches rule + span.
  Shape-changing fixes add BRO1519's blank line themselves (`BlankLineRuns.WantsBlankLineAfter`): BRO1514's wrap
  (closing edit) and BRO1508/BRO1509's expansion (insert after `}`, or inside the gap before a following block's `{`
  with `gapIsReplaced`, else an iterative fix removed it). Parity `blank-line-runs`: 7/5, the 2 hand-offs documented.
  CSharpier mode (trial 64, ArchUnitNET): BRO1519's own finding skips a `}` followed by a `checked`/`unchecked`
  statement (CSharpier 1.3.0 removes every blank line above one); `WantsBlankLineAfter` still says yes for the fixes that
  create a `}`, since CSharpier runs after them and takes the line out (threading the option to its 20 callers wasn't worth it).
- **BRO1514-BRO1516** (SA1503/SA1519/SA1520, 2026-10-03, `Layout/Braces.cs`, replace IDE0011 per docs/decisions.md):
  which statements and which id like StyleCop (multi-line -> BRO1515, inconsistent if/else chain -> BRO1516, else
  BRO1514; a rule that is off leaves its statements to the next, via `Severities.IsOn`; `using (a) using (b)` allowed).
  The fix is text edits computed for ALL wrapped statements at once: gap edit (`{` + line break + indentation),
  reindent of the statement's other lines (shift relative to the statement's first line, or to the owner line when it
  shared it), closing `}` before the line break (innermost first when several close at one place). The statement's own
  `else` follows `}` per csharp_new_line_before_else wherever it was; a do-while `while` keeps its line. Line edits skip
  lines whose start a gap/close edit covers (else they overlap). Skipped: comments/directives in the gap, `#if` in the
  parent, a token spanning lines (multi-line strings), other code after it on its line. Parity `braces`: 15/13, output
  identical apart from the 2 skips. `init` writes IDE0011 = none; migrate writes `Rule("IDE0011")` (none).
  `stylebro_allow_single_line_jump_statements` (StyleCop issue #2252, default false, 2026-10-04, agent's proposal in
  docs/decisions.md): a jump (`return`/`throw`/`break`/`continue`/`goto`/`yield break`) ending on its `if` keyword's
  line isn't BRO1514's; if another clause of the chain is reported, the allowed jumps get BRO1516 in the same run.
- **BRO1131** (SA1100, 2026-10-03, `Readability/BaseCalls.cs`) `base.` -> `this.`: StyleCop's check (speculative
  `this` binds to the same symbol, so no override/hiding member), plus a deviation: virtual/abstract/override (not
  sealed) members are skipped unless the enclosing type is sealed (a derived override would run instead).
- **BRO1134** (2026-10-04, StyleCop issue #605, beyond StyleCop): BRO1132's (SA1108) move for declarations, a new id so
  migrate's SA1108 -> BRO1132 mapping doesn't widen. Shares `Readability/EmbeddedComments.cs`, EmbeddedCommentAnalyzer
  and the fix (`GetDeclarationOpenBrace`: types, block namespaces, block-bodied methods/ctors/operators/finalizers,
  accessor lists, accessor bodies, local functions; header start skips attribute lists). BRO1504's exemption now asks
  `GetMovingRule` which rule moves the comment (`NeedsBlankLineAbove` takes an isOn predicate). Fixed for both: the
  comment goes below blank lines after `{` (above them BRO1506 could be left for a second run; FixOrderTests). Survey: 7
  findings in the 8 repos (+2 multi-line headers skipped).
- **BRO1133** (2026-10-04, beyond StyleCop: Roslynator RCS1248, `Readability/NullChecks.cs`, semantic)
  `stylebro_null_check_style = pattern_matching` (default, owner's decision over the survey's majority, docs/decisions.md)
  | `equality_operator`.
  The fix only swaps the operator (`==` <-> `is`, `!=` <-> `is not`); pattern mode also turns `null == x` into `x is null`
  (equality mode leaves it to BRO1103; the fix finds a check by its START, which BRO1103's swap keeps, so both converge
  in one run, Messy `Lookups.cs`). Skipped: an operator that isn't built-in, the core library's (null-safe) or lifted
  (records, classes with `==`; checked by speculative binding for `is` -> `==`), dynamic, non-nullable value types, `is`
  in expression trees (IOperation parents: an anonymous function converted to a System.Linq.Expressions type, also
  query clauses over IQueryable), `is not` < C# 9 / `is` < C# 7, operands looser than `is` (whitelist), `is` checks
  whose parent binds tighter than `==` (whitelist of parents), comments/line breaks in the removed text; `is null` ->
  `== null` on a nullable value type with a lifted user-defined `==` (CI: Jellyfin's BannedApiAnalyzers bans
  `Guid.operator ==`, RS0030 after the fix). Pointers have no
  operator symbol (that guard was dead per mutation testing). Probed: no SDK rule rewrites `x == null` (IDE0041/0150/
  0083/0078 only produce patterns from other shapes).
- **BRO1006** (SA1006, 2026-10-03, `Spacing/DirectiveSpacingAnalyzer.cs`) `# if` -> `#if`: StyleCop's check (a `#` whose
  trailing trivia has no line break, reported on the next token); empty `# ` directives skipped. Parity `directive-spacing`: 8/8, identical.
- **BRO1106** (SA1122) `""`/`@""` -> `string.Empty`, except constant contexts (const, attribute args, parameter
  defaults, case labels, patterns). Same results as StyleCop, except expression trees (trial 58, owner's decision
  2026-10-27: both directions skip a lambda or query converted to System.Linq.Expressions, `NullChecks.IsInExpressionTree`;
  ExpressionTreeToString's tests render `() => string.Empty`).
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
  1.2.0-beta.556 misses records/primary constructors (documented deviation) and has no working SA1117 fix. An item
  moved to its own line takes its later lines along (a lambda body; StyleCop #1620/#3183, 2026-10-04); nested split
  lists are fixed outer first, in rounds, so single fix and Fix All give the same result (no `batchFixedSource`).
  A `(` that starts its line and that BRO1109 (on) moves up: the items are indented from the name's line, as if BRO1109
  ran first (trial 61, Respawn: run first, BRO1107 indented the first item one level deeper than its neighbour).
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
  at the end of the file takes the preceding line break along. Removing regions lets BRO1001 sort across them; since
  2026-10-04 the fix does that sort itself (and BRO1506's blank-line removal below a comment), see the convergence note.
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

- **BRO1127-BRO1130** (SA1102-SA1105, query clause layout, 2026-10-03; `Readability/QueryLayout.cs`, one analyzer + one
  fix): StyleCop's SA110xQueryClauses logic (pairs of clause keywords, `into` handling, StyleCop's query indentation:
  the from-line's indentation, +1 unit unless `from` or `(` starts that line). Every fix rewrites only the gap before a
  clause keyword. BRO1128 always splits (StyleCop's SA1103 fix joins or splits, and under `dotnet format` which one it
  applies changed between two identical runs, so the parity set `query-layout` compares positions only: 9/8, 3
  documented). Deviations: BRO1128 also reported next to BRO1129/BRO1130 (StyleCop suppresses it, leaving a mixed
  query for a second run); gaps with comments skipped, and BRO1128 skips a query with a commented same-line pair (the
  fix couldn't make it consistent). Mutation-tested; 0 findings expected in the surveyed repos (none use queries
  like this).

- **BRO1135** (2026-10-04, beyond StyleCop: issue #1563, Sonar S818; `LiteralSuffixes.GetUpperCaseSuffix`, reported by
  `LiteralSuffixAnalyzer`, fixed by `LiteralSuffixCodeFixProvider` next to BRO1122): integer suffixes upper case
  (`1l` -> `1L`, `ul`/`Ul`/`uL` -> `UL`, `lu` -> `LU`; hex/binary too). Real suffixes (`f`/`d`/`m`) not checked: lower
  case is the norm in the reference repos (S818 itself checks only `l`). BRO1122 writes upper case, so the two converge
  in any order (FixOrderTests). Survey: 25 in OpenTelemetry (24 `u`, 1 `ul`), 0 elsewhere, no `l` anywhere.

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
- **Protected fields** (2026-10-03, `FieldNames.GetRename`/`IsProtectedChecked`), StyleCop's defaults exactly:
  non-readonly protected/private protected fields are camelCase (SA1306, BRO1303); a readonly one's casing isn't
  checked (SA1306 skips it, SA1304 defers to SA1307, which checks public/internal only: Polly has both
  `DurationOfBreak` and `absoluteExpirationTime`); a leading underscore (SA1309) is removed in the camelCase style and
  kept in `_camelCase` (no underscore added: SX1309 is private). `IsDataMember` (word-inside-string guard) no longer
  includes protected fields: in Messy it blocked `Total` because the referenced StyleBro projects' strings contain it.
- **BRO1307** (SA1308, `m_`/`s_`/`t_` prefixes, lowercase only) and **BRO1308** (SA1310, underscore inside a field
  name): `FieldNames.GetRename` gives every field at most ONE rule and the COMPLETE new name (prefix > underscore >
  casing), so one rename converges and Fix All never gets two names for one field. Words are joined in the field's
  casing (Pascal for const/static readonly/non-private, the BRO1303 style for private, camel for protected); an
  all-caps word of 2+ letters counts as a word (`MAX_VALUE` -> `MaxValue`). Skipped: digit_digit (`Int32_0` would
  become `Int320`, found in Newtonsoft.Json), results that are keywords (`s_static`) or empty (`m_`); `t_` fields are
  usually `[ThreadStatic]` (attributed -> skipped). init counts multi-word private field names, camelCase against
  snake_case (`Migration.CountMultiWordPrivateFields`), and turns BRO1308 off when snake_case wins or the code mixes them
  (trial 62, mono.linq.expressions: 11 of 13 snake_case, renamed).
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
- **BRO1310** (SA1305, Hungarian prefixes; 2026-10-03; `Naming/HungarianNames.cs`): the FIRST rule that is off by
  default (`isEnabledByDefault: false`, preset `none`, like StyleCop). StyleCop's pattern `^[a-z]{1,2}[A-Z]`, its 14
  common words, `stylebro_allowed_hungarian_prefixes`/`stylebro_allow_common_hungarian_prefixes` (migrate writes them
  from namingRules), `*NativeMethods` classes skipped. No analyzer of its own: when on, CamelCaseNamingAnalyzer and
  FieldNamingAnalyzer fold the prefix into the ONE rename they compute (`HungarianNames.GetVariableName`,
  `FieldNames.GetRename(field, style, hungarian)`), reported as BRO1310, so `_iCount` -> `count` converges in one run;
  the collision checks (`CamelCaseNames.CanRename`'s mapping, `TypeFacts.CountNewName` keyed by style + prefixes) use
  the same mapping (parity found `xItem`/`yItem` both becoming `item`: CS1930). Pascal fields are BRO1306's.
  Off-by-default plumbing: `Severities.IsOn(..., enabledByDefault: false)`; the test framework turns on EVERY supported
  diagnostic, so `Verifier.RunAsync` suppresses off-by-default ids unless the test's .editorconfig sets them;
  DocExamplesTests runs all analyzers supporting an id; the parity runner sets the BRO ids' severity too.
- **BRO1311** (SA1316, tuple element casing; 2026-10-03; `Naming/TupleElementNames.cs`, `TupleElementNamingAnalyzer`,
  fix `TupleElementRenamer`): StyleCop's reports exactly (tuple TYPE elements, first letter, `_` skipped, names forced by
  a LIBRARY member whose own declaration names them; overrides of solution members ARE reported; parity
  `tuple-element-casing` 12/12). `stylebro_tuple_element_name_casing` (PascalCase default) from tupleElementNameCasing;
  includeInferredTupleElementNames isn't supported (report note). Probed: Roslyn's `Renamer` crashes on tuple
  elements ("Unexpected null" in ConflictResolver) and `FindReferencesAsync` matches elements of DIFFERENT tuple types
  by name; GetTypeInfo's (Converted)Type elements of a literal point at the literal's own names. So the fix renames by
  declarations, solution-wide per name: all tuple type elements (not library-forced), literal names whose TARGET (from
  the operation parent: assignment/return/initializer/argument) is renamed, uses (`.x`, `?.x`, patterns) of elements
  declared at renamed places; a use that a tuple/anonymous object infers a name from gets the old name written out
  (`(first: pair.First, 2)`). Safety net: a rename that adds compile errors or CS8123 warnings is dropped (warning stays). Since trial 18 (sharpcompress, IDE0042 at error) also a rename that lets IDE0042 report a tuple
  variable where IDE0042 is a warning/error (`Naming/Deconstruction.cs` mirrors Roslyn's check: an element sharing its
  name with something in scope keeps it quiet, the rename ended the clash); CamelCaseRenamer (BRO1301-BRO1309) checks
  the same per rename (`KeptReason.AddsDeconstruction`).
- **Trial 23 guards** (2026-10-15): member names used through `dynamic` (`RepositoryNames.DynamicKind`, syntax: a chain on
  `(dynamic)x`, `x as dynamic` or a name declared dynamic; `KeptReason.UsedThroughDynamic`); snapshot/approval files
  (`*.verified.*`/`*.received.*`/`*.approved.*`, `SnapshotFiles.cs`: additional files + a disk scan from each project's
  git root, cached 5 min; data members only; `NameInSnapshotFile`; owner's choice); a reference in a non-C# document keeps
  the rename (`UsedFromVisualBasic`, owner: "Leave VB alone"); `FieldNames.IsDataMember` no longer exempts private types
  (jint's scripts); a snake_case property keeps its type's other properties (`HasKeptProperty`); fields go through
  `NamingRules.AllowsRename` with the new name (`const` implies static/readonly in its modifier comparer, like
  Roslyn's; the comparers are partial, so key order decides ties, stryker's `_lockObj`). That makes FieldNames' own
  naming-rule guards (`IsLeftByNamingRule`, `ruled == Other`, `IsPrefixRequiredByNamingRule`) redundant for enforced
  rules: their mutation entries were dropped. Not done: two-run renames (a field whose new name a
  method renamed in the same run frees).
- Remaining naming: protected readonly casing, namespaces.

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
- **BRO1616** (2026-10-04, beyond StyleCop: Roslynator RCS1253, Meziantou MA0177/MA0211; `Documentation/SummaryLayout.cs`,
  own `SummaryLayoutAnalyzer` + fix, so the other doc tests don't see it): `stylebro_summary_layout = multi_line`
  (default; survey of the 8 repos: 14,329 three-line to 148 one-line summaries of one text line) | `single_line_when_fits`
  (one text line, no `<para>`/`<code>`/`<list>`, joined line <= `max_line_length`, no limit when unset). Summary only.
  Edits only the gaps between a tag and its text (line break + the start line's indentation + `/// `, or nothing), so
  BRO1603/BRO1005 stay independent; a summary with text on a tag's line gets its tags on own lines in both layouts.
  Skipped: empty, a tag sharing its line with other content (also how `/** */` is skipped: the prefix isn't `///`),
  missing/misspelled end tag; a one-line summary is never split in single_line_when_fits (not a line-length rule).
  The repo's .editorconfig sets `single_line_when_fits` (StyleBro's own code: 291 one-line to 4; Messy inherits it).
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
  extern+extern, attribute list+attribute list. With BRO1506 on, the blank lines it removes below the element's
  comments don't count, and the fix removes them while adding the line above the comments (trial 62, Rebus: usings,
  `// ReSharper ...`, blank, `namespace` took the host a second run; FixOrderTests case). A blank line ANYWHERE before the element's code counts (also below its
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
- **BRO1520-BRO1522** (2026-10-04, first rules beyond StyleCop, `Layout/WrappingPlacement.cs`, one analyzer + one fix):
  operator (binary + `?`/`:`, SDK key `dotnet_style_operator_placement_when_wrapping`, default beginning), `=>`
  (expression bodies + switch arms, not lambdas; `stylebro_arrow_placement_when_wrapping`, default end: owner), `=`
  (assignments incl. compound + EqualsValueClause; `stylebro_equals_placement_when_wrapping`, default end); defaults
  from a survey of the 8 reference repos (docs/decisions.md). Only the token moves (edit = both gaps; the gap with the
  line break is kept from the break on). Skipped: any non-whitespace trivia in the gaps, break on both sides, missing
  tokens, `{`/`[` after the token, a query after it (BRO1127-BRO1130 indent clauses by whether `from` starts the line:
  Messy showed `select` re-indented), `=>` after a `where` (BRO1111's). Probed: no SDK rule (IDE0055 included) enforces
  the SDK key. Migration writes them `none` (no SA ids); MigrationTests requires rules without SA ids to be listed
  under "Rules beyond StyleCop" in differences-from-stylecop.md.
- **BRO1523** (2026-10-04, Roslynator RCS0054, `Layout/CallChains.cs`, one analyzer + one fix): a chain = the outermost
  invocation/element access/conditional access/member access; its links = every `.`/`?.` (the `?` token) on the spine.
  Only SPLIT chains (a link starts a line); a link must start its line when it follows a call (`).X`, also `)[0].X`,
  `)!.X`), its step contains a call (trailing `.Arguments`/`.Result` exempt) and it isn't on the chain's first line
  (first line free, like RCS0054); `.WriteTo.Sink(x)` is one step (RCS0054 splits it: Serilog 142 of 182 chains).
  Defaults from the survey in decisions.md (110 calls in 79 of 20,959 split chains change). Indentation NOT checked: a
  new line copies the leading whitespace of the nearest line-starting link above (else below), so Polly's aligned
  chains stay. Edit = the gap before the link only. Skipped: comment in the gap, directives/syntax errors anywhere in
  the chain, interpolation holes, a multi-line step whose other lines don't start with the new indentation (decided on
  the step's own lines, which other links' moves never change: a first version used the line's indentation, which
  changes when an earlier link on the same line moves, i.e. a second-run finding). FixOrderTests case with
  BRO1108/BRO1110/BRO1520.
- **BRO1525** (2026-10-04, StyleCop's proposed SA1521 / issue #738, never implemented; beyond StyleCop, so no SA id in
  the descriptor or migrate would map it; `Layout/AttributeBlankLines.cs`): blank lines after any attribute list except
  assembly/module ones (before the element or the next list) are removed as whole lines. Skipped: any comment/doc/
  directive in the gap, syntax errors in the element. BRO1505 doesn't count a blank line below an attribute (element
  code starts at `[`), so no coupling is needed (FixOrderTests case). Survey: 9 findings in the 8 repos.
- **BRO1524** (2026-10-04, StyleCop issue #651, beyond StyleCop, `Layout/ConditionalLayout.cs`): once a line break sits
  right next to `?` or `:` (not inside a part), a `?`/`:` with no line break on either side gets one, on the side
  BRO1520's key asks for; a token with its break on the wrong side stays BRO1520's (disjoint gaps; FixOrderTests case).
  New line indented like the first part already starting a line. Skipped: chains (whenFalse is a conditional, or it is
  one's whenFalse: list layouts would become staircases; decisions.md), comments/directives in the gaps, a moved part
  spanning lines, syntax errors. Survey: 13 findings in the 8 repos, plus 18 skipped chain links.
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
  drops blank lines before the item, else BRO1119 needs a 2nd run; an item on a line below its comma keeps its own
  indentation, trial 56: after BRO1108 split FusionCache's `x: 1` / `, token: 2` at column 0, BRO1117 gave `token:` the
  comma's empty indentation, so the result depended on fix order); **BRO1118/BRO1119** (SA1114/SA1115, no StyleCop
  fix) blank lines after `(`/after a comma removed (only whole blank lines, so a comment on its own line blocks it).
  Comments (`Readability/CommentText.cs`): **BRO1005** (SA1004) a missing space after `///` anywhere, but several
  spaces only before a TOP-LEVEL tag (`///   <param>`); indented text/nested tags and `<code>` are fine (probed; the
  first version flagged 367 lines in Polly, which enforces SA1004);
  **BRO1120** (SA1120) empty `//`/`/* */` reported at the start/end of a group of consecutive comment lines (a comment
  after code starts a group; `////` isn't empty), the fix removes the whole empty run at that end (StyleCop one per
  run); init counts the groups it checks with text, framed by an empty comment at an end or not
  (`CommentText.GetGroupFraming`), and turns BRO1120 off when framing wins or the code mixes them (trial 62,
  mono.linq.expressions: 4 of 11 framed, Mono style). **BRO1121** (SA1136, `Readability/EnumValueLines.cs`) one enum
  value per line; a one-line enum gets BRO1509's
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
