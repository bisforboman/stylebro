# BRO1311: the uses of a literal's own element are renamed with its name
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = '                        if (kind == TupleLiteralElement.Own)'
    Replace = '                        if (false)'
    Tests = 'TupleElementNamingTests.NamesInTupleLiterals_AreRenamedWithTheirUses'
}
