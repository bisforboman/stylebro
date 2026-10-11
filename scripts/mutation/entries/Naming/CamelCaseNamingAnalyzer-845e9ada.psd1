# Names a rename never produces: 'field' (C# 14 keyword in accessors), 'value' inside a property/indexer/event
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'
    Find = '&& CamelCaseNames.CanRename(declaration, name, renamed, getNewName) ? renamed'
    Replace = '? renamed'
    Tests = 'ParameterMatchesBaseTests'
}
