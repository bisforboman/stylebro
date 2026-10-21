### Fixed

- **BRO1509** puts the attribute lists of a member it expands on their own lines
  (`[Parameter, EditorRequired] public void M() { }`), as the .NET formatter does for a declaration spanning several lines.
  Before, `dotnet format` moved them only on its next run, so one run wasn't enough.
- **BRO1139** gives the same result whatever order `dotnet format` applies the fixes in when it turns `else { if (x) y; }`
  into `else if (x) y;` right before another statement: the blank line BRO1519 wants after the removed `}` is kept either
  way (it depended on the order, so two machines could format the same code differently).
