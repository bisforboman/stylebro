# BRO1603: one closing parenthesis may follow the '?' or '!'
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = '        if (end > 0 && text[end - 1] == '')'')'
    Replace = '        if (false)'
    Tests = 'DocumentationTests.InlineElementEndingInAQuestionOrExclamation_IsPassedOver'
}
