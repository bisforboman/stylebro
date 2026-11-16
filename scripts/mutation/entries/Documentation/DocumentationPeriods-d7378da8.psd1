# BRO1603 (StyleCop 1.3's SA1629): an inline element ending in '?' or '!' is passed over, the text before it gets the period
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = 'if (EndsWithQuestionOrExclamation(child))'
    Replace = 'if (false)'
    Tests = 'DocumentationTests.InlineElementEndingInAQuestionOrExclamation_IsPassedOver'
}
