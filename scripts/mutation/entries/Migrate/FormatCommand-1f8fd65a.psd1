# Trial 23: baseline entries whose line is gone go
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (Fingerprints(Path.Combine(folder, key.Path))?.Contains(key.Fingerprint) == true)'
    Replace = 'if (true)'
    Tests = 'MigrationTests.Format_BaselinesTheFindingsItKeeps'
}
