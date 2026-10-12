# BRO1504: only the punctuation of a list counts, not any '(' or ','
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'
    Find = '&& IsList(previous.Parent))'
    Replace = '&& true)'
    Tests = 'BlankLineAfterTests.CommentInsideAListItem_IsReported'
}
