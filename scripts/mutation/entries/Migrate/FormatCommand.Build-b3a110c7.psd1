# Check coverage (2026-10-20): a build that failed like the untouched one checked only part of the code
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = 'else if (exitCode != 0 && found.Count == 0)'
    Replace = 'else if (false)'
    Tests = 'FormatCoverageTests.BuildCheck'
}
