# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = 'using var stream = File.Open(file, FileMode.Open, FileAccess.Write, FileShare.Read);'
    Replace = 'using var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);'
    Tests = 'MigrationTests.Migrate_WithALockedFile_WritesNothing'
}
