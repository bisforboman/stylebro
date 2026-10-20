# Check coverage (2026-10-20): a project skipped for some frameworks names them
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Coverage.cs'
    Find = '!frameworks.TryGetValue(s.Key, out var all) || all.All(s.Value.Contains)'
    Replace = '!frameworks.TryGetValue(s.Key, out var all)'
    Tests = 'FormatCoverageTests.SkippedProjects'
}
