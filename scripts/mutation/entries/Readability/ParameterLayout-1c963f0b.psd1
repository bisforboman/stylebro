# BRO1108: not when the '(' stays at the start of its line (BRO1109 off; trial 61: with it on, the name's line counts).
@{
    File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'
    Find = ' && openLine.Start + LeadingWhitespace(text, openLine).Length != open.SpanStart)'
    Replace = ')'
    Tests = 'ParameterLayoutTests.OpenParenthesisOnItsOwnLine_IndentsFromTheLineBRO1109MovesItTo'
}
