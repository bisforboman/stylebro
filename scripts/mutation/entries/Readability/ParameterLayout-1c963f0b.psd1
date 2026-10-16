# BRO1108: not when the '(' starts its line (BRO1109 moves it).
@{
    File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'
    Find = ' && openLine.Start + LeadingWhitespace(text, openLine).Length != open.SpanStart)'
    Replace = ')'
    Tests = 'StyleBro.Tests.ParameterLayoutTests.ItemsAtTheStartOfALine_GetTheMovedItemsIndentation'
}
