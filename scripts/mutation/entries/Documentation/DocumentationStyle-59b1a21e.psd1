# BRO1617-BRO1619 (documentation style) and BRO1504's exempt prefixes
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationStyle.cs'
    Find = 'token.Parent is not (TypeArgumentListSyntax or TypeParameterListSyntax)'
    Replace = 'false'
    Tests = 'DocumentationStyleTests'
}
