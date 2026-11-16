# BRO1311: 'item1' can't become 'Item1' for another element than the first (CS8125)
@{
    File = 'src/StyleBro.Analyzers/Naming/TupleElementNames.cs'
    Find = '            || (newName.StartsWith("Item", StringComparison.Ordinal) && int.TryParse(newName.Substring(4), out var position) && position != index + 1))'
    Replace = ')'
    Tests = 'TupleElementNamingTests.NamesInTupleLiterals_WhoseNewNameIsTaken_AreNotReported'
}
