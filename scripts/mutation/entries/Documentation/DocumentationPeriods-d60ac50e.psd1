# BRO1603: only inline elements are passed over for their '?' or '!' (a <para> holds the sentence itself)
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = '        if (!InlineElements.Contains(name))'
    Replace = '        if (false)'
    Tests = 'DocumentationTests.InlineElementEndingInAQuestionOrExclamation_WithoutAnOpenSentenceBefore_IsNotReported'
}
