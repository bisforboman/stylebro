# Names a rename never produces: 'field' (C# 14 keyword in accessors), 'value' inside a property/indexer/event
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'
    Find = 'if (depth < 8 && baseParameter'
    Replace = 'if (false && baseParameter'
    Tests = 'ParameterMatchesBaseTests'
}
