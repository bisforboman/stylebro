# BRO1311: no written-out name in a literal converted to a tuple type (coverlet, trial 20)
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = 'when model.GetOperation(tuple, cancellationToken)?.Parent is not IConversionOperation { Type: INamedTypeSymbol { IsTupleType: true } }'
    Replace = 'when true'
    Tests = 'TupleElementNamingTests.UsesInConvertedTupleLiterals'
}
