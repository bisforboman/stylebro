# BRO1149 (nested ifs)
# Sonar ids (NodeCodeFixProvider): no action where the shared logic skips
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = '|| condition.IsKind(SyntaxKind.LogicalOrExpression)'
    Replace = ''
    Tests = 'NestedIfTests.LooserOperands_GetParentheses'
}
