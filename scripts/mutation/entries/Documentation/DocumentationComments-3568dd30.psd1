# Trial 21: a bare ampersand is malformed
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationComments.cs'
    Find = ' || IsBareAmpersand(t)'
    Replace = ''
    Tests = 'DocumentationTests.MalformedDocumentation_IsToldByTheEndTags'
}
