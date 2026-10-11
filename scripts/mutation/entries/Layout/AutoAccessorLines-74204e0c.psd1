# BRO1527 (auto-accessors on one line)
@{
    File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'
    Find = 'preserve.Trim().Equals("false", StringComparison.OrdinalIgnoreCase)'
    Replace = 'false'
    Tests = 'AutoAccessorLinesTests'
}
