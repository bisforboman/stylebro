### New

- **`bro format`** also applies xunit.analyzers' assertion fixes in repositories that use xunit, such as
  `Assert.Equal(true, x)` -> `Assert.True(x)` and `Assert.Equal(0, list.Count)` -> `Assert.Empty(list)`, where the
  repository leaves those rules at warning or error. Fixes that change what a test does stay out. `bro check`, `bro
  preview format` and `bro baseline` cover the same rules. See [xUnit's fixes](https://bisforboman.github.io/stylebro/xunit/).

### Fixed

- **BRO1602** also removes the empty `//` lines its conversion leaves at the start or end of a comment (BRO1120's), so
  `bro format` no longer needs a second run for them.
- **Naming rules** (BRO1303-BRO1309) rename theory data that `[MemberData(nameof(...))]` names, instead of keeping the
  name: the rename updates the `nameof` too.
