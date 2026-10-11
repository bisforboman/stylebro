# BRO1617-BRO1619 (documentation style) and BRO1504's exempt prefixes
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationStyle.cs'
    Find = '|| element.StartTag.Attributes.Count > 0'
    Replace = ''
    Tests = 'DocumentationStyleTests'
}
