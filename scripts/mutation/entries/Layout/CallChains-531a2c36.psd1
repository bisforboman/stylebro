# BRO1523 (call chain layout)
@{
    File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'
    Find = '|| !links.Skip(i).Any(l => l.Call)'
    Replace = ''
    Tests = 'CallChainTests'
}
