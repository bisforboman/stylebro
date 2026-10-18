### Fixed

- **BRO1603** no longer checks the documentation of events and finalizers, like StyleCop's SA1629.
- **BRO1603** no longer adds a period after an inline element whose own text ends a sentence
  (`<paramref name="name">The name of the request.</paramref>`), or after a last word ending in a period and digits
  (`HTML 5.0`).
- **BRO1603** and the other documentation rules that edit text leave a comment with a bare `&` alone: it's malformed
  XML (the compiler reports CS1570), like a bare `<`.
- **BRO1308** no longer joins a name starting with `_` in the `_camelCase` style (`_signature_size` stays), like
  StyleCop's SA1310.
- **BRO1126** keeps how a type argument names a built-in type in a using alias: `System.Char` no longer becomes `char`,
  which IDE0049 reported under `dotnet_style_predefined_type_* = false`. Same as StyleCop.
- **BRO1303/BRO1306** rename a nested type's field when an enclosing type has a member with the new name, and a field
  named after its own type (`SimulatorEvent _simulatorEvent` -> `SimulatorEvent`); both were kept before.
- **BRO1309** keeps a static property only for a string that reaches it by name (`GetProperty("nbc")`), not for a URL or
  file name that contains the word.
- `bro format` lists a tuple element (**BRO1311**) kept because another element of its tuple type keeps
  its name as kept with that element, instead of repeating the other element's reason.
