# Types nested in an interop type count too
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = '        for (var type = field.ContainingType; type is not null; type = type.ContainingType)'
    Replace = '        for (var type = field.ContainingType; type is not null; type = null)'
    Tests = 'FieldNamingTests'
}
