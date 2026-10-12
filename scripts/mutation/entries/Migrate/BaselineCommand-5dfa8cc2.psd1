# Trial 12 (2026-10-12): Steeltoe, sourcegit
@{
    File = 'src/StyleBro.Migrate/BaselineCommand.cs'
    Find = '&& filters.Count > 1 &&'
    Replace = '&& filters.Count > 0 &&'
    Tests = 'MigrationTests.Workspace_IsNeverPickedSilentlyFromSeveral'
}
