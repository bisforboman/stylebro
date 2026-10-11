# BRO1303/BRO1306/BRO1307 (fields)
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'var newName = isProtected && field.IsReadOnly ?'
    Replace = 'var newName = false ?'
    Tests = 'FieldNamingTests'
}
