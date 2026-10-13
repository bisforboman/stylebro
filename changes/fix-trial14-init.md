### Changed

- **`stylebro-migrate init`** follows two more conventions:
  - **Spacing** that IDE0055 enforces: `if (x)` or `if(x)`, spaces inside parentheses (`if ( x )`, `( a + b )`,
    `( int )x`), after a cast (`(int) x`), inside call and parameter lists (`F( x )`). A clear form is written
    (`csharp_space_after_keywords_in_control_flow_statements`, `csharp_space_between_parentheses`, ...); mixed code turns
    IDE0055 off with a note, so `format` no longer rewrites `foreach( var x in y )` in a code base that mostly writes it
    that way.
  - **Blank lines between members** (**BRO1505**) and **above doc comments** (**BRO1513**): compact or mixed code turns
    the rule off, so `format` doesn't add blank lines all over a compact code base.

### Fixed

- **`stylebro-migrate init`** judges `end_of_line`, `charset` and `insert_final_newline` against the `.editorconfig` that
  actually decides each file (the nearest one that sets the key, as the compiler reads it), not only the root one, and
  writes its line into that file (a `# BEGIN stylebro-whitespace` block). Before, a `Source/.editorconfig` saying
  `insert_final_newline = false` overrode the root block's line, and `format` stripped the last line break from files the
  report said it would leave alone. A second `init` keeps these decisions.
- **`stylebro-migrate init`** accepts `--project <solution or project>`, like `format` and the preview.
- **`stylebro-migrate init`** run a second time writes its block lines in the same order.
- **`stylebro-migrate baseline`** skips the whitespace pass where IDE0055 is off (CSharpier, column alignment), like
  `format`: it no longer reports CSharpier's layout as formatting changes.
- **`stylebro-migrate format`** leaves projects a `.sln` lists but no configuration builds alone (they aren't restored, so
  StyleBro couldn't run there), and says so instead of warning "Without StyleBro".

### Upgrading

- Run `stylebro-migrate init --write` again in a repository with a nested `.editorconfig` that sets line endings, the
  charset or the final line break, or whose spacing differs from the SDK's defaults.
