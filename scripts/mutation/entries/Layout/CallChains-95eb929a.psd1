# BRO1523: the chain inside such parentheses is the outer chain's (trial 27)
@{
    File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'
    Find = 'ParenthesizedExpressionSyntax parenthesized => IsInChain(parenthesized, isRemoved) && isRemoved(parenthesized),'
    Replace = ''
    Tests = 'CallChainTests.ParenthesesBro1405Removes_TheInnerChainIsPartOfTheOuterOne'
}
