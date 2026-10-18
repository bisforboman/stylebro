### New

- **The Microsoft style:** `<StyleBroStyle>microsoft</StyleBroStyle>` puts dotnet/runtime's coding style on top of the
  preset: `_camelCase` and `s_camelCase` fields, fields first, braces optional on one-line bodies, no `this.`, and the
  StyleCop-derived rules runtime has off turned off. `bro init --style microsoft --write` sets it up with the
  built-in rules it needs; your code's own conventions still win. Plain `init` suggests it when your code looks like
  runtime's. An unknown style fails the build (STYLEBRO003), a style with `<StyleBroPreset>none</StyleBroPreset>` warns
  (STYLEBRO004). Details: [Styles](https://bisforboman.github.io/stylebro/styles/).

### Changed

- The preset's global configs moved from `global_level = -1` to `-2` (a style sits at `-1`). Your own `.editorconfig` and
  `.globalconfig` still win over both.
