# BRO1142 (one local per declaration)
@{
    File = 'src/StyleBro.Analyzers/Readability/CombinedFields.cs'
    Find = '(declaration is BaseFieldDeclarationSyntax && text.ToString('
    Replace = '(true && text.ToString('
    Tests = 'CombinedFieldsTests'
}
