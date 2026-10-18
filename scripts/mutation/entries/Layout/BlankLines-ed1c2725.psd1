# Trial 22: a comment continuing the trailing comment above needs no blank line.
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'
    Find = '            && !ContinuesTrailingComment(comment, text, aboveLine, tabWidth)'
    Replace = ''
    Tests = 'FixOrderTests.TrailingCommentContinuedBelow_StaysTogether'
}
