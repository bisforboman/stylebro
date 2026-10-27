### Fixed

- **BRO1117** keeps the indentation of an argument or parameter that is on its own line below a comma when it moves the
  comma up. A list with a comma at the start of a line (`, token: ...`) no longer ends up with that item at column 0,
  depending on which fix `dotnet format` applied first.
- **bro format**, **bro check** and **bro init** no longer stop at their first project query when a project's build files
  call a property function on item metadata that only an editor-style evaluation trips over (DynamicData:
  `Regex.Replace(%(Filename), ...)` met a generated `.NETCoreApp,Version=v9.0.AssemblyAttributes.cs` once the tests had
  been built, MSB4186). They read the project the way a build does, as they already did for condition errors.
