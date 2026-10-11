# A documented property is set apart (BRO1505), BRO1601 adds the blank line below (Ocelot)
@{
    File = 'src/StyleBro.CodeFixes/Documentation/DocumentationCodeFixProvider.cs'
    Find = 'if (separate && member is PropertyDeclarationSyntax property'
    Replace = 'if (false && member is PropertyDeclarationSyntax property'
    Tests = 'DocumentationTests.InheritDoc_OnAdjacentProperties_SeparatesThem'
}
