# Names a rename never produces: 'field' (C# 14 keyword in accessors), 'value' inside a property/indexer/event
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'
    Find = '|| !CamelCaseNames.CanRename(node, oldName, newName, getNewName)'
    Replace = ''
    Tests = 'ParameterMatchesBaseTests'
}
