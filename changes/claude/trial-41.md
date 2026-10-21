### Fixed

- **BRO1509** puts the attribute lists of a member it expands on their own lines
  (`[Parameter, EditorRequired] public void M() { }`), as the .NET formatter does for a declaration spanning several lines.
  Before, `dotnet format` moved them only on its next run, so one run wasn't enough.
