# Only .props/.targets count (a build/notes.txt naming it does not)
@{
    File = 'src/StyleBro.Migrate/Migration.SyncedConfig.cs'
    Find = '(n.EndsWith(".props", StringComparison.OrdinalIgnoreCase) || n.EndsWith(".targets", StringComparison.OrdinalIgnoreCase))'
    Replace = 'true'
    Tests = 'MigrationTests.PackageCopy_FindsAnOlderCopy'
}
