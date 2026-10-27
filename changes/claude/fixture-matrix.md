### Fixed

- **`bro check --build`** no longer says "Not clean" in a shallow clone of a repository that versions from git history
  (Nerdbank.GitVersioning and the like): the copy of HEAD it compares with now has the repository's history, so its builds
  fail the same way and those errors aren't counted as new.
- **`bro format`** no longer deletes C# members in a solution that also has a Visual Basic project: the fixes of rules
  both languages have (modifier order, IDE0036) ran the Visual Basic fix on C# code too.
- With `--engine dotnet-format`, C# projects that reference a Visual Basic project are now left out and named as not
  checked, because `dotnet format` itself has the same bug there; the default engine checks them.
- **`bro check`** no longer lists findings kept on purpose that `bro format` has already put into the baseline (with the
  default engine on .NET SDK 10.0.4xx).
