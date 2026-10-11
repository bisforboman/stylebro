# BRO1523 (call chain layout)
@{
    File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'
    Find = '!directive.IsKind(SyntaxKind.RegionDirectiveTrivia) && !'
    Replace = '!'
    Tests = 'CallChainTests'
}
