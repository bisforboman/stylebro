# the bulk severity (dotnet_analyzer_diagnostic.severity) counts
@{
    File = 'src/StyleBro.Analyzers/Severities.cs'
    Find = '?? Parse(Get(options, "dotnet_analyzer_diagnostic.severity"))'
    Replace = '?? null'
    Tests = 'TupleElementNamingTests.RenamesThatLetIde0042'
}
