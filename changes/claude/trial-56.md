### Fixed

- **BRO1117** keeps the indentation of an argument or parameter that is on its own line below a comma when it moves the
  comma up. A list with a comma at the start of a line (`, token: ...`) no longer ends up with that item at column 0,
  depending on which fix `dotnet format` applied first.
