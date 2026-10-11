# A comment below code (BRO1504, BRO1506, BRO1001; eShop)
@{
    File = 'src/StyleBro.Analyzers/Ordering/GroupComments.cs'
    Find = 'members[i].GetLeadingTrivia().Any(t => Layout.BlankLines.IsCommentBelowCode(t, members[i].SyntaxTree.GetText())) && position[i] != position[i - 1] + 1'
    Replace = 'false'
    Tests = 'MemberOrderingTests.ACommentBelowAMember_KeepsTheContainerAsItIs'
}
