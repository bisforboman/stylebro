# Trial 55: the preview lists an 'Indentation kept' note for the same files once
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = '|| !notes.Any(n => Files(n) == Files(line));'
    Replace = '|| true;'
    Tests = 'MigrationTests.Preview_ListsAnIndentationNoteOnce'
}
