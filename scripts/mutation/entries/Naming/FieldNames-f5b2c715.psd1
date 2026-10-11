# SCREAMING_CASE constants and static readonly fields are interop names
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = '        if ((field.IsConst || (field.IsStatic && field.IsReadOnly))'
    Replace = '        if (field.Name.Length < 0'
    Tests = 'FieldNamingTests'
}
