# BRO1523 (call chain layout)
@{
    File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'
    Find = 'if (!starts.Contains(true) || '
    Replace = 'if ('
    Tests = 'CallChainTests'
}
