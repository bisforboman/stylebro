# BRO1311: an inferred name next to the new name is written out with the old one
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = 'if (TupleElementNames.HasNameClash(tuple, index, newName))'
    Replace = 'if (false)'
    Tests = 'TupleElementNamingTests.NamesInferredFromAUse_NextToTheNewName'
}
