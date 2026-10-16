# Trial 24: a '#' starting the first line of '$"""' + line break (the break is the start token's)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'Where(t => t.Text.Contains(''#'', StringComparison.Ordinal)))'
    Replace = 'Where(t => t.Text.Contains(''#'', StringComparison.Ordinal) && t.Text.Contains(''\n'', StringComparison.Ordinal)))'
    Tests = 'MigrationTests.Format_ProtectsTheFirstLineOfEveryStringShape'
}
