# A comment below code (BRO1504, BRO1506, BRO1001; eShop)
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'
    Find = '&& (previous.Parent is StatementSyntax || previous.Parent is MemberDeclarationSyntax and not BaseNamespaceDeclarationSyntax)'
    Replace = ''
    Tests = 'TrailingBlankLinesTests.BlankLinesAfterComments_AreRemoved'
}
