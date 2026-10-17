# Trial 27: a configuration run sets the platform too
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '        if (platform is not null)
        {
            environment["Platform"] = platform;'
    Replace = '        if (false)
        {
            environment["Platform"] = platform;'
    Tests = 'MigrationTests.Format_RunsTheSolutionsPlatformsLikeConfigurations'
}
