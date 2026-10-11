# BRO1523 (call chain layout)
@{
    File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'
    Find = 'if (Enumerable.Range(line + 1, Line(text, end) - line)'
    Replace = 'if (false && Enumerable.Range(line + 1, Line(text, end) - line)'
    Tests = 'CallChainTests'
}
