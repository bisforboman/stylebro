# Trial 25: the default configuration is not another one
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '            .Where(c => !c.Equals(own, StringComparison.OrdinalIgnoreCase))'
    Replace = '            .Where(c => true)'
    Tests = 'MigrationTests.Format_RunsAProjectsOtherConfigurations_ThatTheSolutionBuilds'
}
