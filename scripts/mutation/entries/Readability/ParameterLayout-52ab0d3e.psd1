# Trial 22 (owner's choice 2026-10-15): the leading multi-line lambda option
@{
    File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'
    Find = 'IsSameLine(options)) && !(AllowsLeadingLambda(options) && IsLeadingLambda(list, line))'
    Replace = 'IsSameLine(options)) && !(false)'
    Tests = 'ParameterLayoutTests.LeadingMultiLineLambda_StaysWithTheOption'
}
