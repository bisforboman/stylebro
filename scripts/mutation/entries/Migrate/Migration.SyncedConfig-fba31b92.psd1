# Trial 45: a package whose build files name its editorconfig counts
@{
    File = 'src/StyleBro.Migrate/Migration.SyncedConfig.cs'
    Find = '|| BuildFilesName(folder!, names, file))'
    Replace = ')'
    Tests = 'MigrationTests.PackageCopy_FindsAnOlderCopy'
}
