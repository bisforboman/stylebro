# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'or "--no-restore" or RestoreOption or TimingsOption or'
    Replace = 'or "--no-restore" or'
    Tests = 'MigrationTests.Format_RestoreOptions_AreFormatsOwn'
}
