# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Speed.cs'
    Find = 'is "nuget.config" or "global.json"'
    Replace = 'is "global.json"'
    Tests = 'MigrationTests.Format_RestoreInputs_IncludeTheSettingsAboveTheRepository'
}
