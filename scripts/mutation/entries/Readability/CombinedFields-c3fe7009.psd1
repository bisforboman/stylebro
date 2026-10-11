# BRO1142 (one local per declaration)
@{
    File = 'src/StyleBro.Analyzers/Readability/CombinedFields.cs'
    Find = 'declaration.UsingKeyword.IsKind(SyntaxKind.None) && useVar'
    Replace = 'true && useVar'
    Tests = 'CombinedFieldsTests'
}
