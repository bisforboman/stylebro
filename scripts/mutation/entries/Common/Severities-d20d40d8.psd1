# a category severity (dotnet_analyzer_diagnostic.category-Style.severity) counts, and wins over the bulk one
@{
    File = 'src/StyleBro.Analyzers/Severities.cs'
    Find = 'Parse(category is null ? null : Get(options, $"dotnet_analyzer_diagnostic.category-{category}.severity"))'
    Replace = 'Parse(null)'
    Tests = 'TupleElementNamingTests.RenamesThatLetIde0042'
}
