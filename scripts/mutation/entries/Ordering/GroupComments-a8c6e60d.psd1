# BRO1001: comments that introduce a group (GroupComments)
@{
    File = 'src/StyleBro.Analyzers/Ordering/GroupComments.cs'
    Find = 'if (keys[i + 1].KindRank == keys[i].KindRank && '
    Replace = 'if ('
    Tests = 'MemberOrderingTests'
}
