# BRO1603: one word of code in <c> ending in '?' or '!' ('int?', 'value!') still ends the sentence
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = '            && (name != "c" || text[text.Length - 1] is ''\'''' or ''"'' || text.Any(char.IsWhiteSpace));'
    Replace = ';'
    Tests = 'DocumentationTests.CodeEndingInAQuestionOrExclamationMark_StillEndsTheSentence'
}
