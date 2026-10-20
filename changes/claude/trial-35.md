### Fixed

- **`bro format`** no longer stops at the restore when a multi-targeted project has a framework that needs a workload the
  machine doesn't have (`netstandard2.0;net8.0;net8.0-ios` without the iOS workload). It leaves that project alone, as it
  already did for single-target iOS, Android and browser projects, and formats the rest.
- **Multi-targeted projects**: the SDK's IDE0049 (`Int32` -> `int`) no longer writes `nint`/`nuint` for `IntPtr`/`UIntPtr`,
  or a keyword in a using alias (`using Handle = System.IntPtr;`), when one of the project's target frameworks compiles
  with an older C# (netstandard2.0 or .NET Framework without a `LangVersion`): the fix came from the newest framework and
  broke the older one's build (CS8370).
- **BRO1606** no longer breaks the doc comment when a filler summary names the type in `<c>`
  (`Creates a new instance of <c>Options</c>.` became `... class.</c>.`, a build error): the whole sentence is replaced.
- **BRO1304** and the other renames keep a nested type's name when a string names it the way reflection writes it
  (`"...Tests+PrivateInterface is not accessible"`), as they already did for `App.Shape` and `App.Shape, App`.
