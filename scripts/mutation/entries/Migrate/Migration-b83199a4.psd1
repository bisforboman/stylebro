# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '            || IsSnapshot(name)'
    Replace = ''
    Tests = 'MigrationTests.Init_CountsOnlyCompiledFiles'
}
