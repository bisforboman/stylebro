# BRO1503 convention: a one-line type body isn't counted (trial 61)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '|| Line(open.GetNextToken().SpanStart) == Line(open.SpanStart))'
    Replace = ')'
    Tests = 'MigrationTests.Conventions_CountNamespaceAndTypeBracesWithAndWithoutABlankLineBelow'
}
