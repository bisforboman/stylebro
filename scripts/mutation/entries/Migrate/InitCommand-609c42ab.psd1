# Trial 53: init reports the files its charset or final newline changes
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'report.Add($"  {"note",-8} {minorityFileNote}");'
    Replace = '_ = minorityFileNote;'
    Tests = 'MigrationTests.Init_AndMigrate_SayWhichFilesTheirCharsetAndFinalNewlineChange'
}
