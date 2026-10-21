### Fixed

- **`bro format`/`bro check`** no longer load the projects a solution lists but never builds (no build configuration
  includes them). They are left alone as before, but their load failures no longer appear as "Not checked" gaps.
- **Git submodules**: when a solution builds a project that lives in a git submodule (MailKit's solution and its MimeKit
  submodule), the build no longer reports StyleBro's findings in that project. `bro format` leaves submodules alone, so
  they could never be fixed there. Building the submodule's own solution or project still reports them.
- **`bro migrate`**: StyleCop's `blankLinesBetweenUsingGroups: require` no longer turns into
  `dotnet_separate_import_directive_groups = true` when the code doesn't put blank lines between root namespaces' usings.
  StyleCop's using groups aren't the SDK's, and the first `bro format` added blank lines to hundreds of StyleCop-clean
  files. The report says which value was written and why.
