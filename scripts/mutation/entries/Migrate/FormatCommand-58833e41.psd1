@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '            listed.TryAdd(project, info);'
    Replace = '            _ = info;'
    Tests = 'MigrationTests.Format_AProjectTheWorkspaceDoesntList_LoosensItsReferences'
}
