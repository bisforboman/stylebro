# BRO1001: only accessor lists with a body are expanded by BRO1509 (trial 27)
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = '(brace.Parent is AccessorListSyntax list && list.Accessors.Any(a => a.Body is not null))'
    Replace = 'brace.Parent is AccessorListSyntax'
    Tests = 'MemberOrderingTests.CompactMembersThatWereAlreadyCompact_StayCompact'
}
