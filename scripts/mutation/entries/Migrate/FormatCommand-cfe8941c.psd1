# format --verify-no-changes: whitespace findings that only swap the OS's line ending don't print when the copy formats clean (#133)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'lines.Where(l => !IsKeptLine(l, kept) && !IsFormattingLine(l)).ToList()'
    Replace = 'lines.Where(l => !IsKeptLine(l, kept)).ToList()'
    Tests = 'FormatLineEndingsTests.Verify_OnlyLineEndingsDiffer_IsClean'
}
