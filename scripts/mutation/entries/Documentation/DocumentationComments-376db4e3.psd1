# Malformed doc XML: the end tags tell when the parser reports nothing
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationComments.cs'
    Find = 'e.EndTag.IsMissing || e.EndTag.Name.LocalName.ValueText != e.StartTag.Name.LocalName.ValueText'
    Replace = 'false'
    Tests = 'FullyQualifiedName~MalformedDocumentation_IsToldByTheEndTags'
}
