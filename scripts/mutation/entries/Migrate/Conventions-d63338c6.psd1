# Trial 22 (ctk): the final-newline note names the files format changes
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = ': files[1 - winner] == 0 ? "''format'' leaves the files alone"'
    Replace = ': false ? "''format'' leaves the files alone"'
    Tests = 'MigrationTests.ContradictedFinalNewline_SaysWhichFilesFormatChanges'
}
