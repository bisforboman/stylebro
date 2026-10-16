# BRO1311 (tuple element casing)
@{
    File = 'src/StyleBro.Analyzers/Naming/TupleElementNamingAnalyzer.cs'
    Find = '|| TupleElementNames.InheritsNames(element, context.SemanticModel, options, context.CancellationToken)'
    Replace = ''
    Tests = 'TupleElementNamingTests'
}
