# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'var after = rest.Trim().Length == 0 ? string.Empty :'
    Replace = 'var after = true ? string.Empty :'
    Tests = 'MigrationTests.SetPreset_WritesWellFormedXml'
}
