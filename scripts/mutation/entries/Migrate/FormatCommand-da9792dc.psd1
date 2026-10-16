# Trial 22: a run whose own check is clean is replayed
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (IsCleanCheck([(code, lines)], known))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Format_RunsAgainOnlyWhatItsOwnCheckFound'
}
