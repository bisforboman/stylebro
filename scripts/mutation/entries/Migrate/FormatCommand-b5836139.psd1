# Trial 22: a run with findings formats only their files
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (code != NotCleanExitCode || FindingFiles(lines, root) is not { } files)'
    Replace = 'if (code != NotCleanExitCode || FindingFiles(lines, root) is not { Count: > 1000 } files)'
    Tests = 'MigrationTests.Format_RunsAgainOnlyWhatItsOwnCheckFound'
}
