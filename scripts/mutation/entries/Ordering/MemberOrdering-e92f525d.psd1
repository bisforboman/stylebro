# BRO1001: the sort adds BRO1519's blank line below a '}'.
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = 'var createsBrace = WantsBlankLineAfterBrace(members[order[slot - 1]], members[source]);'
    Replace = 'var createsBrace = false;'
    Tests = 'StyleBro.Tests.FixOrderTests.Sort_AMemberBelowAConstructor_GetsBRO1519sBlankLine'
}
