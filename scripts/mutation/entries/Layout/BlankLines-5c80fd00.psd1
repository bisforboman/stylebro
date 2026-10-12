# BRO1504: a comment right before a list's ',' or closing token
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'
    Find = '&& IsList(next.Parent));'
    Replace = '&& IsList(next.Parent) && false);'
    Tests = 'BlankLineAfterTests.CommentBetweenListItems_NoDiagnostic'
}
