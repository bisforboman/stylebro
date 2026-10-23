# Format names a run that undoes the run before (2026-10-23)

## Question

Trial 48, Pidgin: a package's global config sets `dotnet_sort_system_directives_first = true`, and the SDK's style pass
then flips `using static Pidgin.Parser;` and `using static Pidgin.Parser<char>;` on every run in 5 files. Plain
`dotnet format` on the untouched repository does the same, without StyleBro. `bro format` ended "Not clean: still changing
after 3 runs ... please report it" (exit 2), and check said formatting would change 5 files. Should format recognise such
an undo loop?

## Choices

1. **Name it, stay Not clean** (owner's pick): when a run leaves a file as it was two runs earlier, format names those files
   and says each run undoes the one before (here `dotnet format`'s own using sort) instead of "please report it"; the exit
   code stays 2.
2. Leave it: the current message is accurate enough; record the SDK issue in the notes only.

## Decision

Name it, stay Not clean.
