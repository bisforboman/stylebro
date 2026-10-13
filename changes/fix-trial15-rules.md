### Fixed

- **BRO1519** no longer asks for a blank line after a `}` that ends a pattern or expression when the next line continues
  it (`?`, `??`, `?.`, a binary operator, `with`, `switch`, `as`, `when`, ...), nor between a property's accessors and
  its initializer. With CSharpier, the blank line before a conditional's `?` came back on every run.
- **BRO1303** renames drop a `this.` that only told the field apart from a parameter of the old name
  (`this.pet = pet` now becomes `_pet = pet`, not `this._pet = pet`), unless `dotnet_style_qualification_for_field`
  is `true`.
- **BRO1124** keeps `x.Item1` when the element is named `x` (a name inferred from another lambda's `x`): `x.x` was no
  clearer.
