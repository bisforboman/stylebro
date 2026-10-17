# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Speed.cs'
    Find = 'files.AddRange(Directory.EnumerateFiles(folder).Where(IsInput));'
    Replace = ''
    Tests = 'MigrationTests.Format_RestoreInputs_IncludeTheSettingsAboveTheRepository'
}
