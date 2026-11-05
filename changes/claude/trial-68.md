### Fixed

- A project whose `<TargetFrameworks>` spans several lines (for example with a commented-out entry on its own line) is no longer left out of `bro format` and `bro check` without a word: they said Clean while the build reported findings in it. It now ends as `Not checked`, Not verified (exit 3), like any project `dotnet format` cannot load. (`--engine dotnet-format` still formats it.)
- `--report` without a folder or file (for example `bro check App.sln --report`) is refused with a message. `bro check` appends `--verify-no-changes` after your options, which `dotnet format` took for the report folder, so the "check" ran as a format and rewrote files.
