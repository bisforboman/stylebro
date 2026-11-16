# BRO1311: a literal's name isn't reported when another element has the new name, or it's a member of the tuple
@{
    File = 'src/StyleBro.Analyzers/Naming/TupleElementNamingAnalyzer.cs'
    Find = '                || TupleElementNames.HasNameClash(tuple, i, newName)'
    Replace = ''
    Tests = 'TupleElementNamingTests.NamesInTupleLiterals_WhoseNewNameIsTaken_AreNotReported'
}
