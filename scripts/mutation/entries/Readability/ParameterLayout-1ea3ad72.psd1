# Trial 22 (owner's choice 2026-10-15): the leading multi-line lambda option
@{
    File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'
    Find = 'return line(first.SpanStart) == line(arguments.OpenParenToken.SpanStart)'
    Replace = 'return true'
    Tests = 'ParameterLayoutTests.LeadingMultiLineLambda_StaysWithTheOption'
}
