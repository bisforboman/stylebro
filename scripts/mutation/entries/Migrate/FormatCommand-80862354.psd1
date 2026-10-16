# Trial 25: at most four other configurations
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '            .Take(4)'
    Replace = '            .Take(5)'
    Tests = 'MigrationTests.Format_RunsAProjectsOtherConfigurations_ThatTheSolutionBuilds'
}
