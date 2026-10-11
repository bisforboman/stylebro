# BRO1504: a comment between call chain links needs no blank line (owner's decision, 2026-10-11)
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'
    Find = '&& !IsBetweenChainLinks(comment)'
    Replace = ''
    Tests = 'BlankLineAfterTests.CommentBetweenCallChainLinks_NoDiagnostic'
}
