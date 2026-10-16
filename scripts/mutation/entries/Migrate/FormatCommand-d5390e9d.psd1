# The workspace warnings name the run that had them.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var where = run.Length > 0 ? $" in its {run} run" : string.Empty;'
    Replace = 'var where = string.Empty;'
    Tests = 'MigrationTests.Format_ExplainsWorkspaceWarnings_WithTheCommandThatShowsThem'
}
