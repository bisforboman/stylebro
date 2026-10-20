# Check coverage (2026-10-20): --build needs a whole check
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '(!args.Contains("--verify-no-changes") || args.Contains(FilesOption))'
    Replace = '!args.Contains("--verify-no-changes")'
    Tests = 'FormatCoverageTests.Build_GoesWithAWholeCheck'
}
