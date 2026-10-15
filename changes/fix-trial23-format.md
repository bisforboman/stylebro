### New

- `stylebro-migrate format` puts the findings its fixes keep on purpose into `stylebro.baseline` (the one the build
  reads) when a run ends clean, so a build with warnings as errors passes and `--verify-no-changes` agrees; new
  violations still fail. `<StyleBroBaseline>none</StyleBroBaseline>` turns it off.
- `format`, `--verify-no-changes`, `--diff` and `baseline` build an in-solution source generator first when it isn't
  built, so its generated code counts (a rename it used broke the build before).

### Changed

- `format` leaves Visual Basic alone: it runs on the C# projects only, never changes or reports `.vb` files, and keeps
  the name of a C# member that Visual Basic code uses.
- Projects that link a shared file now share a `format` run when the `#if` symbols that file tests agree (fewer runs
  on multi-targeted repositories).

### Fixed

- `format` now also protects `#` lines in multi-line strings under `#elif`, under conditions like
  `NET8_0_OR_GREATER && !NET10_0_OR_GREATER`, and a string's own `#if`/`#endif` lines (CS8999 after formatting).
- After a killed `format`, a `dotnet format` still running no longer leaves swapped `#` characters behind, and a
  temporary `.stylebro-format-*.slnf` it left is removed by the next run.
- A CR inside a string literal (a verbatim regex) no longer counts as an old Mac line ending.
- A code fix of another analyzer that fails is no longer reported as a StyleBro bug.
- The baseline now also covers files a project compiles from another folder (`<Compile Include="..\Lib\A.cs" />`): their
  baselined findings still failed that project's build.
- `--verify-no-changes` deletes its temporary copy, and copies a killed preview or verify left behind are removed
  after a day.
