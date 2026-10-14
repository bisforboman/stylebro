### Changed

- **`format`**, its preview and `--verify-no-changes` are much faster on large multi-targeted repositories:
  - projects that compile a shared file with the same `#if` symbols, language version and nullable context (a polyfill
    every library links) share a run instead of getting one each;
  - a run whose files are all left to another formatter (CSharpier) skips the whitespace pass, and a run with none of
    them does all passes in one `dotnet format` call;
  - the check after a run that changed files counts the findings the fixes keep on purpose as clean, instead of running
    everything again;
  - `--verify-no-changes` right after a clean `format` knows the findings it kept and formats no copy; otherwise it
    formats only the files with findings, for their rules, in its temporary copy; and it shows its progress;
  - at most one `dotnet format` per processor runs at once.

### Fixed

- **BRO1311** no longer keeps a project's tuple element names because a tuple in another project with the same element
  names can't be renamed: each project's elements are judged on their own then.
