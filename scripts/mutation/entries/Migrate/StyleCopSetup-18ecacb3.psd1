# Trial 25 (2026-10-16): migrate/init
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'var settingsRecorded = json is null &&'
    Replace = 'var settingsRecorded = false &&'
    Tests = 'MigrationTests.Migrate_KeepsStyleCopJsonsSettings'
}
