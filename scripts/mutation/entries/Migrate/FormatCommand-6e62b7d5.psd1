# Check coverage (2026-10-20): gaps are said once, with the verdict
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'lines.RemoveAll(IsGapLine);'
    Replace = ''
    Tests = 'FormatCoverageTests.Verify_WithAGap'
}
