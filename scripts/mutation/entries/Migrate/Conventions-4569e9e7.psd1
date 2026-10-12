# Issue #129: init counts implicit private (BRO1404) and combined declarations (BRO1142, BRO1114)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '!member.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword) || m.IsKind(SyntaxKind.FileKeyword))'
    Replace = 'true'
    Tests = 'MigrationTests.Conventions_CountPrivateByDefaultMembers_WithAndWithoutPrivate'
}
