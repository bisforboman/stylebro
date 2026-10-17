### Changed

- **BRO1524** with BRO1520 off puts the missing line break of a split conditional on the same side as its other
  token's (`cond ?` / `a : b` becomes `cond ?` / `a :` / `b`), instead of the side
  `dotnet_style_operator_placement_when_wrapping` asks for, so a conditional no longer mixes both layouts in code where
  `init` turned BRO1520 off.
