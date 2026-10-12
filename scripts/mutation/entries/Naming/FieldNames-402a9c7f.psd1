# BRO1306 (trial 11, Autofac): a leading underscore is SA1309's when stylebro_allow_leading_underscore_in_pascal_fields is true
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'if (pascal && style.PascalAllowsUnderscore && name.StartsWith("_", System.StringComparison.Ordinal))'
    Replace = 'if (false)'
    Tests = 'FieldNamingTests.AllowUnderscore_KeepsPascalFieldsStartingWithUnderscore'
}
