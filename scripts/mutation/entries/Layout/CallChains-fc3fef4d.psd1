# BRO1523: a chain whose first line holds two or more calls isn't checked (#135)
@{
    File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'
    Find = ' + links.Count(l => l.Call && Line(text, l.Token.SpanStart) == firstLine) >= 2'
    Replace = ' >= 2'
    Tests = 'CallChainTests.AChainThatFillsItsFirstLineWithCalls_IsLeftAlone'
}
