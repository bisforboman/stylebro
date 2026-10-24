# Trial 50: a check shows the Engine line
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '"Using ", "Engine: ", NoWhitespaceHeader'
    Replace = '"Using ", NoWhitespaceHeader'
    Tests = 'MigrationTests.Check_ShowsTheEngineLine'
}
