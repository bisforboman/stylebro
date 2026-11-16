# BRO1311 (StyleCop 1.3's SA1316): explicit names in tuple literals are checked
@{
    File = 'src/StyleBro.Analyzers/Naming/TupleElementNamingAnalyzer.cs'
    Find = '        context.RegisterSyntaxNodeAction(AnalyzeLiteral, SyntaxKind.TupleExpression);'
    Replace = ''
    Tests = 'TupleElementNamingTests.NamesInTupleLiterals_AreRenamedWithTheirUses'
}
