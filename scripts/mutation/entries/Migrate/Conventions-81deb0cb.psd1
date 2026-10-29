# BRO1503 convention: an empty type body isn't counted (trial 61)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'open.IsMissing || open.GetNextToken() == close'
    Replace = 'open.IsMissing'
    Tests = 'MigrationTests.Conventions_CountNamespaceAndTypeBracesWithAndWithoutABlankLineBelow'
}
