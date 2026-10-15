# Only a private-use character where a directive's # would be is damage
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'text.Any(c => c is >= '''' and <= '''') && MaskedLine.IsMatch(text)'
    Replace = 'text.Any(c => c is >= '''' and <= '''')'
    Tests = 'MigrationTests.Format_PutsBackWhatAKilledRun'
}
