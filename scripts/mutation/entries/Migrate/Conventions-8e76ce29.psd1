# Issue #129: init counts implicit private (BRO1404) and combined declarations (BRO1142, BRO1114)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '&& field.Modifiers.ToString() == ((FieldDeclarationSyntax)node).Modifiers.ToString(),'
    Replace = ','
    Tests = 'MigrationTests.Conventions_CountCombinedDeclarations_AgainstOnesThatCouldHaveBeenCombined'
}
