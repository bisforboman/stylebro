# BRO1311: 'item1' may become 'Item1' for the first element
@{
    File = 'src/StyleBro.Analyzers/Naming/TupleElementNames.cs'
    Find = ' && position != index + 1))'
    Replace = '))'
    Tests = 'TupleElementNamingTests.NamesInTupleLiterals_ItemNamesAtTheirOwnPosition'
}
