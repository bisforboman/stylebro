# Trial 21: unless the nested type uses the outer name
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'return !type.DeclaringSyntaxReferences.Any('
    Replace = 'return true || !type.DeclaringSyntaxReferences.Any('
    Tests = 'FieldNamingTests.NestedField_IsKeptWhenTheNestedClassUsesTheOuterName'
}
