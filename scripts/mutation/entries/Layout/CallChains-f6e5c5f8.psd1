# BRO1523 looks through parentheses BRO1405 removes (trial 27)
@{
    File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'
    Find = 'case ParenthesizedExpressionSyntax parenthesized when isRemoved(parenthesized):'
    Replace = 'case ParenthesizedExpressionSyntax parenthesized when false:'
    Tests = 'CallChainTests.ParenthesesBro1405Removes'
}
