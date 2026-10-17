# BRO1523: only while BRO1405 is on (trial 27)
@{
    File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'
    Find = 'Dictionary<ParenthesizedExpressionSyntax, bool>? removed = parenthesesRemoved ? new() : null;'
    Replace = 'Dictionary<ParenthesizedExpressionSyntax, bool>? removed = new();'
    Tests = 'CallChainTests.ParenthesesBro1405Removes_AreLookedThrough'
}
