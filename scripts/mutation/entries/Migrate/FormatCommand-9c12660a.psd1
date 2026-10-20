# Trial 25: files of one project need no temporary solution
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (workspaces.Count > 1)'
    Replace = 'if (workspaces.Count > 0)'
    Tests = 'MigrationTests.Files_AFileLinkedThroughAProperty_SharesItsProjectsRun'
}
