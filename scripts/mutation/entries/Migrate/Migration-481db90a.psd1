# Trial 43: only a global config's bulk and category severities count, not its rules by id.
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '.Where(p => p.Key.StartsWith("dotnet_analyzer_diagnostic.", StringComparison.OrdinalIgnoreCase))'
    Replace = ''
    Tests = 'MigrationTests.WithGlobalBulk'
}
