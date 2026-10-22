# BRO1617-BRO1619 (documentation style) and BRO1504's exempt prefixes
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationStyle.cs'
    Find = '(n is XmlElementSyntax && !IsCode(n))'
    Replace = 'n is XmlElementSyntax'
    Tests = 'DocumentationStyleTests'
}
