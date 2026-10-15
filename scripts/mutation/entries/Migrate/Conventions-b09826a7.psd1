# Trial 22: init conventions (partial parts, empty blocks, one-line properties, accessor layout, leading lambdas)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (node is TypeDeclarationSyntax { Modifiers: var partialModifiers } && partialModifiers.Any(SyntaxKind.PartialKeyword))'
    Replace = 'if (node is TypeDeclarationSyntax { Modifiers: var partialModifiers })'
    Tests = 'MigrationTests.Conventions_CountLeadingLambdas_EmptyBlocks_OneLineProperties_AndPartialParts'
}
