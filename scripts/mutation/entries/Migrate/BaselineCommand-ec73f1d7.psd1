# Trial 27: a baseline gets the findings of its projects' files only
@{
    File = 'src/StyleBro.Migrate/BaselineCommand.cs'
    Find = '            if (owns is not null && !owns(file))'
    Replace = '            if (false)'
    Tests = 'MigrationTests.Baseline_AddsOnlyTheFilesInItsFolder_AndItsProjectsFiles'
}
