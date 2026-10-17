# Trial 27: a solution platform other than Any CPU is a configuration of its own
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'static string Pair(string name, string platform) => IsAnyPlatform(platform) ? name'
    Replace = 'static string Pair(string name, string platform) => true ? name'
    Tests = 'MigrationTests.Format_RunsTheSolutionsPlatformsLikeConfigurations'
}
