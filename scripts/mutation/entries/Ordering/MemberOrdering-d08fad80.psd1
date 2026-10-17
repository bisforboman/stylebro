# BRO1001: members that stay neighbours keep their gap (trial 27)
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = 'if (slot > 0 && source > 0 && order[slot - 1] == source - 1)'
    Replace = 'if (false)'
    Tests = 'MemberOrderingTests.FieldsFirst_GivesMovedMembersTheirBlankLines'
}
