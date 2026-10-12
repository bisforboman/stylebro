# Issue #129: init counts implicit private (BRO1404) and combined declarations (BRO1142, BRO1114)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'LocalDeclarationStatementSyntax local => local.Modifiers.ToString() == ((LocalDeclarationStatementSyntax)node).Modifiers.ToString()'
    Replace = 'LocalDeclarationStatementSyntax local => true'
    Tests = 'MigrationTests.Conventions_CountCombinedDeclarations_AgainstOnesThatCouldHaveBeenCombined'
}
