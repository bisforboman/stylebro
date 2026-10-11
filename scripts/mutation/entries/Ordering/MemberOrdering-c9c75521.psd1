# BRO1001 inside regions
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = 'if (i > 0 && LastDirective(trivia) >= 0)'
    Replace = 'if (false)'
    Tests = 'MemberOrderingTests'
}
