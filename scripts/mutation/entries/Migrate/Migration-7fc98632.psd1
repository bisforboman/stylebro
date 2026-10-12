# Trial 13 (2026-10-13): conditional StyleCop references, leftover StyleCop settings, report fixes
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'folders.Count >= 3 && folders[^3].Equals("generated", StringComparison.OrdinalIgnoreCase)'
    Replace = 'folders.Count >= 3'
    Tests = 'MigrationTests.Suppressions_SkipGeneratedCode_'
}
