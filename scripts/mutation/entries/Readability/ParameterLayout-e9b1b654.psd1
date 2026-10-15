# Trial 22 (owner's choice 2026-10-15): the leading multi-line lambda option
@{
    File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'
    Find = 'return misplaced is not null && !(AllowsLeadingLambda(options) && IsLeadingLambda(list, line))'
    Replace = 'return misplaced is not null && !(false)'
    Tests = 'ParameterLayoutTests.LeadingMultiLineLambda_StaysWithTheOption'
}
