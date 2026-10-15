# An opt-out folder doesn't inherit the using order key
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'optOutFolders.Add(source);'
    Replace = ''
    Tests = 'MigrationTests.Init_OptOutFolder_GetsNoSortKey'
}
