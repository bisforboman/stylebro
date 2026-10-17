# Trial 27 (Mediator): a solution platform the project doesn't list builds it in its own
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = ' && !projectPlatforms.Contains(platform, StringComparer.OrdinalIgnoreCase) ? name : c;'
    Replace = ' && false ? name : c;'
    Tests = 'MigrationTests.Format_RunsTheSolutionsPlatformsLikeConfigurations'
}
