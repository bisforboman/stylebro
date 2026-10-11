# Names a rename never produces: 'field' (C# 14 keyword in accessors), 'value' inside a property/indexer/event
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNames.cs'
    Find = ' && name != "field";'
    Replace = ';'
    Tests = 'FieldNamingTests'
}
