# BRO1311: a literal that keeps its own type declares its elements
@{
    File = 'src/StyleBro.Analyzers/Naming/TupleElementNames.cs'
    Find = '        if (element is null || IsOwn(element))'
    Replace = '        if (element is null)'
    Tests = 'TupleElementNamingTests.NamesInTupleLiterals_AreRenamedWithTheirUses'
}
