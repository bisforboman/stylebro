# bro format converts old Mac line endings (2026-10-19)

## Question

Trial 33: taglib-sharp (`Ape/StreamHeader.cs`, `Ogg/PageHeader.cs`) and Scriban (`LiquidBuiltinsFunctions.cs`, every line
CR-only) have C# files with a CR that no LF follows. `bro format` left them out ("Not formatted: ... old Mac line endings
... Convert them to LF or CRLF, then run again": the SDK's formatter adds a line break to such a file on every run, so it
never settles), but then ended with "Clean" and exit 0, `bro check` said Clean too, and the TreatWarningsAsErrors build
failed on those files (taglib-sharp: 54 StyleBro errors per framework). What should format do?

## Choices

1. **Convert them** (owner's pick): format turns each lone CR outside string and character literals into the file's
   other line ending (or the one `end_of_line` sets), says so, and formats the files like any other. A lone CR is a line
   break to the compiler, so the code means the same.
2. Keep leaving them out, but end format and check with "Not clean" and a non-zero exit.
3. Both: convert, and say Not clean where a file can't be converted.

## Decision

Convert them. Files whose lone CRs sit inside literals keep today's handling (style fixes only, their literals' values
would change).
