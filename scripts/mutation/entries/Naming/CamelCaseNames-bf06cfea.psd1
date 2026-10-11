# Names a rename never produces: 'field' (C# 14 keyword in accessors), 'value' inside a property/indexer/event
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNames.cs'
    Find = '&& a.ConstructorArguments[0].Value as string == target.Name'
    Replace = '&& false'
    Tests = 'ParameterMatchesBaseTests'
}
