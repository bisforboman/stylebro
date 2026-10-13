# BRO1311: outside code sees an internal member only from an InternalsVisibleTo project.
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = '        if (member is null || !IsInternal(member))'
    Replace = '        if (true)'
    Tests = 'KeptFindingsTests.AnInternalTupleElement_IsKeptOnlyForMemberUsesInProjectsThatSeeIt'
}
