# BRO1311: the fix leaves a literal where the new name is taken
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = 'else if (inScope && !TupleElementNames.HasNameClash(tuple, i, newName))'
    Replace = 'else if (inScope)'
    Tests = 'TupleElementNamingTests.NamesInTupleLiterals_WhoseNewNameIsTaken_StayWhenTheNameIsRenamedElsewhere'
}
