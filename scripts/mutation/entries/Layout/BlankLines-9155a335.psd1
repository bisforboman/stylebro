# BRO1504: a comment above an embedded block's brace is still reported when BRO1132 is off
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'
    Find = 'is { } statement and not BlockSyntax'
    Replace = 'is { } statement'
    Tests = 'BlankLineAfterTests.CommentBeforeOpenBrace_WithBro1132Off'
}
