# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'if (Migration.ReadOrNull(other) is not { } text)'
    Replace = 'if (File.ReadAllText(other) is not { } text)'
    Tests = 'MigrationTests.ReadOrNull_SkipsALockedFile'
}
