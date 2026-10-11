# BRO1523 (call chain layout)
@{
    File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'
    Find = 'if (!directive.IsKind(SyntaxKind.RegionDirectiveTrivia) && '
    Replace = 'if (false && '
    Tests = 'CallChainTests'
}
