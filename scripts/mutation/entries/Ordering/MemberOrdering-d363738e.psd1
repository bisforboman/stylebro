# BRO1001 inside regions
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = 'var split = LastDirective(trivia) + 1;'
    Replace = 'var split = 0;'
    Tests = 'MemberOrderingTests'
}
