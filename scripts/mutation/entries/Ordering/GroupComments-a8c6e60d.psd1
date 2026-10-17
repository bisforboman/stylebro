# BRO1001: comments that introduce a group (GroupComments)
@{
    File = 'src/StyleBro.Analyzers/Ordering/GroupComments.cs'
    Find = 'if (!fieldsFirst && keys[i + 1].KindRank == keys[i].KindRank && '
    Replace = 'if (!fieldsFirst && '
    Tests = 'MemberOrderingTests'
}
