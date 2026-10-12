# Trial 10, tool (2026-10-12): a .slnf's projects come from its JSON ('dotnet sln list' fails on it)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var filter = IsSolutionFilter(workspacePath) ? ReadSolutionFilter(workspacePath) : default;'
    Replace = 'var filter = default((string? Solution, string[] Projects));'
    Tests = 'MigrationTests.Format_AWorkspaceWithoutProjects_'
}
