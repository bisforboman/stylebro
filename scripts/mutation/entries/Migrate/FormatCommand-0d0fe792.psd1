# Trial 27: configurations in the project's own platform first (at most four)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '            .OrderBy(c => SplitConfiguration(c).Platform is not null)'
    Replace = ''
    Tests = 'MigrationTests.Format_RunsTheSolutionsPlatformsLikeConfigurations'
}
