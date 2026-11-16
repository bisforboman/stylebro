# BRO1311: an inferred name counts as taken too ('(lower: 1, other.Lower)')
@{
    File = 'src/StyleBro.Analyzers/Naming/TupleElementNames.cs'
    Find = ' ?? GetInferredName(tuple.Arguments[i].Expression)) == newName)'
    Replace = ') == newName)'
    Tests = 'TupleElementNamingTests.NamesInTupleLiterals_WhoseNewNameIsTaken_AreNotReported'
}
