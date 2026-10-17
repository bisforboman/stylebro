# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = 'if (blocked.Count == 0)'
    Replace = 'if (blocked.Count >= 0)'
    Tests = 'MigrationTests.Migrate_WithALockedFile_WritesNothing'
}
