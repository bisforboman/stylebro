# Only the build folders count (a tools/ targets file naming it does not)
@{
    File = 'src/StyleBro.Migrate/Migration.SyncedConfig.cs'
    Find = 'n.StartsWith("build", StringComparison.OrdinalIgnoreCase) && n.Contains('
    Replace = 'true || n.Contains('
    Tests = 'MigrationTests.PackageCopy_FindsAnOlderCopy'
}
