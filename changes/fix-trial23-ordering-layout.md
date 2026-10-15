### Changed

- **BRO1001** sorts like StyleCop in two more places: an explicit event implementation (`event EventHandler IServer.E
  { ... }`) counts as private, not public, and in a record only kind and accessibility count (StyleCop doesn't check
  constants, statics or readonly fields there). StyleCop-clean code no longer gets reordered after migrating.
- **BRO1002** inserts its space before tabs after `//` instead of replacing them, so commented-out code indented with
  tabs keeps its layout.
- **BRO1504** no longer asks for a blank line above a comment between two parts of one expression (`ok ? value`,
  `// why`, `: throw ...`).

### Fixed

- **BRO1151** leaves the array in a project that targets frameworks before and from .NET 9 (`net8.0;net10.0`) when the
  call goes to a library: on .NET 9 and later, `string.Join`, `string.Format` or `Task.WhenAll` with the elements
  bind to a `params ReadOnlySpan<T>` overload instead. In any multi-targeted project it also leaves calls to a method
  whose type has `#if` in it, where an overload can hide.
- **BRO1508** leaves a one-line block inside a one-line block whose rule is off (`int M() { foreach (...) { ... }
  return s; }` with **BRO1509** off). Expanding it alone took a second `dotnet format` run and put two statements on
  one line.
- **BRO1504**'s fix removes the blank line below the comment when **BRO1506** is on, so a comment below a `}` with a
  blank line under it ends up the same whichever fix runs first.
- **BRO1505** puts its blank line below an `#endif`, not above the `#else` or `#endif` before the element.
