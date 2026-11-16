# BRO1311: a rename that adds CS8383 (a name ignored in a tuple comparison) is left
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = 'd.Id is "CS8123" or "CS8383")'
    Replace = 'd.Id is "CS8123")'
    Tests = 'TupleElementNamingTests.NamesInTupleLiterals_WhoseRenameAddsAnIgnoredNameWarning'
}
