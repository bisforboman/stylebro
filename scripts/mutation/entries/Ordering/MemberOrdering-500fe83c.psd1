# BRO1001 at the namespace level
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = 'if (slot == count - 1 && !EndsWithNewLine(members[count - 1]) && EndsWithNewLine(member))'
    Replace = 'if (false)'
    Tests = 'MemberOrderingTests'
}
