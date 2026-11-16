# BRO1311: a name the target type names differently is ignored by the compiler, and renamed on its own
@{
    File = 'src/StyleBro.Analyzers/Naming/TupleElementNames.cs'
    Find = 'return (element.Name == name ? TupleLiteralElement.FollowsTarget : TupleLiteralElement.Ignored, element);'
    Replace = 'return (TupleLiteralElement.FollowsTarget, element);'
    Tests = 'TupleElementNamingTests.NamesInTupleLiterals_TheTargetTypeIgnores'
}
