### Fixed

- **Naming rules (BRO1303, BRO1306, BRO1309)**: a name in a checked-in snapshot or approval file (`*.verified.*`,
  `*.received.*`, `*.approved.*`) keeps it, like a name in a string: renaming a DTO's property failed Verify's snapshot
  tests. `stylebro-migrate format` lists such findings as kept, with the file.
- **Naming rules**: a member used through `dynamic` (`((dynamic)x).Field`) keeps its name; renaming it compiled but failed
  at run time.
- **BRO1306**: a public or internal field of a private class is now also kept when its name is inside a string (a script
  engine reading `host.ByteField`).
- **BRO1306**: a private `static readonly` field is no longer renamed against the repository's own naming rule when the
  SDK's naming rule order gives it a `_` prefix (the renamed field got IDE1006).
- **BRO1309**: a class with a snake_case property (`line_count`) keeps all its property names, instead of renaming the
  others and mixing casings.
- **Naming rules**: a name used from Visual Basic code keeps it, with the reason "Visual Basic code uses the name"
  (StyleBro doesn't edit VB code); it said "the new name would mean something else" before.
