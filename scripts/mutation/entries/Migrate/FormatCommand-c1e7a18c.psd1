# Check coverage (2026-10-20): format with gaps exits 3
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'return WithGaps(0, gaps);'
    Replace = 'return 0;'
    Tests = 'FormatCoverageTests.Format_WithAGap'
}
