# BRO1106 literal style
@{
    File = 'src/StyleBro.Analyzers/Documentation/ParameterDocumentation.cs'
    Find = 'TypeDeclarationSyntax type => type.ParameterList,'
    Replace = ''
    Tests = 'DocumentationTests'
}
