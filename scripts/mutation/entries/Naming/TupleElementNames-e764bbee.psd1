# BRO1311: 'rest' and 'toString' can't become the tuple's own members' names (CS8126)
@{
    File = 'src/StyleBro.Analyzers/Naming/TupleElementNames.cs'
    Find = 'if (newName is "CompareTo" or "Deconstruct" or "Equals" or "GetHashCode" or "Rest" or "ToString"'
    Replace = 'if (newName is "CompareTo"'
    Tests = 'TupleElementNamingTests.NamesInTupleLiterals_WhoseNewNameIsTaken_AreNotReported'
}
