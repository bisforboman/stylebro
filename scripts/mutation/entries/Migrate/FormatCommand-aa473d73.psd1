# The raw string files formatting findings are accounted for by the gap (079).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '!IsLineEndingLine(l) && !IsInFiles(l, rawStringFiles))'
    Replace = '!IsLineEndingLine(l))'
    Tests = 'FormatLineEndingsTests.Verify_AFileTheRawStringGuardKeptInTheCopy'
}
