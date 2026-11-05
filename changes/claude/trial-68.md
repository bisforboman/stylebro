### Fixed

- A project whose `<TargetFrameworks>` spans several lines (for example with a commented-out entry on its own line) is no longer left out of `bro format` and `bro check` without a word: they said Clean while the build reported findings in it. It now ends as `Not checked`, Not verified (exit 3), like any project `dotnet format` cannot load. (`--engine dotnet-format` still formats it.)
