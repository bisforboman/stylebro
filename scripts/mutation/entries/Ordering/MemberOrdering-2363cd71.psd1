# BRO1001: a one-line accessor list BRO1509 expands gets BRO1519's blank line (trial 27)
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = '(brace.Parent is AccessorListSyntax list && list.Accessors.Any(a => a.Body is not null))'
    Replace = 'false'
    Tests = 'FixOrderTests.Sort_ExpandedBlocks_BRO1519sBlankLineFollowsTheBrace'
}
