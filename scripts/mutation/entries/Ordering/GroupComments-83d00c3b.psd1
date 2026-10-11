# BRO1001: comments that introduce a group (GroupComments)
@{
    File = 'src/StyleBro.Analyzers/Ordering/GroupComments.cs'
    Find = '&& Moves(position, i) && HasComment'
    Replace = '&& HasComment'
    Tests = 'MemberOrderingTests'
}
