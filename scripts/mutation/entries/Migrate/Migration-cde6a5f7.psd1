# Trial 52/53: the converted-minority notes only when IDE0055 formats
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (formattingOn)
        {
            result.Notes.AddRange(minorityNotes);'
    Replace = 'if (true)
        {
            result.Notes.AddRange(minorityNotes);'
    Tests = 'MigrationTests.Init_AndMigrate_SayWhichFilesTheirCharsetAndFinalNewlineChange'
}
