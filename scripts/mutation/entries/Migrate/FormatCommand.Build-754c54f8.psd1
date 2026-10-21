# Trial 40: a configuration without a platform is the solution's Any CPU
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = '{platform ?? "Any CPU"}'
    Replace = '{platform}'
    Tests = 'StyleBro.Tests.MigrationTests.NotBuiltIn_NamesTheProjectsAConfigurationDoesntBuild'
}
