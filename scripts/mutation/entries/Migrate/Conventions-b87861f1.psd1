# Issue #129: init counts implicit private (BRO1404) and combined declarations (BRO1142, BRO1114)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '&& local.UsingKeyword.IsKind(SyntaxKind.None) == ((LocalDeclarationStatementSyntax)node).UsingKeyword.IsKind(SyntaxKind.None),'
    Replace = ','
    Tests = 'MigrationTests.Conventions_CountCombinedDeclarations_AgainstOnesThatCouldHaveBeenCombined'
}
