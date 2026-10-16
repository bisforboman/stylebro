# Trial 25: a project with an in-solution generator stays in its default configuration
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '            .Where(p => p.Value.Analyzers.Length == 0)'
    Replace = '            .Where(p => p.Value.Analyzers.Length >= 0)'
    Tests = 'MigrationTests.Format_FindsTheConfigurationsWhoseCodeDiffers_AndLeavesGeneratorProjectsInTheirDefault'
}
