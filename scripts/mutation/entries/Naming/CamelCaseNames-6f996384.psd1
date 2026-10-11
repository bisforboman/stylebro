# Names in disabled #if code (DisabledCode), SCREAMING_CASE locals joined word by word
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNames.cs'
    Find = '? FieldNames.GetJoinedName(core, FieldNames.FieldCasing.Camel) ?? ToCamelCase(name)'
    Replace = '? ToCamelCase(name)'
    Tests = 'CamelCaseNamingTests.NewName'
}
