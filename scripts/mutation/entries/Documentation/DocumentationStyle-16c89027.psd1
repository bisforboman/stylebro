# BRO1617-BRO1619 (documentation style) and BRO1504's exempt prefixes
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationStyle.cs'
    Find = 'if (element.StartTag.Name.LocalName.ValueText != "c"'
    Replace = 'if (false'
    Tests = 'DocumentationStyleTests'
}
