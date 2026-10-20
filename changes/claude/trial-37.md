### Fixed

- **bro format**, **check**, **init** and **migrate** no longer restore your projects while they read them. In a repository whose
  `Directory.Build.rsp` says `/Restore`, those reads restored each project for one target framework at a time and could leave
  a project's restore output for a framework it doesn't target, so `dotnet format` couldn't load it and its findings stayed
  for the build.
- **BRO1112** and **BRO1113** no longer remove a region your docs include as a
  [MarkdownSnippets](https://github.com/SimonCropp/MarkdownSnippets) snippet (`snippet: Name` in a markdown file, with an
  `mdsnippets.json` above the project). Removing it made MarkdownSnippets' build step fail with "Missing snippet".
