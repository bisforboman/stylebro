# trial 48: only a file back at its text of two runs ago is undone
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'after.TryGetValue(f, out var now) && was == now'
    Replace = 'after.TryGetValue(f, out var now)'
    Tests = 'FormatCoverageTests.Format_NamesTheFilesEachRunUndoes'
}
