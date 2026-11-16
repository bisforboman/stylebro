# BRO1311: no written-out name in a literal converted to a tuple type (coverlet, trial 20)
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = 'is (TupleLiteralElement.Own, { } inferred)'
    Replace = 'is (_, { } inferred)'
    Tests = 'TupleElementNamingTests.NamesInferredFromAUse_NextToTheNewName'
}
