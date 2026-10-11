# BRO1142 (one local per declaration)
@{
    File = 'src/StyleBro.Analyzers/Readability/CombinedFields.cs'
    Find = 'if (sameLine && declaration is not LocalDeclarationStatementSyntax)'
    Replace = 'if (false)'
    Tests = 'CombinedFieldsTests'
}
