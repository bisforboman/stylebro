# Trial 25: at most four other configurations
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'public const int MaxConfigurations = 4;'
    Replace = 'public const int MaxConfigurations = 5;'
    Tests = 'MigrationTests.Format_RunsAProjectsOtherConfigurations_ThatTheSolutionBuilds'
}
