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
