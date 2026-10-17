# Trial 27: baseline adds no entries for files outside its folder
@{
    File = 'src/StyleBro.Migrate/BaselineCommand.cs'
    Find = '                if (isOutside)'
    Replace = '                if (false)'
    Tests = 'MigrationTests.Baseline_AddsOnlyTheFilesInItsFolder_AndItsProjectsFiles'
}
