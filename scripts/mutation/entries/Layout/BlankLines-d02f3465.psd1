# Trial 26: a comment right below an attribute list
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'
    Find = '            && !IsBelowAttributes(previous)'
    Replace = '            && !false'
    Tests = 'BlankLineAfterTests.CommentBelowAnAttribute_NoDiagnostic_UnlessAnAssemblyAttribute'
}
