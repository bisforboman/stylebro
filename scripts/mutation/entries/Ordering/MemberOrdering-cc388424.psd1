# BRO1001: comments that introduce a group (GroupComments)
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = '|| GroupComments.MovesGroupComment(members, keys, order)'
    Replace = ''
    Tests = 'MemberOrderingTests'
}
