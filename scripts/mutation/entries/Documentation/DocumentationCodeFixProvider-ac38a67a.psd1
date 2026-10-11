# A documented property is set apart (BRO1505), BRO1601 adds the blank line below (Ocelot)
@{
    File = 'src/StyleBro.CodeFixes/Documentation/DocumentationCodeFixProvider.cs'
    Find = '&& !(blankLineBefore && property.Parent is TypeDeclarationSyntax type && documented.Contains('
    Replace = '&& !(false && property.Parent is TypeDeclarationSyntax type && documented.Contains('
    Tests = 'DocumentationTests.InheritDoc_OnAdjacentProperties_SeparatesThem'
}
