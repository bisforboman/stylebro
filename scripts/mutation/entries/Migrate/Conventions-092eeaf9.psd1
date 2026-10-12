# Issue #129: init counts implicit private (BRO1404) and combined declarations (BRO1142, BRO1114)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'FieldDeclarationSyntax field => field.AttributeLists.Count == 0 && ((FieldDeclarationSyntax)node).AttributeLists.Count == 0'
    Replace = 'FieldDeclarationSyntax field => true'
    Tests = 'MigrationTests.Conventions_CountCombinedDeclarations_AgainstOnesThatCouldHaveBeenCombined'
}
