### Fixed

- **`bro format` settles on multi-targeted code where a `#region` sits inside `#if` code some target frameworks leave
  out.** The SDK's formatter indented such a `#endregion` with the code in the frameworks that compile it and moved it in
  the others, so each run undid the one before and format ended "still changing after 3 runs" (Newtonsoft.Json). Their
  indentation is now left as it is (`Indentation kept: ...`), like an if/else body that `#if` decides.
- **`bro format` settles on comments inside an if/else body that `#if` decides.** It kept such a body's indentation as it
  was but not that of a `//` line above one of its statements, which each target framework indented its own way.
