# Trial 27 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '.Where(f => IsEfMigration(ReadOrNull(f) ?? string.Empty))'
    Replace = '.Where(f => IsEfMigration(File.ReadAllText(f)))'
    Tests = 'MigrationTests.Init_SkipsALockedCSharpFile'
}
