# Trial 22: a run with findings formats only their files
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = ': (false, code == NotCleanExitCode ? FindingFiles(lines, root) : null);'
    Replace = ': (false, null);'
    Tests = 'MigrationTests.Format_RunsAgainOnlyWhatItsOwnCheckFound'
}
