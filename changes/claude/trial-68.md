### Fixed

- A project whose `<TargetFrameworks>` spans several lines (for example with a commented-out entry on its own line) is no longer skipped without a word: `dotnet format` could not load it, `bro format` and `bro check` said Clean, and the build still reported findings in it. It now ends as `Not checked`, Not verified (exit 3), like any project `dotnet format` cannot load.
