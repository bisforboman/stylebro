# Trial 13 (2026-10-13): conditional StyleCop references, leftover StyleCop settings, report fixes
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '&& folders[^1].StartsWith(folders[^2] + ".", StringComparison.Ordinal)'
    Replace = ''
    Tests = 'MigrationTests.Suppressions_SkipGeneratedCode_'
}
