### Fixed

- BRO1514-BRO1516 put the braces of a nested statement on its owner's line (`for (...) if (x) y;`) at the wrong
  indentation, so `dotnet format` needed a second run to fix it.
- BRO1508/BRO1509, with `csharp_preserve_single_line_statements = false`: expanding a one-line body now also lays out
  nested statements (`while (n > 0) if (x) return n;`, `using (s) lock (gate) ...`) and a one-line `switch` the way the
  SDK's formatter does, so one `dotnet format` run is enough.
- BRO1601 below a `//` comment and a blank line: the blank line now goes whether BRO1601 or BRO1506 runs first (it
  stayed when BRO1601 ran first).
- BRO1603 no longer adds a period after text ending in `,` or `;` (`1033 => "en",` became `"en",.`).
- BRO1609 no longer unwraps a `<placeholder>` at the top level of a comment, which left its text outside any element.
