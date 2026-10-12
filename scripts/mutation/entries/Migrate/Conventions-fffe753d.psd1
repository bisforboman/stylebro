# Issue #129: init counts implicit private (BRO1404) and combined declarations (BRO1142, BRO1114)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (access.Count <= 1 && access.All(m => m.IsKind(SyntaxKind.PrivateKeyword)))'
    Replace = 'if (access.Count <= 2)'
    Tests = 'MigrationTests.Conventions_CountPrivateByDefaultMembers_WithAndWithoutPrivate'
}
