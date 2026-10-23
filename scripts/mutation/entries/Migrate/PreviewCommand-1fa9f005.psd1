# trial 48: the preview's migrate sees the repository's restore output
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = '!restored ? CopiedRestoreFolders(source, copy) : [];'
    Replace = '!restored && false ? CopiedRestoreFolders(source, copy) : [];'
    Tests = 'MigrationTests.PreviewOfMigrate_ReadsThePackageGlobalConfigFromTheRepositorysRestore'
}
