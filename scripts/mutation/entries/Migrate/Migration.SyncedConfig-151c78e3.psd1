# Blank lines don't count (trial 44)
@{
    File = 'src/StyleBro.Migrate/Migration.SyncedConfig.cs'
    Find = '.Where(l => l.Length > 0).ToList();'
    Replace = '.ToList();'
    Tests = 'MigrationTests.PackageCopy'
}
