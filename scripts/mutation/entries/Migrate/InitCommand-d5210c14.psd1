# Trial 18: a category severity counts like the compiler reads it
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = '&& settings.TryGetValue($"dotnet_analyzer_diagnostic.category-{name}.severity", out var value) ? value : bulk;'
    Replace = '&& false ? null : bulk;'
    Tests = 'MigrationTests.HiddenFromFormat_CountsCategories_AndOnlySeveritiesBelowWarning'
}
