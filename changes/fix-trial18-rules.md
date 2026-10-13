### Fixed

- **BRO1405** and **BRO1410** no longer delete a comment inside the parentheses they remove: `(x + 1 /* one */)` becomes
  `x + 1 /* one */`, and parentheses with a `//` comment and a line break inside them are left alone.
- **BRO1104** and **BRO1123** leave code with a comment inside (`new Guid(/* none */)`, `ValueTuple<int, /* id */ int>`)
  alone instead of dropping the comment.
- **BRO1603** no longer adds a period after a quoted char or string literal (`Default is ',';`).
