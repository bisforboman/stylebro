# BRO1303/BRO1306/BRO1307 (fields)
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = '(isProtectedReadOnly || IsPascalChecked(field, style))'
    Replace = 'IsPascalChecked(field, style)'
    Tests = 'FieldNamingTests.ProtectedReadOnlyFields'
}
