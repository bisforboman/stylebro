# An empty attribute value (name="") is well-formed XML
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationComments.cs'
    Find = ' && !t.Parent!.AncestorsAndSelf().Any(n => n is XmlAttributeSyntax)'
    Replace = ''
    Tests = 'DocumentationTests.MalformedDocumentation_IsToldByTheEndTags|DocumentationTests.UnnamedParamTags_GetTheirName'
}
