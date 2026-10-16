# Trial 25: findings only in files an earlier run changed: nothing runs
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '            ? (false, [])'
    Replace = '            ? (false, null)'
    Tests = 'MigrationTests.Format_RunsAgainOnlyWhatItsOwnCheckFound'
}
