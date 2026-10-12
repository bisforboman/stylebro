# Issue #129: init counts implicit private (BRO1404) and combined declarations (BRO1142, BRO1114)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'BasePropertyDeclarationSyntax property => property.ExplicitInterfaceSpecifier is null'
    Replace = 'BasePropertyDeclarationSyntax property => true'
    Tests = 'MigrationTests.Conventions_CountPrivateByDefaultMembers_WithAndWithoutPrivate'
}
