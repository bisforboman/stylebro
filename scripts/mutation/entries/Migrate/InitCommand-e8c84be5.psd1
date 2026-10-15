# Without a root solution, the repository's other solutions tell fixture folders apart
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'var elsewhere = solutions.Count == 0;'
    Replace = 'var elsewhere = false;'
    Tests = 'MigrationTests.Init_LeavesFixtureFoldersOutsideTheSolutions'
}
