# BRO1001: comments that introduce a group (GroupComments)
@{
    File = 'src/StyleBro.Analyzers/Ordering/GroupComments.cs'
    Find = 'if (keys[i + 1].Kind == keys[i].Kind && '
    Replace = 'if ('
    Tests = 'MemberOrderingTests'
}
