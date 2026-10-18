@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'
    Find = '== Indentation.GetVisualColumn(text, comment.SpanStart, tabWidth);'
    Replace = '>= 0;'
    Tests = 'BlankLineAfterTests'
}
