# trial 48: the preview passes on migrate's Not restored warning
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = ' || l.StartsWith(StyleCopSetup.NotRestoredWarning, StringComparison.Ordinal)'
    Replace = ''
    Tests = 'MigrationTests.PreviewOfMigrate_ReadsThePackageGlobalConfigFromTheRepositorysRestore'
}
