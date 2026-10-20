# Check coverage (2026-10-20): verify with gaps lists them, not "Clean"
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'GapVerdict(gaps).ForEach(log);'
    Replace = 'log("Clean.");'
    Tests = 'FormatCoverageTests.Verify_WithAGap'
}
