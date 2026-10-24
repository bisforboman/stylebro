# Trial 53: migrate reports the files its charset or final newline changes
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'minorityNotes.Add(minorityFileNote);'
    Replace = '_ = minorityFileNote;'
    Tests = 'MigrationTests.Init_AndMigrate_SayWhichFilesTheirCharsetAndFinalNewlineChange'
}
