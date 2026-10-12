# Trial 9, tool (2026-10-12): build output folders aren't searched for a solution
@{
    File = 'src/StyleBro.Migrate/BaselineCommand.cs'
    Find = '!SkippedFolders.Contains(Path.GetFileName(d), StringComparer.OrdinalIgnoreCase)'
    Replace = 'true'
    Tests = 'MigrationTests.Workspace_TheOnlySolutionOneFolderDown'
}
