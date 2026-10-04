# Beyond StyleCop: rules from other analyzers

StyleCop parity is (almost) done. This page lists style rules from other popular C# analyzer packages that could
become StyleBro rules: rules with a fix that `dotnet format` could apply, that the .NET SDK doesn't already cover.
It is a survey (2026-10-03), not a plan. The top five are in the "Maybe" section of [backlog.md](backlog.md).

## Method

- Packages read, with their nuget.org downloads on 2026-10-03:
  [Roslynator](https://josefpihrt.github.io/docs/roslynator/analyzers) (Roslynator.Analyzers 61M,
  Roslynator.Formatting.Analyzers 19M), [Meziantou.Analyzer](https://github.com/meziantou/Meziantou.Analyzer) (26M),
  [SonarAnalyzer.CSharp](https://rules.sonarsource.com/csharp/) (111M; its code fixes are the `*CodeFix.cs` files in
  [sonar-dotnet](https://github.com/SonarSource/sonar-dotnet/tree/master/analyzers/src)),
  [ErrorProne.NET](https://github.com/SergeyTeplyakov/ErrorProne.NET) (2M), and StyleCop's own SX rules.
  For comparison: StyleCop.Analyzers 277M.
- Every rule list was read in full. Kept: style rules (layout, syntax choices, naming, doc comment form) whose own
  package has a code fix, or where a fix is obvious and safe. Dropped: correctness, performance and security rules,
  rules a StyleBro rule already does, and rules the SDK covers.
- SDK coverage was checked against the [IDE rule index](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/),
  the [formatting options](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/csharp-formatting-options)
  of IDE0055, and the CA rules that ship with the SDK. Where a row says "check", nobody has probed it yet. To probe,
  add a case to `scripts/stylecop-survey/sdk-check-cases.ps1` and run `Test-SdkCoverage.ps1`, as for StyleCop rules.
- Value is a judgment: how often the pattern appears in real code, whether teams argue about it (style guides,
  issues), and whether the source package turns it on by default. Almost all Roslynator formatting rules are off by
  default and need an option, so "on by default" says little there.
- Effort and risk use StyleBro's design rules (CLAUDE.md): Fix All through `LinkedFileFixAllProvider`, idempotent
  fixes, skip `#if` and comment cases rather than guess.

## Ranked candidates

| # | Source | What | SDK | Effort / risk |
|---|--------|------|-----|---------------|
| 1 | Roslynator [RCS0027](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS0027), [RCS0028](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS0028) | Operator placement when an expression is wrapped: `&&`, `+`, `??` and `?`/`:` at the start or the end of the line | No | Low |
| 2 | Roslynator [RCS0032](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS0032), [RCS0052](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS0052) | `=>` and `=` placement when the line breaks there | No | Low |
| 3 | Roslynator [RCS1248](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1248) | `x == null` vs `x is null` (one style) | No | Medium (semantic) |
| 4 | Roslynator [RCS0054](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS0054) | A split call chain has every call on its own line | No | High (indentation) |
| 5 | Roslynator [RCS1253](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1253), Meziantou [MA0177](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0177.md)/[MA0211](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0211.md) | `<summary>` on one line or on three | No | Low |
| 6 | Roslynator [RCS0061](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS0061) | Blank line between switch sections (include / omit) | No | Low |
| 7 | Roslynator [RCS1081](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1081), Sonar [S1659](https://rules.sonarsource.com/csharp/RSPEC-1659/) | One local per declaration: `int a, b;` split | No | Low |
| 8 | Roslynator [RCS1214](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1214), [RCS1192](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1192), [RCS1262](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1262) | Unneeded `$`, `@` or raw string on a literal | No (check) | Low |
| 9 | Meziantou [MA0154](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0154.md) | `<c>null</c>` -> `<see langword="null"/>` in doc comments | No | Low |
| 10 | Roslynator [RCS1050](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1050) | `new Foo() { X = 1 }` vs `new Foo { X = 1 }` | No | Low |
| 11 | Sonar [S818](https://rules.sonarsource.com/csharp/RSPEC-818/) | Upper-case literal suffixes: `1ul` -> `1UL`, `2.5f` -> `2.5F` | No (CS0078 warns about `l` only) | Low |
| 12 | Roslynator [RCS0041](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS0041), [RCS1006](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1006) | `else` + `if` on one line; `else { if ... }` -> `else if` | No | Low / Medium |
| 13 | Roslynator [RCS1251](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1251), Meziantou [MA0206](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0206.md) | Empty record body: `record R(int X) { }` -> `record R(int X);` | No | Low |
| 14 | Roslynator [RCS1042](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1042), Sonar [S1939](https://rules.sonarsource.com/csharp/RSPEC-1939/) | Redundant base: `enum E : int`, `class C : object` | No | Low |
| 15 | Roslynator [RCS1232](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1232) | Doc comment elements in a fixed order (summary, typeparam, param, returns, exception, remarks) | No | Medium |
| 16 | Meziantou [MA0071](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0071.md), Roslynator [RCS1211](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1211) | No `else` after `return`/`throw`/`break`/`continue` | No | Medium |
| 17 | Sonar [S4136](https://rules.sonarsource.com/csharp/RSPEC-4136/) | Overloads next to each other | No | Medium (BRO1001 option) |
| 18 | Roslynator [RCS1046](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1046), Meziantou [MA0137](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0137.md) | `Async` suffix on awaitable methods (and not on others) | Detect only (IDE1006, no Fix All) | High (renames) |

### 1. Operator placement when wrapping (RCS0027, RCS0028)

Done (2026-10-04): [BRO1520](rules/BRO1520.md).

```csharp
// before (option: before)                // after
if (isValid &&                             if (isValid
    isReady)                                   && isReady)
```

- Fix in Roslynator: yes. Off by default; needs `roslynator_binary_operator_new_line` / `roslynator_conditional_operator_new_line`.
- SDK: no rule. The SDK has the key [`dotnet_style_operator_placement_when_wrapping`](https://learn.microsoft.com/en-us/visualstudio/ide/reference/code-styles-refactoring-options),
  but only the IDE's "wrap" refactorings read it; IDE0055 lists no such option, and a request for enforcement is open
  ([roslyn discussion 47601](https://github.com/dotnet/roslyn/discussions/47601)).
- Value: high. A common style-guide point, and StyleBro can follow the SDK key (decisions.md: SDK key before StyleCop).
- Effort: low. Syntax only: move the token across the line break, keep indentation. Skip a comment in the gap and
  `#if` around the operator. No StyleCop rule competes.

### 2. `=>` and `=` placement (RCS0032, RCS0052)

Done (2026-10-04): [BRO1521](rules/BRO1521.md) and [BRO1522](rules/BRO1522.md).

`int P =>` + newline + `value;` vs `int P` + newline + `=> value;`. Same shape as #1 and the same fix code; could be one
rule with several options, or rules next to #1. The SDK's experimental IDE2006 only removes blank lines after `=>`.

### 3. Null check style (RCS1248)

`if (x == null)` -> `if (x is null)`, or the reverse. Off by default, needs `roslynator_null_check_style`. SDK: IDE0041
only rewrites `ReferenceEquals(x, null)`, IDE0150 only `x is object`. Value: high; teams argue about it, and `is null`
avoids user-defined `==`. Effort: medium, semantic. Skip types with a user-defined `==` (the rewrite changes behavior),
expression trees (`is` patterns don't compile there), and `is not null` before C# 9.

### 4. Call chain layout (RCS0054)

```csharp
// before                                  // after
var names = people.Where(p => p.Active)    var names = people
    .Select(p => p.Name).ToList();             .Where(p => p.Active)
                                               .Select(p => p.Name)
                                               .ToList();
```

Fix in Roslynator: yes, off by default. SDK: none. Value: high in LINQ and builder code. Effort: high. StyleBro should
mirror BRO1108 (only chains that are already split; never split a one-line chain) and decide where the first call
goes (`people` + newline, or `people.Where(...)` on the first line). Multi-line lambdas inside the chain need their
bodies reindented, conditional access (`?.`) counts as a link, and `#if` inside a chain is a skip.

### 5. Summary layout (RCS1253, MA0177, MA0211)

`/// <summary>Gets the name.</summary>` vs the three-line form. Off by default in Roslynator; Meziantou has both
directions as separate rules. SDK: none. Effort: low, text only, same doc helpers as BRO1603. Single-line only when
the text is one line. Fits next to BRO1601-BRO1615.

### 6. Blank line between switch sections (RCS0061)

Options `include`, `omit`, `omit_after_block`. SDK: none; StyleCop has no rule (SA1513 wants a blank line after a `}`,
which BRO1519 already handles, but not after `break;`). Effort: low; reuse `BlankLineRuns`. Must agree with BRO1519
when a section ends in a block.

### 7. One local per declaration (RCS1081, S1659)

`int a = 1, b;` -> two statements. BRO1114 does this for fields only (like StyleCop). Fix exists in both packages.
Effort: low, extend BRO1114. Skip `for` initializers and `using`/`fixed` declarations (splitting changes the scopes
or doesn't compile).

### 8. Unneeded `$`, `@`, raw string (RCS1214, RCS1192, RCS1262)

`$"Done"` -> `"Done"`, `@"Done"` -> `"Done"`, `"""Done"""` -> `"Done"`. Fixes exist. SDK: IDE0071 simplifies holes,
not a `$` without holes (check). Effort: low, text only; the fix must unescape `{{`/`}}` and keep `@` when the text has
`\`, `"` or a line break.

### 9. `langword` in doc comments (MA0154)

`<c>null</c>`, `<c>true</c>` -> `<see langword="null"/>`. Meziantou turns it on (info) and has a fix; the .NET runtime
repo writes docs this way. Effort: low. Only C# keywords.

### 10. Object creation parentheses (RCS1050)

`new List<int>() { 1 }` vs `new List<int> { 1 }`. Off by default, needs `roslynator_object_creation_parentheses_style`.
SDK: none (IDE0090 is about `new()`). Effort: low; target-typed `new() { }` needs its parentheses.

### 11. Upper-case literal suffixes (S818)

Sonar has a fix (`LiteralSuffixUpperCaseCodeFix`). The compiler warns (CS0078) about `l` only. Effort: low, next to
BRO1122. Hex digits are not suffixes (`0xff` stays).

### 12. `else if` (RCS0041, RCS1006)

RCS0041 joins `else` and `if` that sit on two lines; RCS1006 turns `else { if (...) ... }` (the block holds only the
`if`) into `else if`. The first is a gap rewrite; the second needs the inner statement reindented, and a comment in the
block is a skip.

### 13. Empty record body (RCS1251, MA0206)

`record R(int X) { }` -> `record R(int X);`. Effort: low. BRO1101 already treats the bodiless form; check that the two
fixes don't fight over the `;`.

### 14. Redundant base (RCS1042, S1939)

`enum E : int` and `class C : object`. Semantic (`Int32`, aliases). Effort: low. Low value; most code doesn't do it.

### 15. Doc comment element order (RCS1232)

BRO1611 already orders `<param>` tags. A fixed order for the top-level elements is the same move. Only when every
element has its own lines (like BRO1611).

### 16. No `else` after a jump (MA0071, RCS1211)

`if (x) { return a; } else { return b; }` -> `if (x) { return a; }` + `return b;`. Fixes exist. Opinionated (some
teams want the symmetric form). Effort: medium: unindent the else body, skip when a local declared there would clash
with one in the enclosing block, skip comments on `else`.

### 17. Overloads together (S4136)

Sonar has no fix. For StyleBro it is an extra sort key in BRO1001 (keep members with the same name next to each
other, inside the kind/access order). Conflicts with StyleCop's access order when overloads differ in access, so an
option, off by default.

### 18. Async suffix (RCS1046, MA0137)

`Task LoadAsync()` naming. The SDK can report it with a naming rule (`required_modifiers = async`), but `dotnet format`
can't fix IDE1006. StyleBro's renamer could, but this renames public API, and frameworks bind some names (MVC trims
the suffix from action names; test frameworks don't care). Effort: high; only after the rename guards prove out.

### Small ones, if someone asks

Roslynator [RCS1209](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1209) (constraints in type parameter
order), [RCS1222](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1222) (merge adjacent `#pragma warning`
lines), [RCS0051](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS0051) (`while` of a do-while on the `}` line or
not), [RCS0042](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS0042) (auto-accessors on one line),
[RCS1252](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1252) (`while (true)` vs `for (;;)`),
[RCS1099](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1099) (`default` label last in its section),
[RCS0024](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS0024) (statement after `case X:` on its own line;
check `csharp_preserve_single_line_statements` first), Meziantou [MA0175](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0175.md)
(`record class` -> `record`), Sonar [S3253](https://rules.sonarsource.com/csharp/RSPEC-3253/) (redundant `: base()`).

## Rejected

| Rules | Why |
|-------|-----|
| RCS0003, RCS0006, RCS0010, RCS0012, RCS0060 | Blank lines between declarations/usings: BRO1505 (SA1516) |
| RCS0008, RCS0063, RCS0001 | Blank lines after blocks, extra blank lines: BRO1517-BRO1519; RCS0001 is moot with braces (BRO1514) |
| RCS0009 | Blank line before a doc comment: BRO1513 |
| RCS0020, RCS0021, RCS0023 | Brace layout: BRO1508-BRO1510 and `csharp_new_line_before_open_brace` |
| RCS0029, RCS0031, RCS0034 | Own-line rules: BRO1105, BRO1121, BRO1111 |
| RCS0053 | List layout: BRO1107/BRO1108. Its indentation half (all items at one column) could extend BRO1108 later |
| RCS0002, RCS0005, RCS1189 | Region layout and names: BRO1112/BRO1113 remove regions |
| RCS1001, RCS1003, RCS1007, RCS1126 | Braces: BRO1514-BRO1516 |
| RCS1018, RCS1019 | Accessibility modifiers: BRO1404/BRO1007; modifier order: IDE0036 |
| RCS1020, RCS1032, RCS1039, RCS1048, RCS1052, RCS1055, RCS1078, RCS1098, RCS1123 | Already StyleBro: BRO1115, BRO1405, BRO1402, BRO1125, BRO1102, BRO1101, BRO1106, BRO1103, BRO1406/BRO1407 |
| RCS1260, MA0007 | Trailing comma: BRO1401 |
| MA0067 | `new Guid()` -> `Guid.Empty`: BRO1104 |
| RCS0015, RCS0016, RCS0033, RCS1037, RCS0044, RCS0045, RCS0046 | SDK formatting: `dotnet_separate_import_directive_groups`, IDE0055 (SA1134, SA1107 in stylecop-mapping.md), trailing whitespace, `end_of_line`, `indent_style` |
| RCS0057, RCS0058 | Whitespace at start/end of file: SA1517/SA1518, already decided in stylecop-mapping.md (BRO1507, SDK) |
| RCS1013, RCS1016, RCS1021, RCS1058, RCS1085, RCS1094, RCS1146, RCS1128, RCS1084, RCS1169, RCS1207, RCS1244, RCS1250, RCS1264 | SDK: IDE0049, IDE0021-IDE0027, IDE0053, IDE0054, IDE0032, IDE0065, IDE0031, IDE0029, IDE0029, IDE0044, IDE0200, IDE0034, IDE0090, IDE0007/IDE0008 |
| RCS1033, RCS1049, MA0073, S1125 | Boolean comparisons: IDE0100/IDE0075 (check `== false`) |
| RCS1129, RCS1188, S3052 | Redundant default initializers: CA1805 (ships with the SDK, has a fix) |
| RCS1015, S3441 | `nameof`: CA1507/IDE0280; anonymous member names: IDE0037 |
| RCS0030 | Embedded statement on its own line: IDE2001 (experimental), moot with braces |
| RCS1002, RCS1004 | Remove braces: the opposite of BRO1514-BRO1516 |
| RCS0036, RCS0011, RCS0013, RCS0007 | Blank lines between single-line declarations/accessors: overlap or conflict with BRO1505 |
| RCS0039, RCS0048, RCS0025 | Join lines (base list, initializer, accessor): opinion with no clear default, and joining fights the expanding rules |
| RCS0056 | Line too long: the fix needs a full layout engine (where to break) |
| RCS1205 | Reorder named arguments: changes evaluation order |
| RCS1154, RCS1161 | Sort enum members / explicit values: changes declaration order or adds noise |
| RCS1068, MA0213, MA0205 | Negation and xor rewrites: logic, not layout (NaN, user-defined operators) |
| RCS1061, S1066 | Merge nested `if`: restructures logic; comments and `else` branches make it unsafe more often than not |
| MA0003 | Named arguments for literals: semantic and very noisy; not style in the StyleCop sense |
| RCS1060, MA0048, RCS1110, RCS1093 | One type per file, file names, namespaces: `dotnet format` can't move code or rename files |
| RCS1138-RCS1142, RCS1226, MA0197 | Add doc elements: placeholder text (StyleBro's decision: no generated stubs) |
| RCS1077, RCS1080, RCS1197, MA0020, MA0028, MA0029 and the other LINQ/StringBuilder rules | Performance, not style |
| ErrorProne.NET (all) | Correctness and performance only; no style rules |
| Sonar `SonarAnalyzer.CSharp.Styling` (T0xxx) | SonarSource's own house style (0.2M downloads), no code fixes |
| StyleCop SX1101, SX1309, SX1309S | Already in [stylecop-mapping.md](stylecop-mapping.md) (IDE0003, BRO1303, skipped-rules.md) |

## Open questions

- Rows marked "check" need a probe against the SDK before work starts.
- #1 and #2 might be one rule with options or several rules; decide with the user when one is picked.

## From StyleCop's issue tracker

A second survey (2026-10-04): proposals, requested fixes and options, and known bugs in the
[StyleCop.Analyzers issue tracker](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues), checked against
StyleBro. Not a plan either; the top five are in the "Maybe" section of [backlog.md](backlog.md).

### Method

- `gh issue list --repo DotNetAnalyzers/StyleCopAnalyzers --state all --limit 5000 --json number,title,labels,state,comments,reactionGroups`:
  2,436 issues (430 open, 2,006 closed; pull requests not included). Labels: `bug` 625, `enhancement` 351,
  `duplicate` 245, `code fix` 105, `needs discussion` 97, `new rule` 87, `proposal` 83.
- Read: every title with the labels `new rule`, `proposal`, `enhancement`, `code fix` or `needs discussion` (minus
  duplicates), every open issue naming an SA rule StyleBro replaces, and every title mentioning an option, setting
  or exception, ranked by reactions x 3 + comments. About 40 were read in full with `gh issue view --comments`.
- The commits on StyleCop's `master` since its last release (1.2.0-beta.556, 2023-12-20): 359, of which about 20
  change what a rule StyleBro replaces reports. StyleBro's parity sets compare with beta.556, so these are behavior
  changes StyleBro hasn't been compared with.
- StyleCop's `documentation` folder: the proposed rules SA1029, SA1138, SA1315, SA1521 and SA1653 have no page (never
  implemented). The SX rules (SX1101, SX1309, SX1309S) are already in [stylecop-mapping.md](stylecop-mapping.md).
- Suspected bugs were confirmed with throwaway unit tests (not committed).

Popularity in StyleCop's tracker is low overall: the most-discussed open proposal has 19 reactions. So the signal
below is "the maintainers agreed and nobody built it" more than "many people asked".

### Ranked candidates

| # | Issue | What | SDK / StyleBro today | Effort / risk |
|---|-------|------|----------------------|---------------|
| 1 | [#651](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/651) (19 reactions, 23 comments, open, maintainers agreed), [#752](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/752) | A conditional expression is on one line, or the condition, `? a` and `: b` each on their own line | No SDK rule. BRO1520 only moves `?`/`:` across an existing line break | Low |
| 2 | [#738](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/738) (SA1521, up for grabs) | No blank line between an attribute and what it applies to | No SDK rule; BRO1511 does the same for doc comments | Low |
| 3 | [#2252](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2252) (12 reactions, 13 comments), [#1175](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1175) | Option: allow `if (x) return;` (a jump statement on the `if` line) without braces | `csharp_prefer_braces = when_multiline` (which BRO1514 follows) allows every single-line child, not only jumps | Low (an option for BRO1514) |
| 4 | [#605](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/605) (9 comments, maintainers in favor) | No comment between a declaration's header and its `{` (`class C // note`, then `{` on the next line) | No SDK rule. BRO1132 (SA1108) does this for statements | Low (extend BRO1132's move) |
| 5 | [#1563](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1563) (2 reactions, 8 comments) | Upper-case literal suffixes: `1ul` -> `1UL` | Same as #11 above (Sonar S818); CS0078 warns about `l` only | Low |
| 6 | [#758](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/758) (SA1653, maintainers liked it) | `cref="List&lt;T&gt;"` -> `cref="List{T}"` | No SDK rule | Low, text only |
| 7 | [#3546](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3546), [#1490](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1490) | Option: tool marker comments (`// ReSharper disable once ...`, `// @formatter:off`) don't need BRO1504's blank line | BRO1504 treats them like any comment (the workaround is `////`) | Low (a prefix list option) |
| 8 | [#762](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/762) | `(x) => x` -> `x => x` (a maintainer suggested folding it into SA1119) | No SDK rule (check) | Low; keep the parentheses with a type, modifier or attribute |
| 9 | [#760](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/760) | Redundant `return;` at the end of a void method (and `yield break;` at the end of an iterator) | No SDK rule (check); Roslynator RCS1134 | Low; skip when a comment or label is on it |
| 10 | [#2641](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2641) (4 reactions, 9 comments, wontfix), [#3793](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3793) | camelCase private constants and static readonly fields | BRO1306 always wants PascalCase. Could follow `dotnet_naming_rule.*` like BRO1303 does | Low (an option); the renamer exists |
| 11 | [#2981](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2981) (3 reactions, 15 comments) | No `public` on members of an internal type: `internal` says the same | No SDK rule | High: implicit interface implementations, overrides and operators must stay `public`, and serializers, data binding and reflection see only public members, so the rewrite can change run-time behavior |
| 12 | [#1949](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1949) (SA1315) | A parameter has the same name as in the member it overrides or implements | SDK CA1725 reports it (check whether `dotnet format` can fix it) | Medium: BRO1302's renamer could do it, but callers' named arguments change |

#### 1. Conditional expression layout (#651)

```csharp
// before                                  // after
var label = express ? name + " (express)"  var label = express
    : name;                                    ? name + " (express)"
                                               : name;
```

Like BRO1108 for arguments: when the expression is already split, each part gets its own line; a one-line expression
is never split. The operator side comes from BRO1520's setting (`dotnet_style_operator_placement_when_wrapping`), so
the two rules agree. New lines are one indentation unit deeper than the line the expression starts on. Skip comments
in the gaps and `#if`. #752 asks the same for chains of binary operators; that is harder (mixed operators and
precedence) and could come later.

Done (2026-10-04): [BRO1524](rules/BRO1524.md) (chains are skipped, see decisions.md).

#### 2. No blank line after attributes (SA1521, #738)

`[Fact]`, a blank line, then `public void Test()`: the blank line goes. A text fix like BRO1511's. Skip when a comment
sits between, and blank lines inside `#if`. Must agree with BRO1505, which counts a blank line anywhere above the
element's code: if that blank line was the only one between two members, BRO1505 then wants one above the attribute
(check the order-independence as for BRO1509).

#### 3. Braceless jump statements (#2252)

An option for BRO1514 (for example `stylebro_braces_allow_single_line_jump = true`): `return`, `throw`, `break` and
`continue` on the `if` line stay without braces; on the next line they still get braces. StyleCop's maintainers were
split on it; #1175 asked the same. Off by default, so nothing changes for StyleCop users.

#### 4. Comments in declaration headers (#605)

`class C // note` followed by `{` on the next line, also for methods, properties and accessors. BRO1132 already moves
such a comment into the block for `if`, `while` and the other statements; the same move works for types and members.
Skip multi-line headers like BRO1132 does.

### Requested fixes and options StyleBro already has

- Code fixes StyleCop never shipped: SA1108 ([#819](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/819), BRO1132),
  SA1114/SA1115 ([#505](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/505),
  [#922](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/922), BRO1118/BRO1119), SA1117
  ([#1504](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1504), BRO1108), SA1612
  ([#754](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/754), BRO1611). The other open fix requests
  (SA1118 [#818](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/818), SA1403
  [#353](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/353), SA1648
  [#1464](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1464)) stay as in
  [skipped-rules.md](skipped-rules.md): nothing in the issues changes those reasons.
- Custom member order ([#2541](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2541),
  [#3795](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3795),
  [#3920](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3920)): `stylebro_member_order` and
  `stylebro_member_access_order`.
- Members whose order matters ([#3808](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3808),
  [#2548](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2548) dependency property keys,
  [#3819](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3819) `[StructLayout]`): BRO1001 skips those types.
- `base.` calls a derived override would intercept ([#2938](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2938)):
  BRO1131 skips virtual members unless the type is sealed.
- `?`, `!`, `:` endings for SA1629 ([#2724](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2724)), "a value
  indicating" for SA1623 ([#3010](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3010)), `_` and `__`
  parameters ([#2974](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2974)), documentation scope for
  internal members ([#2361](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2361),
  `stylebro_document_internal_elements`), `file partial class`
  ([#3906](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3906), BRO1007 skips `file` types), `this.`
  options ([#2612](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2612), the SDK's
  `dotnet_style_qualification_for_*`).
- StyleCop's unreleased changes StyleBro already matches: SA1513 before `into`, `=>`, `&&`/`||` and pattern
  designations (BRO1519), SA1408 for `and`/`or` patterns (BRO1407), SA1110/SA1111 on primary constructors and
  target-typed `new` (BRO1109/BRO1110 report them; StyleCop's `master` now does too, so the "reports more" notes in
  [differences-from-stylecop.md](differences-from-stylecop.md) hold for beta.556 only), SA1623 skipping summaries
  that start with an element (BRO1604).

### Possible bugs to check

Confirmed with throwaway tests on `main` (2026-10-04) unless noted; not fixed here.

1. **BRO1504, comment at the start of a collection expression.** `int[] a =`, `[`, `    // first`, `    1,`, `];` is
   reported (only `{` counts as the start of a scope). StyleCop beta.556 does the same; fixed upstream for
   [#3766](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3766) (commit 7dae8e97, unreleased). Likely fix:
   treat the `[` of a collection expression like `{` in `BlankLines.NeedsBlankLineAbove`.
2. **BRO1603, period followed by `)` or a quote.** `<summary>Gets the list (see above.)</summary>` and
   `<summary>Gets the word "done."</summary>` are reported, and the fix appends another period. Open in StyleCop:
   [#2860](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2860).
3. **BRO1603, text ending in an XML entity.** `<summary>Gets the List&lt;T&gt;</summary>` is reported right after `T`,
   before `&gt;`, so the fix writes `List&lt;T.&gt;`. `GetInsertionPoint` looks at `XmlTextLiteralToken`s only and
   skips the trailing `XmlEntityLiteralToken`. StyleCop beta.556 had a different bug there (it replaced the `;`,
   [#3802](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3802), fixed upstream).
4. **BRO1606, summary wrapped in `<para>`.** A summary whose text is
   `<para>Initializes a new instance of the <see cref="C"/> class.</para>` is reported; going by the code, the fix puts
   a second standard sentence in front of `<para>`. StyleCop beta.556 also reports it; fixed upstream (commit
   29514d23, unreleased).
5. **BRO1510, attribute on an accessor.** `[DebuggerStepThrough]` above `get { return x; }`, next to
   `set { x = value; }`: the attribute line makes `get` count as multi-line, so the list is reported, and the fix
   expands only `set`, leaving `get { return x; }` on one line next to a multi-line `set` (the mixed layout the rule is
   about). StyleCop reports it too: [#3434](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3434). Likely
   fix: measure an accessor from its keyword, not from its attributes.
6. **BRO1505 is less strict than SA1516 (not shared).** StyleCop (beta.556 and `master`) wants a blank line between
   two fields when the first spans several lines (a multi-line initializer); BRO1505 never asks for one between fields.
   StyleCop's `master` also stopped reporting two single-line properties in a row (commit 2aeb4e3d, unreleased). The
   parity set has no multi-line field, so it didn't show.
7. **stylebro-migrate ignores two stylecop.json settings** (from reading `Migration.cs`, not run):
   `documentationCulture` (StyleCop checks SA1623/SA1624/SA1642/SA1643 against translated standard texts, see
   [#1143](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1143) and
   [#3397](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3397)), so BRO1604-BRO1607 would rewrite a German
   or French summary into English; and `excludeFromPunctuationCheck` (SA1629), so BRO1603 would add periods to tags
   the team excluded. Turning those rules off, with a report note, when the settings aren't the defaults would keep a
   migrated repository unchanged.

Also from `master`: SA1413 now reports multi-line property patterns without a trailing comma (commit 94671b70).
BRO1401 leaves patterns out, like beta.556; a parity follow-up once StyleCop releases it.

### Rejected

| Issues | Why |
|--------|-----|
| [#1808](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1808) line length, [#2196](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2196) file length | No deterministic fix (where to break, what to move) |
| [#1481](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1481) namespace matches folder | SDK: IDE0130 |
| [#2193](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2193) `var`, [#1525](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1525) expression bodies, [#865](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/865) `nameof`, [#2352](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2352) `== true` | SDK: IDE0007/IDE0008, IDE0022 and its siblings, CA1507, IDE0100 |
| [#2025](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2025) SA1138 indentation, [#1029](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1029) indent by one, [#2100](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2100) UTF-8 without BOM, [#827](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/827) SA1029 split `?.` | SDK formatting: IDE0055, `charset`; a line break inside `?.` is rare |
| [#2220](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2220), [#746](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/746) named arguments | Semantic and noisy (like MA0003 above) |
| [#3403](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3403) `#pragma` justification, [#2057](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2057) Latin-only names, [#3429](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3429) zero-width spaces | The fix needs text or a name a person chooses |
| [#1357](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1357) file headers with a date | A fix would write the current year: not deterministic |
| [#3721](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3721) `case A: case B:` -> `case A or B:` | Opinion; the maintainers prefer separate labels |
| [#3827](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3827) `$` without holes | Closed by the maintainers; already #8 above |
| [#3458](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3458) `Equals` -> `==`, [#3440](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3440) `&` -> `&&`, [#1455](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1455) `HasValue` -> `!= null`, [#2333](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2333) `default(T)` -> `null` | Change behavior (NaN, side effects) or taste; IDE0034 covers `default` |
| [#3029](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3029) private auto-properties -> fields, [#2780](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2780) internal fields -> properties | Changes what reflection and serializers see |
| [#3068](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3068) blank line after a braceless `if` | Only for code without braces, which BRO1514 asks for |
| [#3550](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3550) comment right after `=>`, [#1613](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1613) `} = value;` after accessors | StyleCop's behavior by design (BRO1504 matches it); BRO1519 already doesn't report `} = value;` |
| [#1312](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1312) local constants in PascalCase, [#3046](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3046) operator parameter names, [#2986](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2986) `static` on every partial part | Possible with the existing renamer or modifier code, but no demand beyond the issue itself |
| Assembly attribute rules ([#1640](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1640) to [#1646](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1646)), documentation content ([#601](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/601) to [#603](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/603), [#1950](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1950)) | Wontfix upstream; the fix would be invented text |
