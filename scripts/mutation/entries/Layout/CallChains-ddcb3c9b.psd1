# BRO1523 (call chain layout)
@{
    File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'
    Find = '|| chain.ContainsDiagnostics'
    Replace = '|| false'
    Tests = 'CallChainTests'
}
