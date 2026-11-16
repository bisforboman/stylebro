# BRO1311: a per-project rename leaves the other projects' literals
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = 'else if (inScope && !TupleElementNames'
    Replace = 'else if (!TupleElementNames'
    Tests = 'TupleElementNamingTests.ElementsInAnotherProject'
}
