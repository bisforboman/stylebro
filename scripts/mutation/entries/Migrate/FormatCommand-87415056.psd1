# IsLineEndingLine: a formatting finding in an LF file is not line-ending noise (079).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'IsFormattingLine(line) && FindingPath(line) is { } path && HasOtherLineEnding(path)'
    Replace = 'IsFormattingLine(line)'
    Tests = 'FormatLineEndingsTests.Verify_AFormattingFindingInAnLfFile'
}
