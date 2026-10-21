# Trial 40: a brace pattern keeps only the folder's alternative
@{
    File = 'src/StyleBro.Migrate/Migration.SyncedConfig.cs'
    Find = 'alternatives.Contains(folder, StringComparer.Ordinal) ?'
    Replace = 'true ?'
    Tests = 'StyleBro.Tests.MigrationTests.Rebase_KeepsTheFoldersOwnSections'
}
