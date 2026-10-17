### Fixed

- **BRO1604** compares a property summary's first words ignoring case, like StyleCop: `Gets or Sets the key` is right
  (it became `Gets or sets or Sets the key`), and a lower-case `gets the count` is replaced instead of prefixed.
- **BRO1605** no longer asks for `Gets` when an `internal` setter belongs to a property only its assembly sees (an
  `internal` class's): like StyleCop's SA1623, it says `Gets or sets`, so the two no longer contradict each other.
- Renames keep names a XAML file uses as element or attribute names from the project's own namespaces:
  `<local:BoolConverter/>`, `local:TagBehavior.Key="main"`, `<local:Badge Caption="hi"/>` (a renamed type or
  property there broke the build). Letters in path markup (`Data="M0,0 z"`) no longer keep a name.
- **BRO1616** leaves a `///` comment that documents nothing (below an attribute) to BRO1602, so `dotnet format` gives
  the same result every time.
- **BRO1305** drops a leading underscore first: `_TVertex` becomes `TVertex`, not `TTVertex`.
- **BRO1306** keeps a field whose new name would differ only in case from another member (`_horizontalgap` next to
  `HorizontalGap`).
- The assembly attributes the build writes under `obj/` no longer count as strings that keep a name.
