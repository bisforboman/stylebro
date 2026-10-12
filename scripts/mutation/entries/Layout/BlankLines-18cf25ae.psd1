# BRO1504: a comment between list items needs no blank line (owner's decision, 2026-10-12); the regions fix asks the same
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'
    Find = '            && !IsBetweenListItems(comment)'
    Replace = ''
    Tests = 'BlankLineAfterTests.CommentBetweenListItems_NoDiagnostic|FullyQualifiedName~RegionsTests.ACommentBetweenArgumentsBelowTheRemovedLines_GetsNoBlankLine'
}
