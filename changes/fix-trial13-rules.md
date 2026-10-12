### Changed

- **BRO1303/BRO1306** follow the SDK's naming rules (`dotnet_naming_rule`) for public, internal and protected fields
  too: the first rule that covers a field, in Roslyn's order, decides its casing. A rule for camelCase internal fields
  no longer gets them renamed to PascalCase; a style StyleBro doesn't write (a prefix) leaves the name alone.

### Fixed

- **BRO1151** no longer unwraps an array whose elements take their type from it (`new()`, `default`, `null`, lambdas,
  method groups, collection and switch expressions): `M([new() { Name = "a" }])` became `M(new() { ... })`, which
  doesn't compile.
- **BRO1123** and **BRO1115** no longer report types inside doc comment crefs (`Func{ValueTuple{int, string}}`,
  `Func{Nullable{int}}`), where tuple and `T?` syntax don't exist: the build warned while `dotnet format` had nothing
  to fix.
- **BRO1311** renames a tuple element only its own project can see (a private method's, a local's) even when code a
  `stylebro-migrate format` run doesn't load has a variable of that name; the reason for a kept rename now says
  "code this format run doesn't load" instead of blaming a project's target frameworks.
