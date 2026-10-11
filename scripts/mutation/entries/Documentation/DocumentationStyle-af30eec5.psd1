# BRO1617-BRO1619 (documentation style) and BRO1504's exempt prefixes
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationStyle.cs'
    Find = ' || !elements.All(e => ParameterDocumentation.IsOnOwnLines(e.Node, text))'
    Replace = ''
    Tests = 'DocumentationStyleTests'
}
