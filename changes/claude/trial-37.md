### Fixed

- **bro format**, **check**, **init** and **migrate** no longer restore your projects while they read them. In a repository whose
  `Directory.Build.rsp` says `/Restore`, those reads restored each project for one target framework at a time and could leave
  a project's restore output for a framework it doesn't target, so `dotnet format` couldn't load it and its findings stayed
  for the build.
