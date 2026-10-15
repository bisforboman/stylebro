# Trial 23: a later framework's run gets the files that compile differently there
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Where(f => earlier.Zip(compiled).All(e => !e.Second.Contains(f) || e.First.Key([f]) != view.Key([f])))'
    Replace = '.Where(f => earlier.Zip(compiled).All(e => !e.Second.Contains(f)))'
    Tests = 'MigrationTests.LaterFrameworks_GetOnlyTheFilesThatCompileDifferently'
}
