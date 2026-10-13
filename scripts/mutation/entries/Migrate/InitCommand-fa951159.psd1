# Trial 18: a bulk severity below warning hides StyleBro from dotnet format; init sets the rules by id
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'if (Below(category))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Init_SetsStyleBroRulesById_WhereABulkSeverityHidesThemFromFormat'
}
