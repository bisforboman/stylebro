# BRO1303/BRO1306/BRO1307 (fields)
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = '            : isProtected && field.IsReadOnly ?'
    Replace = '            : false ?'
    Tests = 'FieldNamingTests'
}
