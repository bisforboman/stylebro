### Fixed

- A project whose `<TargetFrameworks>` spans several lines (for example with a commented-out entry on its own line) is no longer left out of `bro format` and `bro check` without a word: they said Clean while the build reported findings in it. `bro format` now formats it (white space in the list is cut out for its runs), and if a project still can't load, the message with a line break in it is read and the project ends as `Not checked`, Not verified (exit 3).
- `--report` without a folder or file (for example `bro check App.sln --report`) is refused with a message. `bro check` appends `--verify-no-changes` after your options, which `dotnet format` took for the report folder, so the "check" ran as a format and rewrote files.
