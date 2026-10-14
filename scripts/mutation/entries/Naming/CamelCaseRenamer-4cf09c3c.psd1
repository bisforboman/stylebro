# Trial 21: Color Color
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = ' && !SymbolEqualityComparer.Default.Equals(s, field.Type))'
    Replace = ')'
    Tests = 'FieldNamingTests.FieldNamedLikeItsType_IsRenamed'
}
