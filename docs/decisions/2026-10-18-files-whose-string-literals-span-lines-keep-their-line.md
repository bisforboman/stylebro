# Files whose string literals span lines keep their line ending (2026-10-18)

## Question

Trial 32 (RazorLight): `bro init` wrote `end_of_line = lf` (134 of 153 C# files are LF), and `bro format` converted the 19
CRLF files. Two of them hold multi-line verbatim strings, whose value changed from `\r\n` to `\n`; two tests that compare
rendered output with those strings failed. How should StyleBro avoid that behaviour change?

## Choices

1. **Exempt those files** (owner's pick): init still writes `end_of_line`, plus a section for each file of the other
   ending that has a multi-line string literal; the other files are converted. The same for migrate's ending line.
2. Don't write `end_of_line` when any file of the other ending has a multi-line string literal, and say why.
3. Convert anyway and list the files in the report.

## Decision

`init` and `migrate` add a section per such file (its path, special characters escaped) with the file's own ending, and
the report names the files. The option proposed `end_of_line = unset` for the section; probed, `dotnet format` then
writes the OS's ending into the file (a CRLF file with a verbatim string became LF on Linux), so the section names the
file's own ending instead. Files git converts on checkout and files with both endings are left out.
