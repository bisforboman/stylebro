# Trial 22: the line names the frameworks without a run
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (gone.Count > 0)'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Format_RunsAFrameworkOnlyWhereItsCodeCompilesDifferently'
}
