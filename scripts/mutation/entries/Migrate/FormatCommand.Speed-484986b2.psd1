# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Speed.cs'
    Find = 'r.Projects.SelectMany(p => ProjectGraph(p, references))'
    Replace = 'r.Projects'
    Tests = 'MigrationTests.Format_PlansTheViewsOfTheConfigurationRuns_AtOnce'
}
