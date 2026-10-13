# Trial 9: the recorded StyleCop version after the package swap
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'var recordedVersion = Migration.RecordedVersion(root);'
    Replace = 'string? recordedVersion = null;'
    Tests = 'MigrationTests.StyleCopVersion_'
}
