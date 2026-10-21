### Fixed

- **`bro format`/`bro check`** no longer load the projects a solution lists but never builds (no build configuration
  includes them). They are left alone as before, but their load failures no longer appear as "Not checked" gaps.
- **Git submodules**: when a solution builds a project that lives in a git submodule (MailKit's solution and its MimeKit
  submodule), the build no longer reports StyleBro's findings in that project. `bro format` leaves submodules alone, so
  they could never be fixed there. Building the submodule's own solution or project still reports them.
