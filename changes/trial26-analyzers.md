### Fixed

- **Renames (BRO1306, BRO1309)** keep a member or type whose name a XAML file (`.xaml`, `.axaml`) uses: a binding path
  (`{Binding title}`, `DisplayMemberPath="label"`), an event handler or `x:Class`. Bindings are resolved by name at
  run time, so the rename compiled but left the binding empty. The finding stays, with the reason.
- **BRO1001** keeps the order of the fields and properties of a partial type that generated code extends (a source
  generator's part): a generator such as Serde writes them in declaration order, so sorting changed the JSON.
- **BRO1519** no longer asks for a blank line before an accessor that starts with an attribute
  (`[MethodImpl(...)] set => ...`), the same as without one; CSharpier removed it again.
- **BRO1504** no longer asks for a blank line between an element's attributes and a comment right below them (also
  after BRO1602 turned a misplaced `///` there into `//`).
- **BRO1311** no longer reports tuple element names an implementation has to take from a public interface in the
  solution that isn't renamed (`stylebro_rename_public_api = false`); they were reported and then never fixed.
- **BRO1303-BRO1308**: a field that keeps its name (an attribute such as `[ThreadStatic]`) no longer blocks another
  field from getting that field's would-be new name.
