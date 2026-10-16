# Trial 18: a rule the .editorconfig sets by id stays the team's
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'if (settings.ContainsKey($"dotnet_diagnostic.{id}.severity") || style?'
    Replace = 'if (style?'
    Tests = 'MigrationTests.Init_SetsStyleBroRulesById_WhereABulkSeverityHidesThemFromFormat'
}
