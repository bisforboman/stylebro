# Trial 63 picks (2026-10-30)

Variant: three random plus one targeted. SDK 10.0.113 + 8.0.132. Tested: main 16988c8 (after #320, trial 62's fixes).

## Random (scripts/trials/random.json, 95 entries when drawn)

`rng = random.Random(63)`, `rng.randrange(95)` three times: **56, 56, 37**. Entries 0-90 are in `used.tsv` (88-90 since
trial 62).

| Draw | Entry | Result |
| --- | --- | --- |
| 56 | 56 | used; 57-90 used; 91 serilog/serilog-sinks-file unused, 46 files: **picked** (key SF) |
| 56 | 56 | used; 57-90 used; 91 picked by the first draw; 92 Sergio0694/PolySharp: global.json needs SDK 11.0.100-rc.3 and every project targets net11.0 (fails the criteria); 93 statiqdev/Statiq.Framework: 857 files (too large); 94 tonerdo/pose unused, 28 files: **picked** (key PO) |
| 37 | 37 | used; 38-90 used; 91, 94 picked; 92, 93 as above; wrapped to 0-36, all used: the list ran out. Appended entries 95-103 (alphabetically, not in used.tsv or the real-world list) and kept walking: 95 ExtendedXmlSerializer/home: 1,441 files (too large); 96 kerryjiang/SuperSocket: its five core libraries target net9.0/net10.0-ios/-android/-macos/-tvos (needs workloads); 97 NLog/NLog.Extensions.Logging unused, 53 files: **picked** (key NL) |

| Key | Repository | Commit | C# files | Setup |
| --- | --- | --- | --- | --- |
| SF | serilog/serilog-sinks-file | 4c8e6772881a8d9c8f1b3142a753ea37e8ad2342 | 46 | .editorconfig, Directory.Build.props (TreatWarningsAsErrors, Nullable, ImplicitUsings, docs), global.json 9.0.200 (lowered to 10.0.100); library net9.0;net8.0;net6.0;netstandard2.0 (+ net471/net462 on Windows); tests net8.0;net9.0, 71 pass on each |
| PO | tonerdo/pose | c68ec6c120c4707f9ca4860b820162a986fda782 | 28 | No .editorconfig or props; netstandard2.1 library, netcoreapp3.1 MSTest tests (roll forward): 20 pass, 6 fail untouched (ILGenerator internals on a new runtime), 1 skipped; snake_case private fields (26 of 50) |
| NL | NLog/NLog.Extensions.Logging | 15091c8eaeb3922f17fad1268ea1ea2f34bb89c1 | 53 | .editorconfig with end_of_line = crlf (LF checkout), src/Directory.Build.props only (NuGet audit warnings as errors); libraries net462;netstandard2.0/2.1;net6.0;net8.0;net10.0; tests net10.0;net8.0;net462 (net462 left out: mono), 143 + 34 pass |

## Targeted (trial 62's fixes: tabs kept, BRO1308 off for snake_case fields, BRO1120 off for `//` frames, UTF-16 props)

Scanned (`shape.py`): FNA (tabs, no tests), SuperSocket, CppSharp, mono/api-doc-tools, mono/dbus-sharp and mono/mono-addins
(old-style projects), praeclarum/Ooui, mono/t4.

| Key | Repository | Commit | C# files | Why / setup |
| --- | --- | --- | --- | --- |
| T4 | mono/t4 | e128cfc9c86f4de2191cfe8ccbb58fccbe965096 | 94 | Tabs in 93 of 94 files (`.editorconfig`: `[*.cs] indent_style = tab`), Mono license headers framed by `//`, a BOM csproj (Mono.TextTemplating.Build) and a CRLF .sln (`*.sln eol=crlf`); AnalysisLevel latest-Recommended; Nerdbank.GitVersioning; netstandard2.0;net472;net6.0 libraries (net472/net48 on Windows only), tests net6.0;net7.0: 114 + 7 pass on each |
