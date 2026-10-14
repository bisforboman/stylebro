# BRO1609: a top-level placeholder is left alone (trial 20)
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationTags.cs'
    Find = ' && e.Parent is XmlElementSyntax'
    Replace = ''
    Tests = 'DocumentationTests.TopLevelPlaceholder_IsLeftAlone'
}
