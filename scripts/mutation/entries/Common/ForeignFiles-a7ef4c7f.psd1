# Performance: the partial method bodies of a tree, read once for every node action
@{
    File = 'src/StyleBro.Analyzers/ForeignFiles.cs'
    Find = '(method.Body is not null || method.ExpressionBody is not null) && method.Modifiers.Any(SyntaxKind.PartialKeyword)'
    Replace = 'method.ExpressionBody is not null && method.Modifiers.Any(SyntaxKind.PartialKeyword)'
    Tests = 'GeneratedFileSuppressorTests.PartialMethodBody_IsAnalyzedWhenItsDeclarationIsGenerated'
}
