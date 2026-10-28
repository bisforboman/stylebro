# Trial 59: a delegate lambda that is an expression tree's twin is skipped like it
@{
    File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'
    Find = 'return lambdas.Any(lambda => others.Any(other => SyntaxFactory.AreEquivalent(lambda, other)'
    Replace = 'return lambdas.Any(lambda => others.Any(other => false'
    Tests = 'ExpressionTreeTwins'
}
