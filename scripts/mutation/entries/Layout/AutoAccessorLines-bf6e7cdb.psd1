# BRO1527 (auto-accessors on one line)
@{
    File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'
    Find = 'a.ExpressionBody is not null || '
    Replace = ''
    Tests = 'AutoAccessorLinesTests'
}
