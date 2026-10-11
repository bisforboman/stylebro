# BRO1523 (call chain layout)
@{
    File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'
    Find = '|| chain.Ancestors().Any(a => a is InterpolationSyntax)'
    Replace = ''
    Tests = 'CallChainTests'
}
