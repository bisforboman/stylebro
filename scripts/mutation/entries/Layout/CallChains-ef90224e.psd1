# BRO1523 (call chain layout)
@{
    File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'
    Find = '.Where(n => !regionLines.Contains(n))'
    Replace = ''
    Tests = 'CallChainTests'
}
