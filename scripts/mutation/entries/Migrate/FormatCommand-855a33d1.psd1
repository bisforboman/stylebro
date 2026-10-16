# Trial 25: the default configuration is not another one
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '!c.Equals(own, StringComparison.OrdinalIgnoreCase) && '
    Replace = ''
    Tests = 'MigrationTests.Format_RunsAProjectsOtherConfigurations_ThatTheSolutionBuilds'
}
