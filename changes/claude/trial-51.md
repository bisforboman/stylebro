### Fixed

- **bro format/check**: in a multi-targeted repository, a project that references the library but doesn't target the
  framework being formatted (a net8.0;net462 tool next to net472 tests) is no longer forced onto that framework. The
  projects that reference it loaded again, instead of a "Not verified" for code the build compiles fine.
- **bro format/check**: test projects whose libraries reference each other (multi-targeted, an old framework such as
  netstandard1.3 first) are formatted again. Each per-framework run now also loads the projects they reference from the
  solution, so `dotnet format` no longer skips the tests ("references didn't load") and leaves findings for the build.
- **bro preview**: files a repository committed with CRLF line endings although `.gitattributes` normalizes them
  (`* text=auto`) keep their endings in the preview's copy. The preview then shows what `bro init` and `bro format` do in
  the repository, instead of converting them to LF (which changed the value of multi-line string literals).
- **bro preview** of a folder or project (`bro preview format src/Lib/Lib.csproj`) adds StyleBro.Analyzers to the
  `Directory.Build.props` that project already imports (and its version to the `Directory.Packages.props` above it), instead
  of creating a new `Directory.Build.props` in the project's folder that hid the one above (its warnings-as-errors and other
  settings were missing from the preview).
