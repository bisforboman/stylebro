# BRO1603: several words in <c> ending in '?' or '!' are a question or an exclamation
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = ' || text.Any(char.IsWhiteSpace));'
    Replace = ');'
    Tests = 'DocumentationTests.InlineElementEndingInAQuestionOrExclamation_IsPassedOver'
}
