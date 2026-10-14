# Partial types are BRO1007's, not counted for BRO1404's types.
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'SyntaxKind.PrivateKeyword or SyntaxKind.PartialKeyword or SyntaxKind.FileKeyword))'
    Replace = 'SyntaxKind.PrivateKeyword or SyntaxKind.FileKeyword))'
    Tests = 'StyleBro.Tests.MigrationTests.Conventions_CountTypesWithoutInternal_AndCaseBlocksAtTheLabel'
}
