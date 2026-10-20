# Check coverage (2026-10-20): the last run line says not everything was checked
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'gaps.Count > 0 ? ", but not everything was checked (below)." : '
    Replace = ''
    Tests = 'FormatCoverageTests.Format_WithAGap'
}
