# BRO1310: the Hungarian prefix behind s_ is found
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = '&& (GetCamelStaticStyle(field, style) ?? (IsChecked(field, style) ? GetPrivateStyle(field, style) : null)) == FieldStyle.StaticCamelCase'
    Replace = '&& false'
    Tests = 'HungarianAbstractTests.StaticPrefix_StaysInFrontOfTheStrippedName'
}
