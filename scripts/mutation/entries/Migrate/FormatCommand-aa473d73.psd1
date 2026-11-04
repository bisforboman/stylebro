# The raw string files formatting findings are accounted for by the gap (079).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '!(IsFormattingLine(l) && IsInFiles(l, rawStringFiles))'
    Replace = 'false'
    Tests = 'FormatLineEndingsTests.Verify_AFileTheRawStringGuardKeptInTheCopy'
}
