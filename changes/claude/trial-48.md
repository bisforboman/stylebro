### Fixed

- `bro migrate` reads the global config a NuGet package adds to the build (`GlobalAnalyzerConfigFiles` in its build
  props), like the repository's own `.globalconfig` files. A repository whose StyleCop settings come from such a package
  had the rules that package turns off migrated as on, and the first `bro format` moved usings, added `this.` and sorted
  members in code StyleCop accepted. Without restore output, migrate says to run `dotnet restore` first.
- `bro format` names the files when each run undoes the one before (for example `dotnet format`'s own using sort, which
  flips `using static X;` and `using static X<T>;` on every run), instead of only saying it is still changing, and
  `bro check` says the same instead of "Run 'bro format'" when those files are all it would change; `bro preview` names
  them too.
- `bro preview` (and `--diff`) with NuGet's packages folder inside the repository (a `globalPackagesFolder` such as
  `%USERPROFILE%/...`, which isn't expanded outside Windows): the copy no longer takes half of each package (the files git
  doesn't ignore), which left target frameworks unloadable in the preview while `bro format` loaded them, and the
  packages' `.editorconfig` files are no longer listed as settings or put into the patch.
- `bro preview migrate` reads the repository's restore output, as `bro migrate` does, and shows migrate's warnings
  about projects that aren't restored.
