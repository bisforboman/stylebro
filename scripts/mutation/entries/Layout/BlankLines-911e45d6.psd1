# StyleCop's open bugs (2026-10-04)
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'
    Find = '&& !(token.Parent is InitializerExpressionSyntax { Parent: InitializerExpressionSyntax })'
    Replace = ''
    Tests = 'BlankLineBeforeTests'
}
