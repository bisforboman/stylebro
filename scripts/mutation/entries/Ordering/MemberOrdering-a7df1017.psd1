# BRO1001: a blank line BRO1519 gives the slot's old pair counts as there (trial 27)
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = '|| MayHaveBlankLineAfterBrace(members[slot - 1], members[slot]));'
    Replace = ');'
    Tests = 'MemberOrderingTests.FieldsFirst_GivesMovedMembersTheirBlankLines_WhenBro1505IsOff'
}
