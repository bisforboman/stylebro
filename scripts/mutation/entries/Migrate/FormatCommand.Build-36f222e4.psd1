# Check coverage (2026-10-20): the build summary repeats each line
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = '.DistinctBy(x => x.Line, StringComparer.Ordinal)'
    Replace = ''
    Tests = 'FormatCoverageTests.BuildMessages'
}
