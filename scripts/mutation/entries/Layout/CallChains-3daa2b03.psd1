# BRO1523 (call chain layout)
@{
    File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'
    Find = '.Any(l => l.Trim().Length > 0 && '
    Replace = '.Any(l => '
    Tests = 'CallChainTests'
}
