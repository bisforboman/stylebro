# Trial 9: the recorded StyleCop version after the package swap
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'var recorded = version is null ? Migration.RecordedVersion(root) : null;'
    Replace = 'string? recorded = null;'
    Tests = 'MigrationTests.StyleCopVersion_'
}
