### Changed

- **BRO1303, BRO1306** (field renames) are kept only for a string where code reaches a member by name
  (`GetField("_count")`, `GetProperty(...)` on a type, `Type.GetType(...)`, `[MemberData("...")]`, an attribute's
  `Name = "..."`). Before, the field's name as any string anywhere in the repository kept the rename (an exception's
  parameter name, a JSON key), so most renames in large repositories were left as warnings.
- **BRO1303, BRO1306, BRO1307, BRO1308, BRO1310** leave interop names alone: SCREAMING_CASE constants and
  `static readonly` fields (`S_OK`, `MAX_PATH`), COM GUID fields (`IID_IContextMenu`), and the fields of types with
  `[StructLayout]`, `[ComImport]`, `[UnmanagedFunctionPointer]` or `[DllImport]`/`[LibraryImport]` methods.
- **BRO1149** puts a merged condition on a line of its own when the outer condition is already wrapped.

### Fixed

- `stylebro-migrate format` no longer fails before formatting on a repository with a lone surrogate in a string
  (`"\uD800"`).
- `stylebro-migrate format --files` keeps the field renames the whole-repository run keeps (a test project not loaded
  that reads the field by reflection).
- **BRO1527** no longer joins `{ get; set; }` onto a declaration whose type spans lines; with IDE0055 on, the two
  changed the file back and forth on every run.
- **BRO1126** sorts the usings when `dotnet format` sorts them, so a qualified using lands in place in the same run.
- **BRO1149** is no longer held back by a `#region` or `#endregion` next to the `if`, which made it fix only on a
  second run after the regions were removed.
