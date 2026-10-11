# Fields of a [StructLayout] type are interop names
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'a.AttributeClass?.Name is "StructLayoutAttribute" or '
    Replace = 'a.AttributeClass?.Name is '
    Tests = 'FieldNamingTests'
}
