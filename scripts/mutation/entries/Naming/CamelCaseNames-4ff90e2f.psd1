# Names a rename never produces: 'field' (C# 14 keyword in accessors), 'value' inside a property/indexer/event
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNames.cs'
    Find = 'if (newName == "value" && scope is BasePropertyDeclarationSyntax)'
    Replace = 'if (false)'
    Tests = 'CamelCaseNamingTests'
}
