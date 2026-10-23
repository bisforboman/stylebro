# trial 48: the preview names the files each run undoes
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = '.Where(f => !stillDifferent.Contains(f))'
    Replace = '.Where(f => stillDifferent.Contains(f))'
    Tests = 'MigrationTests.Preview_NamesTheFilesEachRunUndoes'
}
