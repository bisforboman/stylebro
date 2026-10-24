# Trial 52: init reports the files its end_of_line converts
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'report.Add($"  {"note",-8} {minorityNote}");'
    Replace = '_ = minorityNote;'
    Tests = 'MigrationTests.Migrate_AndInit_SayWhichFilesTheirEndOfLineConverts'
}
