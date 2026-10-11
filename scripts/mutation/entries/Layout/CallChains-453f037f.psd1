# BRO1523 (call chain layout)
@{
    File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'
    Find = 'ConditionalAccessExpressionSyntax => true,'
    Replace = 'ConditionalAccessExpressionSyntax => false,'
    Tests = 'CallChainTests'
}
