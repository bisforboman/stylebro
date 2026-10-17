# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Speed.cs'
    Find = 'l.TryGetProperty("level", out var level) && level.GetString() == "Warning"'
    Replace = 'false'
    Tests = 'MigrationTests.Format_SaysTheSkippedRestoresWarning_FromTheAssetsLogs'
}
