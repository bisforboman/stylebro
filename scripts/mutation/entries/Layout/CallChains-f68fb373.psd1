# BRO1523 (call chain layout)
@{
    File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'
    Find = 'if (previous.TrailingTrivia.Concat(token.LeadingTrivia).Any('
    Replace = 'if (false && previous.TrailingTrivia.Concat(token.LeadingTrivia).Any('
    Tests = 'CallChainTests'
}
