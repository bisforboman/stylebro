# Lambda and accessor bodies are not one-line blocks BRO1508/BRO1509 judge
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'oneStatement.Parent is not (AnonymousFunctionExpressionSyntax or AccessorDeclarationSyntax)'
    Replace = 'true'
    Tests = 'MigrationTests.Conventions_CountOneLineBlocksWithCode'
}
