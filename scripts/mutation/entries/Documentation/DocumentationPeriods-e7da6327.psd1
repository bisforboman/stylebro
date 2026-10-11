# BRO1603: elements StyleCop doesn't know are passed over
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = '&& text[end] == ''<'' && text[end + 1] == ''/'''
    Replace = '&& text[end] == ''<'''
    Tests = 'DocumentationTests.UnknownElements_ArePassedOver'
}
