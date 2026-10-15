# A built-in rule the file sets by id stays the team's
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = '.Where(p => !OffSeverity(p.Value) && !settings.ContainsKey($"dotnet_diagnostic.{p.Key}.severity"))'
    Replace = '.Where(p => !OffSeverity(p.Value))'
    Tests = 'MigrationTests.StyleOffIds_KeepsRulesTheFileSetsAndACategoryThatOverridesTheBulk'
}
