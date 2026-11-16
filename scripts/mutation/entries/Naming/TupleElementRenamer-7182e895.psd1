# BRO1311: only a use that really names the element ('(1 + pair.first, ...)' doesn't)
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = '&& inferred.Name == oldName)'
    Replace = ')'
    Tests = 'TupleElementNamingTests.NamesInferredFromAUse_NextToTheNewName'
}
