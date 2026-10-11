# BRO1001: comments that introduce a group (GroupComments)
@{
    File = 'src/StyleBro.Analyzers/Ordering/GroupComments.cs'
    Find = 'if (trivia.IsDirective)'
    Replace = 'if (false)'
    Tests = 'MemberOrderingTests'
}
