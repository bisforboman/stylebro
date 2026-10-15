@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'
    Find = '&& trailing.SpanStart == aboveLine.Start + column;'
    Replace = ';'
    Tests = 'BlankLineAfterTests'
}
