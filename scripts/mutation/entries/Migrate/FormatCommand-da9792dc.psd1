# Trial 22: a run whose own check is clean is replayed
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = ': IsCleanCheck([(code, lines)], known) ? (true, null)'
    Replace = ': false ? (true, null)'
    Tests = 'MigrationTests.Format_RunsAgainOnlyWhatItsOwnCheckFound'
}
