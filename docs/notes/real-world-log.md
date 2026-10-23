# Real-world testing log

Results of running StyleBro on real repositories (2026-09-29 onwards), before the first-run trials took over (trials.md). Moved from CLAUDE.md on 2026-10-23.

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
  - Namespace-level ordering of types (SA1201-SA1204 at the outer level): done 2026-10-03, see below.
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
  `csharp_preferred_modifier_order` in the preset and in bro (strongest of SA1206/SA1207; a repo's own
  order is kept). Listed under "covered with known differences".

- **SA1205 -> IDE0040** (2026-10-02): probed with StyleCop 1.2 (9 SA1205 on partial parts without an access modifier:
  classes, structs, interfaces, records, nested, static) and `dotnet format style --diagnostics IDE0040` with
  `for_non_interface_members`: all 9 fixed (the other part's modifier or the default), StyleCop quiet afterwards.
  bro migrate maps SA1205 to IDE0040 with SA1400; the preset already had IDE0040 on. Case added to
  sdk-check-cases.ps1 and the row to data/sdk-check.csv.

- **Migration re-measured after `init` and the Fix All changes** (2026-10-03): Polly 3, OpenTelemetry 1 (unchanged).
  Private app first 22: `migrate --write` turned IDE0040 on because SA1205 (on by default) mapped to it, and IDE0040 now
  really runs (severities in .editorconfig since `init`); with SA1400 off there, it added modifiers everywhere. Fixed:
  IDE0040 only when SA1400 is on. Then 16 = the same 10 as before + 6 line-ending-only changes: stray CRLF lines in doc
  comments, which the SDK formatter (ENDOFLINE) fixes because the repo's own .editorconfig says `end_of_line = lf`.

- **BRO1006/BRO1131** (2026-10-03): 0 in FFMpegCore, Polly, OpenTelemetry, Serilog; Newtonsoft.Json 1 BRO1131 (a
  sealed JsonTextWriter subclass calling `base.WriteValue("redacted")` from its `WriteValue(object)` override: same
  overload with `this.`), fixed, compiles, second run clean. Private app not run (its clone was busy with the delta).

- **GOTCHA (2026-10-03):** `Measure-MigrationDelta.ps1 -ResetTargetRepo` leaves its clone at two extra commits
  ("baseline: plain dotnet format", which in Polly holds a file with conflict markers, and "bro"), and the
  real-world script resets to HEAD, i.e. to those. Reset the clone to upstream (`git reset --hard HEAD~2`) before a
  real-world run. Today's first runs of BRO1006/BRO1131/BRO1514-BRO1516/BRO1404/BRO1007/BRO1405 on Polly,
  OpenTelemetry and the private app used the migrated state; rerun on upstream (see below).
- **BRO1514-BRO1516** (2026-10-03): FFMpegCore, Polly, OpenTelemetry 0; private app 14 (8 files); Newtonsoft.Json 112
  (8 target frameworks, 5 files); Serilog 213 (54 files; the repo where IDE0011 crashed `dotnet format`). All fixed in
  one pass, no new compile errors, second run clean, no conflict markers (Polly's one marker file is the measurement
  clone's committed plain-`dotnet format` baseline).

- **Today's rules on upstream repos** (2026-10-03; BRO1006, BRO1007, BRO1131, BRO1404-BRO1407, BRO1514-BRO1519):
  Polly 45 (all BRO1518; SA1508 is off there), OpenTelemetry 2 (BRO1518), private app 168, FFMpegCore 5, Newtonsoft.Json
  905 (581 BRO1519, 287 BRO1405), Serilog 27 (the other three from the earlier runs). Every repo: fixed in one pass, no new
  compile errors, second run clean, no conflict markers. Found on the way: BRO1405-BRO1407 reported expressions inside
  `#if` directives (Newtonsoft.Json, 69 left after a run), fixed.

- **Migration re-measured with the SDK replacements** (2026-10-03): Polly 3 -> 1 (the 2 IDE0047 lines are gone: BRO1405 is
  SA1119 exactly; the 1 is the file plain `dotnet format` breaks), private app 16 -> 7 (the 9 IDE2000 files are gone; 6
  line-ending-only + the known BRO1104), OpenTelemetry 1 -> 3 -> 1 after a fix: BRO1518 counted LINES, StyleCop counts
  line-break trivia, so one blank line after `#endif` before `}` isn't SA1508's (a directive owns its line break).

- **Protected fields + namespace-level ordering** (2026-10-03): BRO1303/BRO1306 renames (camelCase default) FFMpegCore 72,
  Polly 466 (4 protected), private app 1079 (23 protected), Newtonsoft.Json 376 (8 protected), Serilog 197; compile;
  tests: Polly all (4 frameworks), private app 6454/6454, Newtonsoft.Json 3613/3617 (the 4 locale failures). BRO1001
  with namespace ordering: FFMpegCore 34, Polly 87, private app 881, Newtonsoft.Json 330, Serilog 46, OpenTelemetry 0;
  all converge in one pass, no new compile errors, no conflict markers (e.g. FFMpegCore: interfaces moved above classes).

- **Following the SDK's settings** (2026-10-03, docs/decisions.md; precedence `stylebro_*` > SDK key > StyleCop):
  `csharp_prefer_braces` (BRO1514-1516: when_multiline with IDE0011's multi-line definition incl. split headers;
  false = nothing), `dotnet_style_qualification_for_*` (BRO1131: false drops `base.` where the plain name binds the same,
  checked with `Speculation.SymbolAfterReplacing` on the whole statement: a position-based speculative lookup missed a
  parameter shadowing the field), `dotnet_style_require_accessibility_modifiers` (BRO1404/BRO1007: always = interface
  members too, never/omit_if_default = nothing), `dotnet_style_parentheses_in_*_binary_operators` (never_if_unnecessary
  turns BRO1406/BRO1407 off; removal stays SA1119), `dotnet_naming_rule.*` (BRO1303 style when no stylebro key; the
  preset no longer sets `stylebro_private_field_naming`; BRO1307 leaves prefixes a naming rule requires, e.g. `s_`).
  `end_of_line` dropped from this list (no rule of StyleBro's follows it). CORRECTION (2026-10-11): the whitespace pass
  does NOT normalize endings without the key; it rewrites only the lines it touches, with the OS's newline (on Windows
  `Metadata = ` + LF became `Metadata =` + CRLF: SmartEnum got 26 mixed-ending files). With `end_of_line` set it fixes
  whole files. `init` writes the files' ending when it's clear (files git converts on checkout don't count).

- **Hardening pass, all 88 rules together** (2026-10-03): every repo compiles after one pass, no conflict markers;
  tests: Polly all, private app 6454/6454, Serilog all, FFMpegCore only its 4 known cancellation failures, but
  Newtonsoft.Json 3 new failures: BRO1001 changed the order of serialized members (Json.NET writes declaration
  order; its tests compare JSON). Fixed with `Ordering/ObservableOrder.cs`: a type is skipped when the sort would
  change the relative order of instance fields of a struct / [StructLayout] type (memory layout) or of fields and
  properties of a type with a serializer attribute on it or a member. Left: `MemberSearchFlags` serializes an
  attribute-less class by reflection (can't be recognized; `KnownFailures` in scripts/realworld/repos.psd1).
  Also found: `--verify-no-changes` fails on guard-kept renames (a fix that deliberately changes nothing still counts),
  so the real-world script tests convergence as "a run changes no file".
