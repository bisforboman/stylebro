# BRO1302: a lambda nested inside an expression tree is part of it
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNames.cs'
    Find = '|| Readability.NullChecks.IsInExpressionTree(lambdaOrQuery, model, cancellationToken)'
    Replace = '|| false'
    Tests = 'CamelCaseNamingTests'
}
