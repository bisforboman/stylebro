# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = 'blocked.Add(relative);'
    Replace = ''
    Tests = 'MigrationTests.Migrate_WithALockedFile_WritesNothing'
}
