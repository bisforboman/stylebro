# BRO1149: a condition that is just a dynamic variable ('if (d)') counts too.
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = 'condition.DescendantNodesAndSelf().OfType<ExpressionSyntax>()'
    Replace = 'condition.DescendantNodes().OfType<ExpressionSyntax>()'
    Tests = 'NestedIfTests'
}
