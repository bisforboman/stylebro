# BRO1523 (call chain layout)
@{
    File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'
    Find = '|| !links[i - 1].Call '
    Replace = ''
    Tests = 'CallChainTests'
}
