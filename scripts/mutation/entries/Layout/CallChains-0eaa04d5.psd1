# BRO1523: the receiver's own call counts toward a first line with two or more calls (#135)
@{
    File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'
    Find = '(receiverCall ? 1 : 0) + '
    Replace = ''
    Tests = 'CallChainTests.AChainThatFillsItsFirstLineWithCalls_IsLeftAlone'
}
