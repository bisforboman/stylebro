# Check coverage (2026-10-20): an unexplained failure counts only where the untouched build succeeded
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = '(untouched is null || (before is { } u && u.ExitCode == 0))'
    Replace = 'true'
    Tests = 'FormatCoverageTests.BuildCheck'
}
