# BRO1142 (one local per declaration)
@{
    File = 'src/StyleBro.Analyzers/Readability/CombinedFields.cs'
    Find = 'declaration.UsingKeyword.IsKind(SyntaxKind.None) ?'
    Replace = 'true ?'
    Tests = 'CombinedFieldsTests'
}
