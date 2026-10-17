# Trial 10, tool (2026-10-12): a workspace without C# projects stops
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '        if (projects.Count == 0)
        {
            error('
    Replace = '        if (projects.Count < 0)
        {
            error('
    Tests = 'MigrationTests.Format_AWorkspaceWithoutProjects_'
}
