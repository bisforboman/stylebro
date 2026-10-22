# Performance: BRO1617/BRO1618 open only elements and their tags
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationStyle.cs'
    Find = 'XmlElementStartTagSyntax or XmlEmptyElementSyntax ||'
    Replace = 'XmlElementStartTagSyntax ||'
    Tests = 'DocumentationStyleTests.GenericCref_EntitiesBecomeBraces'
}
