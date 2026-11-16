# BRO1603: closing quotes may follow the '?' or '!'
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = '        while (end > 0 && text[end - 1] is ''"'' or ''\'''' or ''\u2019'' or ''\u201D'')'
    Replace = '        while (false)'
    Tests = 'DocumentationTests.InlineElementEndingInAQuestionOrExclamation_IsPassedOver'
}
