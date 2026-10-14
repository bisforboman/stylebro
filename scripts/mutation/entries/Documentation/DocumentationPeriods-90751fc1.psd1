# Trial 21: an inline element ending a sentence
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = 'element.StartTag.Name.LocalName.ValueText != "c" || '
    Replace = ''
    Tests = 'DocumentationTests.InlineElementEndingASentence_GetsNoPeriod'
}
