# Trial 22 (owner's choice 2026-10-15): the leading multi-line lambda option
@{
    File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'
    Find = '.All(a => line(a.SpanStart) == end)'
    Replace = '.All(a => true)'
    Tests = 'ParameterLayoutTests.LeadingMultiLineLambda_StaysWithTheOption'
}
