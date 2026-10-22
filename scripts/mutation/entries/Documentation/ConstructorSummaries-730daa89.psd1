# Trial 24 (Refit): SST1642 does not check static constructors
@{
    File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'
    Find = '|| (styleSharp && member is ConstructorDeclarationSyntax && member.Modifiers.Any(SyntaxKind.StaticKeyword))'
    Replace = '|| false'
    Tests = 'DocumentationTests.ConstructorSummaries_FollowStyleSharp'
}
