# BRO1527 (auto-accessors on one line)
@{
    File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'
    Find = 'a.AttributeLists.Count > 0 || '
    Replace = ''
    Tests = 'AutoAccessorLinesTests'
}
