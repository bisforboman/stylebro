# BRO1306 (SA1304 in StyleCop 1.3): protected readonly fields are PascalCase, not BRO1303's camelCase
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'IsProtectedInstanceLike(field, style) && !field.IsReadOnly'
    Replace = 'IsProtectedInstanceLike(field, style)'
    Tests = 'FieldNamingTests.ProtectedReadOnlyFields'
}
