# Trial 22: a run whose files an earlier run changed runs as usual
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '        touched ? (false, null)'
    Replace = '        false ? (false, null)'
    Tests = 'MigrationTests.Format_RunsAgainOnlyWhatItsOwnCheckFound'
}
