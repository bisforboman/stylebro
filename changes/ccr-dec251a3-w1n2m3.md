### Fixed

- **`bro format` and `bro check`** run their default in-process engine on the .NET 11 SDK again. On SDK 11 they fell back to plain `dotnet format`, which is slower and can format a little differently.

### Changed

- **BRO1001** orders explicit interface events with the public members, and checks records and record structs fully: constants before fields, static before instance members, `readonly` fields first.
- **BRO1104** writes `nint.Zero` (and `nuint.Zero`) for `new nint()` where every target framework has it (.NET 7 or later with C# 11), and `default(nint)` elsewhere.
- **BRO1106** also reports `$""` and empty raw string literals.
- **BRO1306** also reports lower-case `protected readonly` and `private protected readonly` fields, and renames them to PascalCase.
- **BRO1311** also checks the names written in tuple literals (`(lower: 1, upper: 2)`), and renames their uses with them.
- **BRO1401** adds a trailing comma to multi-line collection expressions too.
- **BRO1406** also wants parentheses around a mix of `&`, `|`, `^` or `>>>` with other operators; **BRO1407** around `&&` or `||` next to `??`.
- **BRO1505** lets two single-line properties sit together even when one is documented.
- **BRO1510** also reports an expression-bodied accessor (`get => x;`) next to a multi-line accessor.
- **BRO1603** passes over a closing element whose own text ends in `?` or `!`, and puts the period before it.
