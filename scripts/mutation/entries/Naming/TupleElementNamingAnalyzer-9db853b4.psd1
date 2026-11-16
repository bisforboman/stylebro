# BRO1311: a literal's name that follows a declared tuple type's is that declaration's to report
@{
    File = 'src/StyleBro.Analyzers/Naming/TupleElementNamingAnalyzer.cs'
    Find = '                || TupleElementNames.ClassifyLiteralElement(tuple, i, context.SemanticModel, context.CancellationToken).Kind == TupleLiteralElement.FollowsTarget'
    Replace = ''
    Tests = 'TupleElementNamingTests.NamesInTupleLiterals_ThatFollowADeclaredTupleTypeThatKeepsItsNames'
}
