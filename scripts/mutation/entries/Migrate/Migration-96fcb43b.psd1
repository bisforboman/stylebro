# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = ': project.Groups[1].Length > 0 ?'
    Replace = ': false ?'
    Tests = 'MigrationTests.SetPreset_WritesWellFormedXml'
}
