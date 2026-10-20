# Check coverage (2026-10-20): a clean run with gaps exits 3
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Coverage.cs'
    Find = 'code == 0 && gaps.Count > 0 ? NotVerifiedExitCode : code'
    Replace = 'code'
    Tests = 'FormatCoverageTests.Format_WithAGap'
}
