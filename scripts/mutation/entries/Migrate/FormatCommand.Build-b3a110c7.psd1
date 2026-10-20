# Check coverage (2026-10-20): a build that failed like the untouched one checked only part of the code
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = 'else if (exitCode != 0 && found.Count == 0 && (current.Any(m => m.Error && !m.InSource) || !current.Any(m => m.Error)))'
    Replace = 'else if (false)'
    Tests = 'FormatCoverageTests.BuildCheck'
}
