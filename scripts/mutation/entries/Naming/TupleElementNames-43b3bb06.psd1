# BRO1311: two literals compared with each other are both their own
@{
    File = 'src/StyleBro.Analyzers/Naming/TupleElementNames.cs'
    Find = 'return other is ITupleOperation ? null : other.Type;'
    Replace = 'return other.Type;'
    Tests = 'TupleElementNamingTests.NamesInTupleLiterals_AreRenamedWithTheirUses'
}
