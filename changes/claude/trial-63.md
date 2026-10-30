### Fixed

- **BRO1126** keeps the `?` of a nullable reference type argument when it qualifies a using alias
  (`KeyValuePair<string, object?>` no longer becomes `KeyValuePair<string, object>`, which broke the build with CS8619).
