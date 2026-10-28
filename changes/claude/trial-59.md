### Fixed

- **`bro check --build`** no longer says "Not clean" over an error the untouched code has too when the build takes its
  version from git tags (MinVer, for example) and the error's text names an assembly version: the copy of HEAD it compares
  with now has the repository's tags, so it builds with the same version.
- **`bro format`** now says which file the StyleBro.Analyzers reference belongs in when it sits in a `Directory.Build.props`
  no project reads (a nearer one, `src/Directory.Build.props` say, doesn't import it), instead of only saying to add the
  package.
- **`bro format`** and **`bro check`** now point a kept rename's "first:" use (a string with the name) to the line it is
  on after formatting: it was the line from before the run's other fixes moved it.
- **BRO1103** and **BRO1106** no longer change a delegate lambda written the same way as an expression-tree lambda next to
  it (`Expression<Func<E, bool>> expected = x => E.Bar == x;` and `Func<E, bool> compiled = x => E.Bar == x;`): code that
  decompiles the delegate and compares it with the tree saw the pair differ.
