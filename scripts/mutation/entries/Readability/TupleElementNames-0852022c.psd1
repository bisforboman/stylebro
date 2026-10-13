# BRO1124: the same for 'x?.Item1'
@{
    File = 'src/StyleBro.Analyzers/Readability/TupleElementNames.cs'
    Find = ': name.FirstAncestorOrSelf<ConditionalAccessExpressionSyntax>()?.Expression;'
    Replace = ': null;'
    Tests = 'TupleElementNameTests'
}
