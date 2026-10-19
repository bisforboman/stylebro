@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '            listed.TryAdd(project, info);'
    Replace = '            listed[project] = info;'
    Tests = 'MigrationTests.Format_AProjectTheWorkspaceDoesntList_LoosensItsReferences'
}
