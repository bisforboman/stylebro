# Trial 40: a section for another folder matches nothing in this one
@{
    File = 'src/StyleBro.Migrate/Migration.SyncedConfig.cs'
    Find = 'pattern.StartsWith(folder + "/", StringComparison.Ordinal) ? pattern.Substring(folder.Length + 1) : null'
    Replace = 'pattern'
    Tests = 'StyleBro.Tests.MigrationTests.Rebase_KeepsTheFoldersOwnSections'
}
