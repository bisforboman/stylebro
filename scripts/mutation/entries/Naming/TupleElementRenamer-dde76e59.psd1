# BRO1311: an element named by a renamed use is renamed too, with its uses
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = 'more |= renamed.Add(Key(location));'
    Replace = ''
    Tests = 'TupleElementNamingTests.NamesInTupleLiterals_RenameTheElementsInferredFromTheirUses'
}
