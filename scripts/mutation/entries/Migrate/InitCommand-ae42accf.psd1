# Trial 52: init reports the files its end_of_line converts
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'Conventions.ConvertedMinorityNote(rootEnding.Counts, settings) is { } minorityNote)
        {
            report.Add('
    Replace = 'Conventions.ConvertedMinorityNote(rootEnding.Counts, settings) is { } minorityNote)
        {
            if (false) report.Add('
    Tests = 'MigrationTests.Migrate_AndInit_SayWhichFilesTheirEndOfLineConverts'
}
