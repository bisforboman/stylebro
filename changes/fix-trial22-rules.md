### Changed

- **BRO1508** and **BRO1509** leave a one-line block with a comment after its `}` on its line
  (`catch (IOException) { } // slopwatch-ignore: ...`): expanded, the comment ended up on the `}` line, and tools
  that read a suppression from the statement's line reported the code again.
- **BRO1604** accepts `Gets or sets` on a property with `get` and `init` (it no longer rewrites it to
  `Gets or initializes`).

### Fixed

- The rename fixes (**BRO1301**-**BRO1309**, **BRO1409**) work in a project that references a source generator project
  that isn't built yet. Before, `dotnet format` failed with "Unexpected value 'UnresolvedAnalyzerReference'" and
  renamed nothing.
- **BRO1123** no longer rewrites the `ValueTuple<A, B>` a pattern tests for (`x is ValueTuple<T, T> pair`,
  `case ValueTuple<A, B> p:`): written as `(T, T)`, it is a different pattern and didn't compile.
- **BRO1137** keeps a lambda's last `return;` when the lambda can't reach its end without it
  (`_ => { throw new X(); return; }`): removed, the call could become ambiguous between overloads.
- **BRO1303** and **BRO1306** no longer skip a field because a base class has a private member with the new name. The
  build reported such fields and `dotnet format` didn't fix them.
- **BRO1515** and **BRO1602** add the blank line **BRO1504** wants above a `//` comment that now follows a `}`, so one
  `dotnet format` run is enough.
- **BRO1606** writes a generic class link as `Cache{TKey, TValue}`, with a space after the comma.
