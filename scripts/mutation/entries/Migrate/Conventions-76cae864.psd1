# Issue #129: init counts implicit private (BRO1404) and combined declarations (BRO1142, BRO1114)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'member.Parent is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax'
    Replace = 'member.Parent is not null'
    Tests = 'MigrationTests.Conventions_CountPrivateByDefaultMembers_WithAndWithoutPrivate'
}
