### Changed

- **`bro init`** sets `stylebro_allow_adjacent_single_line_members = true` when your code has three or more
  neighbouring one-line members without a blank line between them (`public void Clear() => items.Clear();` right above
  another one), so BRO1505 no longer separates them.

### Fixed

- **BRO1126** keeps the `?` of a nullable reference type argument when it qualifies a using alias
  (`KeyValuePair<string, object?>` no longer becomes `KeyValuePair<string, object>`, which broke the build with CS8619).
