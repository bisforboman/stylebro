# BRO1301: range variables of a query that ends up in an expression tree keep their names (Mapster)
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'
    Find = '&& CamelCaseNames.IsInExpressionTree(query, context.SemanticModel'
    Replace = '&& false && CamelCaseNames.IsInExpressionTree(query, context.SemanticModel'
    Tests = 'CamelCaseNamingTests'
}
