# BRO1603: a quoted question in <c> is passed over too
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = 'text[text.Length - 1] is ''\'''' or ''"'' || text.Any'
    Replace = 'text.Any'
    Tests = 'DocumentationTests.InlineElementEndingInAQuestionOrExclamation_IsPassedOver'
}
