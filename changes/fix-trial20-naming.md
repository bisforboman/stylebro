### Fixed

- **BRO1301-BRO1314** leave names where the code turns the SDK's naming rule off: after `#pragma warning disable IDE1006`
  or with `[SuppressMessage("Style", "IDE1006")]` on the symbol or what contains it (a file copied from another project
  keeps its names).
- **BRO1309** (and the other renaming rules for properties, events, methods, parameters, locals, type parameters and
  namespaces) no longer renames a symbol to a name your enforced `dotnet_naming_rule` rejects: `private int _format { get;
  set; }` stays when your rule asks for `_camelCase` private properties, instead of becoming an IDE1006 build error.
- **BRO1311** renames element uses inside a tuple literal that is returned or assigned as another tuple type
  (`return (false, map.First().documentName);`); before, the rename was dropped and the warning stayed.
- **BRO1311** renames the element names of one tuple type all or none: it no longer leaves `(int Total, int max)` when
  `max` has to keep its name.
