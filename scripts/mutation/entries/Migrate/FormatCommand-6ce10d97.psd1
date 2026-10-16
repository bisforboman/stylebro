# Trial 25: framework symbols are the same in every configuration
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '^(?:NET|NET\d\w*|NETCOREAPP'
    Replace = '^(?:NET|NETCOREAPP'
    Tests = 'MigrationTests.Format_TellsFrameworkSymbolsFromConfigurationOnes'
}
