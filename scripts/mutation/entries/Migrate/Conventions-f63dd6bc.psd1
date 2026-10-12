# Issue #129: init counts implicit private (BRO1404) and combined declarations (BRO1142, BRO1114)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'ConstructorDeclarationSyntax constructor => !constructor.Modifiers.Any(SyntaxKind.StaticKeyword)'
    Replace = 'ConstructorDeclarationSyntax constructor => true'
    Tests = 'MigrationTests.Conventions_CountPrivateByDefaultMembers_WithAndWithoutPrivate'
}
