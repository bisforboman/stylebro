# Preset versions (2026-10-13): init leaves a StyleBroPreset the team set
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (existing.Contains("<StyleBroPreset>", StringComparison.Ordinal))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Init_PinsThePresetVersion'
}
