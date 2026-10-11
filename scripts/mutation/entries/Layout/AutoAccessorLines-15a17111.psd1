# BRO1527 (auto-accessors on one line)
@{
    File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'
    Find = 'if (list.Accessors.Any(a => !Trivia.IsBlank(a, a.Span)))'
    Replace = 'if (false)'
    Tests = 'AutoAccessorLinesTests'
}
